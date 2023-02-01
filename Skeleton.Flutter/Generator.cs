using System;
using System.IO.Abstractions;
using System.Reflection;
using Skeleton.ProjectGeneration;
using Skeleton.Model;
using Skeleton.Templating;
using HandlebarsDotNet;
using Serilog;

namespace Skeleton.Flutter
{
    public class Generator
    {
        private readonly IFileSystem _fs;
        private readonly Settings _settings;
        private readonly FileWriter _fileWriter;
        private string _rootFolder;

        public Generator(IFileSystem fileSystem, Settings settings, FileWriter fileWriter)
        {
            _fs = fileSystem;
            _settings = settings;
            _fileWriter = fileWriter;
        }

        public void Generate(Domain domain)
        {
            Log.Information("Starting Flutter Generation");
            _rootFolder = _fs.Path.Combine(_settings.RootDirectory, _settings.FlutterSettings.FlutterRootDirectory);
        }

        public static Func<object, string> GetCompiledTemplate(string templateName)
        {
            var template = Util.GetTemplate(templateName, Assembly.GetExecutingAssembly());
            return Handlebars.Compile(template);
        }
    }
}
