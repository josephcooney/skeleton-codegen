using System.IO.Abstractions;
using System.Reflection;
using Skeleton.Model;
using Skeleton.Templating;

namespace Skeleton.ReactNative;

public class Generator : GeneratorBase
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
        return null; // TODO
    }

    public override Assembly Assembly => Assembly.GetExecutingAssembly();
}