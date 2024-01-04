using System.IO.Abstractions;
using System.Reflection;
using Skeleton.Model;
using Skeleton.Templating;
using Skeleton.Templating.ReactClient;

namespace Skeleton.ReactNative;

public class Generator : ReactClientGenerator
{
    private readonly IFileSystem _fileSystem;
    private readonly Settings _settings;

    public Generator(IFileSystem fileSystem, Settings settings)
    {
        _fileSystem = fileSystem;
        _settings = settings;
        RootDirectory = _fileSystem.Path.Combine(_settings.RootDirectory, _settings.ReactNativeSettings.RootDirectory);
    }
    
    public string RootDirectory { get; }
    
    public override List<CodeFile> Generate(Domain domain)
    {
        Util.RegisterHelpers(domain);
        var files = new List<CodeFile>();

        foreach (var type in domain.FilteredTypes)
        {
            if (type.GenerateUI)
            {
                var edit = new CodeFile { Name = Util.TypescriptFileName(type.Name) + "ApiClient.ts", Contents = GenerateApiClient(type, domain), RelativePath = GetRelativePathFromTypeName(type.Name), Template = TemplateNames.ApiClient };
                files.Add(edit);
            }
        }

        return files;
    }

    public override Assembly Assembly => Assembly.GetExecutingAssembly();
}