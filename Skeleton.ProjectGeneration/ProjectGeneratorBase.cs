using System;
using System.IO;
using System.IO.Abstractions;
using Skeleton.Model;

namespace Skeleton.ProjectGeneration
{
    public class ProjectGeneratorBase
    {
        protected readonly Settings _settings;
        protected readonly IFileSystem _fileSystem;

        public ProjectGeneratorBase(Settings settings, IFileSystem fileSystem)
        {
            _settings = settings;
            _fileSystem = fileSystem;
        }


        protected void CopyDirectory(string sourceDirectoryName, string targetDirectoryName)
        {
            var source = _fileSystem.DirectoryInfo.FromDirectoryName(sourceDirectoryName);
            if (!source.Exists)
            {
                throw new DirectoryNotFoundException(sourceDirectoryName);
            }

            if ((source.Attributes & FileAttributes.Hidden) != 0)
            {
                // don't copy hidden directories like .git and .vs
                return;
            }

            if (!_fileSystem.Directory.Exists(targetDirectoryName))
            {
                _fileSystem.Directory.CreateDirectory(targetDirectoryName);
            }

            foreach (var file in source.GetFiles())
            {
                var targetFileName = _fileSystem.Path.Combine(targetDirectoryName, file.Name);
                file.CopyTo(targetFileName);
            }

            foreach (var subDirectory in source.GetDirectories())
            {
                var targetSubDirectoryName = _fileSystem.Path.Combine(targetDirectoryName, subDirectory.Name);
                CopyDirectory(subDirectory.FullName, targetSubDirectoryName);
            }
        }

        protected void RecurseDirectory(string directoryName, Action<IFileInfo> fileProcessingFunction, Func<IDirectoryInfo, IDirectoryInfo> directoryProcessingFunction)
        {
            var source = _fileSystem.DirectoryInfo.FromDirectoryName(directoryName);
            if (!source.Exists)
            {
                throw new DirectoryNotFoundException(directoryName);
            }

            if (directoryProcessingFunction != null)
            {
                source = directoryProcessingFunction(source);
            }

            foreach (var file in source.GetFiles())
            {
                fileProcessingFunction(file);
            }

            foreach (var subDirectory in source.GetDirectories())
            {
                RecurseDirectory(subDirectory.FullName, fileProcessingFunction, directoryProcessingFunction);
            }
        }
    }
}
