using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using Skeleton.Model;
using Skeleton.Templating;
using Serilog;
using Skeleton.Templating.Classes.Adapters;
using Skeleton.Templating.ReactClient;
using Skeleton.Templating.ReactClient.Adapters;

namespace Skeleton.Flutter
{
    public class Generator : GeneratorBase
    {
        private readonly IFileSystem _fs;
        private readonly Settings _settings;
        public const string DartFileExtension = ".dart";

        public Generator(IFileSystem fileSystem, Settings settings)
        {
            _fs = fileSystem;
            _settings = settings;
            
            FlutterRootFolder = _fs.Path.Combine(_settings.RootDirectory, _settings.FlutterSettings.FlutterRootDirectory);
            if (!FlutterRootFolder.ToLowerInvariant().EndsWith("lib"))
            {
                FlutterRootFolder = _fs.Path.Combine(FlutterRootFolder, ".\\lib");
            }
        }

        public string FlutterRootFolder { get; private set; }
        
        public override List<CodeFile> Generate(Domain domain)
        {
            var files = new List<CodeFile>();
            
            Log.Information("Starting Flutter Generation");
            Log.Debug("Generating flutter code into {FlutterDirectory}", FlutterRootFolder);

            files.AddRange(GenerateClientModels(domain));
            return files;
        }

        public override Assembly Assembly => Assembly.GetExecutingAssembly();

        public List<CodeFile> GenerateClientModels(Domain domain)
        {
            Util.RegisterHelpers(domain);
            var files = new List<CodeFile>();
            
            foreach (var type in domain.FilteredTypes)
            {
                if (type.GenerateUI)
                {
                    var adapter = new ClientApiAdapter(type, domain);
                    if (adapter.Operations.Any())
                    {
                        var path = _fs.Path.Combine("model\\", Util.SnakeCase(type.Name) + "\\");

                        foreach (var op in adapter.ApiOperations)
                        {
                            // generate the request model if there is one
                            if (op.HasCustomType)
                            {
                                var file = new CodeFile { Name = Util.SnakeCase(op.CustomType.Name) + DartFileExtension, Contents = GenerateClientApiModel(op), RelativePath = path, Template = TemplateNames.ApiClientModel};
                                files.Add(file);
                            }

                            // generate the response if there is one
                            var resultType = op.SimpleReturnType as ResultType;
                            if (!op.NoResult && !op.IsSingular && (resultType == null || resultType.RelatedType == type) && op.SimpleReturnType != null)
                            {
                                var file = new CodeFile { Name = Util.SnakeCase(op.SimpleReturnType.Name) + DartFileExtension, Contents = GenerateClientApiResultType(op.SimpleReturnType), RelativePath = path, Template = TemplateNames.ApiClientResult };
                                files.Add(file);
                            }
                        }

                        // generate api client
                        var apiClientFile = new CodeFile
                        {
                            Name = Util.SnakeCase(type.Name) + "_api_client" + DartFileExtension,
                            Contents = GenerateFromTemplate(adapter, FlutterTemplateNames.ApiClient),
                            RelativePath = ".\\api", Template = TemplateNames.ApiClient
                        };
                        files.Add(apiClientFile);
                    }
                }
            }

            return files;
        }

        private string GenerateClientApiModel(OperationAdapter op)
        {
            return GenerateFromTemplate(op, FlutterTemplateNames.ApiClientModel);
        }

        private string GenerateClientApiResultType(SimpleType type)
        {
            return GenerateFromTemplate(type, FlutterTemplateNames.ApiClientResult);
        }
    }

    internal class FlutterTemplateNames
    {
        public const string ApiClientModel = "model.dart";
        public const string ApiClientResult = "result.dart";
        public const string ApiClient = "api.dart";
    } 
}
