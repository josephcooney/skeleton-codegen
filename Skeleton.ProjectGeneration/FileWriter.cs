using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Skeleton.Model;
using Skeleton.Templating;
using Serilog;

namespace Skeleton.ProjectGeneration
{
    public class FileWriter
    {
        private readonly IFileSystem _fs;
        private readonly string _rootFolder;

        public FileWriter(IFileSystem fs, string rootFolder)
        {
            _fs = fs;
            _rootFolder = rootFolder;
        }

        public void ApplyCodeFiles(List<CodeFile> files, string folderName)
        {
            ApplyCodeFiles(files, folderName, null);
        }

        public void ApplyCodeFiles(List<CodeFile> files, string folderName, Action<CodeFile> postUpdateAction)
        {
            if (files.Any())
            {
                var folderPath = _fs.Path.Combine(_rootFolder, folderName);

                if (!_fs.Directory.Exists(folderPath))
                {
                    _fs.Directory.CreateDirectory(folderPath);
                }

                foreach (var codeFile in files)
                {
                    var location = folderPath;
                    if (!string.IsNullOrEmpty(codeFile.RelativePath))
                    {
                        location = _fs.Path.Combine(folderPath, codeFile.RelativePath);
                    }

                    if (!_fs.Directory.Exists(location))
                    {
                        _fs.Directory.CreateDirectory(location);
                    }

                    var fileName = _fs.Path.Combine(location, codeFile.Name);

                    if (_fs.File.Exists(fileName))
                    {
                        if (codeFile.IsFragment)
                        {
                            ApplyFragment(codeFile, fileName);
                        }
                        else
                        {
                            if (GeneratedFileHasBeenManuallyModified(fileName, codeFile))
                            {
                                Log.Debug("File {FileName} was not updated because it has been manually modified", fileName);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (codeFile.IsFragment)
                        {
                            Log.Information("File {FileName} was not found to apply fragment to.", fileName);
                        }
                    }

                    if (!codeFile.IsFragment)
                    {
                        _fs.File.WriteAllText(fileName, codeFile.Contents);
                    }
                    postUpdateAction?.Invoke(codeFile);

                    Log.Debug("Updated {FileName} from template {Template}", fileName, codeFile.Template);
                }
            }
        }

        private void ApplyFragment(CodeFile codeFile, string fileName)
        {
            var contents = _fs.File.ReadAllText(fileName);
            if (!string.IsNullOrEmpty(codeFile.Contents))
            {
                var lines = codeFile.Contents.Split('\n');
                var start = contents.IndexOf(lines.First().TrimEnd('\r'));
                var end = contents.IndexOf(lines.Last().TrimEnd('\r'));
                if (start > -1 && end > start)
                {
                    var updatedFileContents = contents.Substring(0, start) + codeFile.Contents +
                                              contents.Substring(end + lines.Last().Length);
                    _fs.File.WriteAllText(fileName, updatedFileContents);
                }
                else
                {
                    Log.Error($"Unable to find location to insert fragment into File {fileName}");
                }
            }
            else
            {
                Log.Information($"Fragment File {fileName} was empty");
            }
        }

        private bool GeneratedFileHasBeenManuallyModified(string fileName, CodeFile codeFile)
        {
            var firstLine = _fs.File.ReadLines(fileName).FirstOrDefault();
            return !string.IsNullOrEmpty(firstLine) && !codeFile.Contents.StartsWith(firstLine);
        }
    }
}
