using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Skeleton.Model;
using Serilog;

namespace Skeleton.Console
{
    public class GeneratedFileCleaner
    {
        private readonly IFileSystem _fileSystem;
        private readonly Settings _settings;

        public GeneratedFileCleaner(IFileSystem fileSystem, Settings settings)
        {
            _fileSystem = fileSystem;
            _settings = settings;
        }

        public void ClearGeneratedFiles()
        {
            if (!_fileSystem.Directory.Exists(_settings.RootDirectory))
            {
                Log.Error("Unable to removed generated files - root directory does not exist.");
                return;
            }

            ClearClientFiles();
            ClearSqlFiles();
            ClearCsharpFiles();

            Log.Information("Finished clearing generated files");
        }

        private void ClearClientFiles()
        {
            if (_fileSystem.Directory.Exists(_settings.RootDirectory))
            {
                var reactPath = _fileSystem.Path.Combine(_settings.RootDirectory, _settings.ResolveClientAppDirectory());
                if (_fileSystem.Directory.Exists(reactPath))
                {
                    var tsxFiles = _fileSystem.Directory.GetFiles(reactPath, "*.tsx", SearchOption.AllDirectories);
                    ClearClientFileList(tsxFiles);
                
                    var tsFiles = _fileSystem.Directory.GetFiles(reactPath, "*.ts", SearchOption.AllDirectories);
                    ClearClientFileList(tsFiles);
                }
            }
            
            if (_settings.ClientAppTypes.Contains(ClientAppUIType.Flutter))
            {
                var flutterGenerator = new Skeleton.Flutter.Generator(_fileSystem, _settings);
                var flutterRoot = flutterGenerator.RootDirectory;
                if (_fileSystem.Directory.Exists(flutterRoot))
                {
                    var flutterFiles = _fileSystem.Directory.GetFiles(flutterRoot, "*" + Skeleton.Flutter.Generator.DartFileExtension, SearchOption.AllDirectories);
                    foreach (var file in flutterFiles)
                    {
                        var contents = _fileSystem.File.ReadAllLines(file);
                        if (contents != null && contents.Any() && IsGeneratedDartFile(contents.First()))
                        {
                            _fileSystem.File.Delete(file);
                        }
                    }
                }
            }

            if (_settings.ClientAppTypes.Contains(ClientAppUIType.ReactNative))
            {
                var rnGenerator = new Skeleton.ReactNative.Generator(_fileSystem, _settings);
                if (_fileSystem.Directory.Exists(rnGenerator.RootDirectory))
                {
                    var tsxFiles = _fileSystem.Directory.GetFiles(rnGenerator.RootDirectory, "*.tsx", SearchOption.AllDirectories);
                    ClearClientFileList(tsxFiles);
                
                    var tsFiles = _fileSystem.Directory.GetFiles(rnGenerator.RootDirectory, "*.ts", SearchOption.AllDirectories);
                    ClearClientFileList(tsFiles);
                }
            }
        }

        private void ClearSqlFiles()
        {
            if (_fileSystem.Directory.Exists(DbDirectory))
            {
                // we only want to do this in the 'latest' version of the DB directory e.g. MyProject\Data\Database\0003 but 0002 and 0001 should not be touched
                var childDirectories = _fileSystem.Directory.EnumerateDirectories(DbDirectory).OrderByDescending(n => n).ToList();
                if (!childDirectories.Any())
                {
                    return;
                }
                
                var sqlFiles = _fileSystem.Directory.GetFiles(childDirectories.First(), "*.sql", SearchOption.AllDirectories);
                foreach (var file in sqlFiles)
                {
                    var contents = _fileSystem.File.ReadAllLines(file);
                    if (contents != null && contents.Any() && IsGeneratedSqlFile(contents.First()))
                    {
                        _fileSystem.File.Delete(file);
                    }
                }
            }
        }

        private void ClearCsharpFiles()
        {
            var csharpFiles = _fileSystem.Directory.GetFiles(_settings.RootDirectory, "*.cs", SearchOption.AllDirectories).ToList();
            if (_fileSystem.Directory.Exists(DbDirectory))
            {
                var dataCsFiles = _fileSystem.Directory.GetFiles(DbDirectory, "*.cs", SearchOption.AllDirectories);
                csharpFiles.AddRange(dataCsFiles);
            }
            foreach (var file in csharpFiles.Distinct())
            {
                if (_fileSystem.File.Exists(file))
                {
                    var contents = _fileSystem.File.ReadAllLines(file);
                    if (contents != null && contents.Any() && IsGeneratedCsFile(contents.First()))
                    {
                        _fileSystem.File.Delete(file);
                    }
                }
            }
        }

        private void ClearClientFileList(string[] files)
        {
            foreach (var file in files)
            {
                var contents = _fileSystem.File.ReadAllLines(file);
                if (contents != null && contents.Any() && IsGeneratedTypescriptFile(contents.First()))
                {
                    _fileSystem.File.Delete(file);
                }
            }
        }

        private bool IsGeneratedTypescriptFile(string firstLine)
        {
            return firstLine.Trim() == "// generated by a tool";
        }

        private bool IsGeneratedSqlFile(string firstLine)
        {
            return firstLine.Trim() == "-- generated by a tool";
        }

        private bool IsGeneratedCsFile(string firstLine)
        {
            return firstLine.Trim() == "// generated by a tool";
        }

        private bool IsGeneratedDartFile(string firstLine)
        {
            return firstLine.Trim() == "// generated by a tool";
        }

        private string DbDirectory
        {
            get
            {
                if (!string.IsNullOrEmpty(_settings.DataDirectory))
                {
                    return _fileSystem.Path.Combine(_settings.RootDirectory, _settings.DataDirectory);
                }

                return _fileSystem.Path.Combine(_settings.RootDirectory, "Database");
            }
        }
    }
}
