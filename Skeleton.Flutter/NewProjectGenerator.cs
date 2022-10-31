using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO.Abstractions;
using Skeleton.Flutter.NewProject;
using Skeleton.ProjectGeneration;
using Skeleton.Model;
using Skeleton.Templating;
using Serilog;
using Svg;

namespace Skeleton.Flutter
{
    public class NewProjectGenerator : ProjectGeneratorBase
    {
        private readonly string _flutterRootDirectory;
        private readonly FileWriter _fileWriter;
        private const string FixMeAppName = "fixmeappname";
        private const string FixMeDefaultNamespace = "FixMeDefaultNamespace";
        private const string FixMeAppTitle = "FixMeAppTitle";

        public NewProjectGenerator(string flutterRootDirectory, Settings settings, IFileSystem fileSystem, FileWriter fileWriter) : base(settings, fileSystem)
        {
            _flutterRootDirectory = flutterRootDirectory;
            _fileWriter = fileWriter;
        }

        public bool Generate()
        {
            try
            {
                CopyBaseProject();
                RenameFilesAndDirectories();
                FindAndReplaceInFiles();
                if (!string.IsNullOrEmpty(_settings.NewAppSettings.LogoFileName))
                {
                    CopyLogoFile();
                }

                //UpdateLaunchSettings();

                if (!string.IsNullOrEmpty(_settings.NewAppSettings.BrandColour))
                {
                    UpdateBrandColour();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error creating new solution");
                return false;
            }
            Log.Information("Finished creating new Solution");
            return true;
        }

        private void CopyBaseProject()
        {
            Log.Information("Copying Base Flutter Project");
            // eventually we'll git clone this from a public repo
            var source = @"E:\FlutterDev\EmptyFlutterProject\fix_me_app_name";
            CopyDirectory(source, _flutterRootDirectory);
        }

        private void RenameFilesAndDirectories()
        {
            Log.Information("Renaming Files");
            RecurseDirectory(_flutterRootDirectory, file =>
            {
                if (file.Name.Contains(FixMeAppName))
                {
                    var newFileName = _fileSystem.Path.Combine(file.DirectoryName, file.Name.Replace(FixMeAppName, _settings.ApplicationName.ToLowerInvariant()));
                    file.MoveTo(newFileName);
                }
            }, dir =>
            {
                if (dir.Name.Contains(FixMeAppName))
                {
                    var newName = dir.Name.Replace(FixMeAppName, _settings.ApplicationName.ToLowerInvariant());
                    newName = _fileSystem.Path.Combine(dir.Parent.FullName, newName);
                    Log.Debug("Re-naming directory from {OldDirectoryName} to {NewDirectoryName}.", dir.FullName, newName);
                    dir.MoveTo(newName);
                }

                return dir;
            });
        }

        private void FindAndReplaceInFiles()
        {
            Log.Information("Replacing File Contents");
            Log.Debug("Starting at directory {DirectoryName}", _flutterRootDirectory);
            RecurseDirectory(_flutterRootDirectory, file =>
            {
                var contents = _fileSystem.File.ReadAllText(file.FullName);
                var touched = false;
                if (contents.Contains(FixMeAppName))
                {
                    contents = contents.Replace(FixMeAppName, _settings.ApplicationName);
                    touched = true;
                }

                if (contents.Contains(FixMeDefaultNamespace))
                {
                    contents = contents.Replace(FixMeDefaultNamespace, _settings.ApplicationName);
                    touched = true;
                }

                if (contents.Contains(FixMeAppTitle))
                {
                    contents = contents.Replace(FixMeAppTitle, Util.HumanizeName(_settings.ApplicationName));
                    touched = true;
                }

                if (touched)
                {
                    Log.Debug("Updating file {FileName}", file.FullName);
                    _fileSystem.File.WriteAllText(file.FullName, contents);
                }

            }, null);
        }

        private void CopyLogoFile()
        {
            if (!_fileSystem.File.Exists(_settings.NewAppSettings.LogoFileName))
            {
                Log.Error("Logo file {FileName} does not exist.", _settings.NewAppSettings.LogoFileName);
                return;
            }

            var svgLogo = SvgDocument.Open(_settings.NewAppSettings.LogoFileName);
            var androidResourcePath = _fileSystem.Path.Combine(_flutterRootDirectory, "android\\app\\src\\main\\res");
            var sizesAndFileNames = new Dictionary<string, int>()
            {
                { "mipmap-hdpi", 72 },
                {"mipmap-mdpi", 48},
                {"mipmap-xhdpi", 96 },
                {"mipmap-xxhdpi", 144 },
                {"mipmap-xxxhdpi", 192  }
            };
            foreach (var sizeAndDirName in sizesAndFileNames)
            {
                ResizeAndUpdateImage(sizeAndDirName.Key, sizeAndDirName.Value, androidResourcePath, svgLogo);
            }
        }

        private void ResizeAndUpdateImage(string folderName, int size, string androidResourcePath, SvgDocument image)
        {
            var resized = image.Draw(size, size);

            var imageFilePath = _fileSystem.Path.Combine(androidResourcePath, folderName, "ic_launcher.png");
            if (_fileSystem.File.Exists(imageFilePath))
            {
                Log.Debug("Deleting existing Flutter application icon {FileName}", imageFilePath);
                _fileSystem.File.Delete(imageFilePath);
            }

            resized.Save(imageFilePath, ImageFormat.Png);
        }

        private void UpdateBrandColour()
        {
            var model = new ColorModel(_settings.NewAppSettings.BrandColour);
            var template = Generator.GetCompiledTemplate("main.dart.colorfragment");
            var codeFile = new CodeFile {Contents = template(model), IsFragment = true, Name = "main.dart", RelativePath = @".\lib\"};
            _fileWriter.ApplyCodeFiles(new List<CodeFile>(){codeFile}, _flutterRootDirectory);
        }
    }
}
