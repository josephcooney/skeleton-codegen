﻿using System.IO.Abstractions;
using System.Reflection;
using Skeleton.Model;
using Skeleton.Templating;
using Skeleton.Templating.ReactClient;
using Skeleton.Templating.ReactClient.Adapters;

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

        var apiClients = base.Generate(domain);
        files.AddRange(apiClients);

        var models = base.GenerateClientModels(domain);
        files.AddRange(models);

        var components = GenerateComponents(domain);
        files.AddRange(components);

        return files;
    }

    public override Assembly Assembly => Assembly.GetExecutingAssembly();

    public List<CodeFile> GenerateComponents(Domain domain)
    {
        var files = new List<CodeFile>();
        foreach (var type in domain.FilteredTypes.OrderBy(t => t.Name))
        {
            if (type.GenerateUI)
            {
                var namestart = Util.TypescriptFileName(type.Name);
                var path = GetRelativePathFromTypeName(type.Name);
                var adapter = new ClientApiAdapter(type, domain);

                if (adapter.GenerateSelectComponent)
                {
                    // TODO - generate select
                }
                
                // TODO generate detail screen
            }
        }
        
        // build 'list' UIs from return types
        foreach (var rt in domain.Operations.Where(o => o.GenerateUI && o.RelatedType?.GenerateUI == true && (o.Returns != null && o.Returns.ReturnType == ReturnType.ApplicationType || o.Returns != null && o.Returns.ReturnType == ReturnType.CustomType))
            .Select(o => new {o.Returns.SimpleReturnType, RelatedType = o.Returns.SimpleReturnType is ApplicationType ? o.RelatedType : ((ResultType)o.Returns.SimpleReturnType).RelatedType}) // get the related type from the result type if it is a custom type, or from the operation if the operation returns an application type - allows OpenApi operations to re-use types across application types.
            .Distinct()
            .OrderBy(rt => rt.RelatedType.Name))
        {
            if (rt.RelatedType == null || domain.FilteredTypes.Contains(rt.RelatedType))
            {
                
                var listAdapter = new ListViewAdapter(rt.SimpleReturnType, domain, rt.RelatedType);
                var listPath = GetRelativePathFromTypeName(rt.RelatedType.Name) + "list\\";
                var nameStart = Util.TypescriptFileName(rt.SimpleReturnType.Name);

                files.Add(new CodeFile { Name = nameStart + "ListScreen.tsx", Contents = GenerateFromTemplate(listAdapter, ReactNativeTemplateNames.ListScreen), RelativePath = listPath, Template = TemplateNames.ReactListPage});
                files.Add(new CodeFile { Name = nameStart + "ListItem.tsx", Contents = GenerateFromTemplate(listAdapter, ReactNativeTemplateNames.ListItem), RelativePath = listPath, Template = TemplateNames.ReactListHeader});
                files.Add(new CodeFile { Name = nameStart + "ListRendering.tsx", Contents = GenerateFromTemplate(listAdapter, TemplateNames.ReactListRendering), RelativePath = listPath, Template = TemplateNames.ReactListRendering});
            }
        }

        return files;
    }
}

public class ReactNativeTemplateNames
{
    public const string ListScreen = "ReactNativeListScreen";
    public const string ListItem = "ReactNativeListItem";
}