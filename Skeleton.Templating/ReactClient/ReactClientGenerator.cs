using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Skeleton.Model;
using Skeleton.Templating.Classes;
using Skeleton.Templating.Classes.Adapters;
using Skeleton.Templating.ReactClient.Adapters;
using Serilog;

namespace Skeleton.Templating.ReactClient
{
    public class ReactClientGenerator : GeneratorBase
    {
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
                        var path = GetRelativePathFromTypeName(type.Name);

                        foreach (var op in adapter.ApiOperations)
                        {
                            // generate the request model if there is one
                            if (op.HasCustomType)
                            {
                                var file = new CodeFile { Name = Util.CSharpNameFromName(op.CustomType.Name) + ".ts", Contents = GenerateClientApiModel(op), RelativePath = path, Template = TemplateNames.ApiClientModel};
                                files.Add(file);
                            }

                            // generate the response if there is one
                            // TODO - re-use of models causes the same files to be generated multiple times 
                            var resultType = op.SimpleReturnType as ResultType;
                            if (!op.NoResult && !op.IsSingular && (resultType == null || resultType.RelatedType == type) && op.SimpleReturnType != null)
                            {
                                var file = new CodeFile { Name = Util.CSharpNameFromName(op.SimpleReturnType.Name) + ".ts", Contents = GenerateClientApiResultType(op.SimpleReturnType), RelativePath = path, Template = TemplateNames.ApiClientResult };
                                files.Add(file);
                            }
                        }
                    }
                }
            }

            return files;
        }

        public List<CodeFile> GenerateComponents(Domain domain)
        {
            Util.RegisterHelpers(domain);
            var files = new List<CodeFile>();
            
            var leftNavItems = new CodeFile { Name = "LeftNavItems.tsx", Contents = GenerateLeftNavItems(domain) };
            files.Add(leftNavItems);
            
            var regUserHome = new CodeFile {Name = "RegularUserHome.tsx", Contents = GenerateRegularUserHomepage(domain)};
            files.Add(regUserHome);

            foreach (var type in domain.FilteredTypes.OrderBy(t => t.Name))
            {
                if (type.GenerateUI)
                {
                    var namestart = Util.TypescriptFileName(type.Name);
                    var path = GetRelativePathFromTypeName(type.Name);
                    var adapter = new ClientApiAdapter(type, domain);

                    if (adapter.GenerateSelectComponent)
                    {
                        files.Add(new CodeFile { Name = namestart + "Select.tsx", Contents = GenerateFromTemplate(adapter, TemplateNames.ReactSelectControl), RelativePath = path, Template = TemplateNames.ReactSelectControl});
                    }
                    
                    files.Add(new CodeFile { Name = namestart + "Component.tsx", Contents = GenerateFromTemplate(adapter, TemplateNames.ReactComponent), RelativePath = path, Template = TemplateNames.ReactComponent});
                    files.Add(new CodeFile { Name = namestart + "DetailDisplay.tsx", Contents = GenerateFromTemplate(adapter, TemplateNames.ReactDetailDisplay), RelativePath = path, Template = TemplateNames.ReactDetailDisplay});
                    
                    if (adapter.HasDetails)
                    { 
                        var detailAdapter = new ClientDetailAdapter(type, domain);
                        files.Add(new CodeFile { Name = namestart + "Detail.tsx", Contents = GenerateFromTemplate(detailAdapter, TemplateNames.ReactDetailPage), RelativePath = path, Template = TemplateNames.ReactDetailPage});
                    }

                    if (!type.IsLink)
                    {
                        foreach (var operation in adapter.ApiOperations.Where(op => op.ChangesData && op.GenerateUI))
                        {
                            var changeDataAdapter = new ClientApiInsertUpdateAdapter(type, domain, operation);
                            files.Add(new CodeFile { Name = namestart + operation.FriendlyName + ".tsx", Contents = GenerateFromTemplate(changeDataAdapter, TemplateNames.ReactAddEditPage), RelativePath = path, Template = TemplateNames.ReactAddEditPage });
                            files.Add(new CodeFile { Name = namestart + operation.FriendlyName + "Rendering.tsx", Contents = GenerateFromTemplate(changeDataAdapter, TemplateNames.ReactAddEditPageRendering), RelativePath = path, Template = TemplateNames.ReactAddEditPageRendering });
                        }
                    }
                    

                    if (adapter.Operations.Any(op => op.ChangesData && op.GenerateUI))
                    {
                        files.Add(new CodeFile { Name = namestart + "Validate.tsx", Contents = GenerateFromTemplate(adapter, TemplateNames.ModelValidator), RelativePath = path, Template = TemplateNames.ModelValidator});
                    }

                    if (adapter.CanDelete)
                    {
                        files.Add(new CodeFile { Name = namestart + "ConfirmDelete.tsx", Contents = GenerateFromTemplate(adapter, TemplateNames.ReactConfirmDeletePage), RelativePath = path, Template = TemplateNames.ReactConfirmDeletePage});
                    }
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

                    files.Add(new CodeFile { Name = nameStart + "List.tsx", Contents = GenerateFromTemplate(listAdapter, TemplateNames.ReactListPage), RelativePath = listPath, Template = TemplateNames.ReactListPage});
                    files.Add(new CodeFile { Name = nameStart + "Header.tsx", Contents = GenerateFromTemplate(listAdapter, TemplateNames.ReactListHeader), RelativePath = listPath, Template = TemplateNames.ReactListHeader});
                    files.Add(new CodeFile { Name = nameStart + "Row.tsx", Contents = GenerateFromTemplate(listAdapter, TemplateNames.ReactListRow), RelativePath = listPath, Template = TemplateNames.ReactListRow});
                    files.Add(new CodeFile { Name = nameStart + "ListRendering.tsx", Contents = GenerateFromTemplate(listAdapter, TemplateNames.ReactListRendering), RelativePath = listPath, Template = TemplateNames.ReactListRendering});

                    if (rt.RelatedType.Paged && listAdapter.PrimaryPagedOperation != null)
                    {
                        files.Add(new CodeFile { Name = nameStart + "ListPaged.tsx", Contents = GenerateFromTemplate(listAdapter, TemplateNames.ReactPagedListPage), RelativePath = listPath, Template = TemplateNames.ReactPagedListPage});
                    }
                }
            }

            foreach (var srchOp in domain.Operations.Where(o => o.RelatedType != null).Select(o => new ClientApiOperationAdapter(o, domain, o.RelatedType)).Where(o => o.RelatedType?.GenerateUI == true && o.IsSearch))
            {
                if (srchOp.RelatedType != null && domain.FilteredTypes.Contains(srchOp.RelatedType))
                {
                    var search = new CodeFile { Name = Util.TypescriptFileName(srchOp.Name) + ".tsx", Contents = GenerateFromTemplate(srchOp, TemplateNames.ReactSearchControl), RelativePath = GetRelativePathFromTypeName(srchOp.RelatedType.Name), Template = TemplateNames.ReactSearchControl};
                    files.Add(search);
                }
            }

            var appImports = new CodeFile { Name = "../App.tsx", Contents = GenerateFromTemplate(new DomainAdapter(domain), "AppImports"), IsFragment = true};
            files.Add(appImports);

            var appRoutes = new CodeFile { Name = "../App.tsx", Contents = GenerateFromTemplate(new DomainAdapter(domain), "AppRoutes"), IsFragment = true };
            files.Add(appRoutes);

            return files;
        }

        private string GenerateRegularUserHomepage(Domain domain)
        {
            return GenerateFromTemplate(new DomainApiAdapter(domain), "ReactRegularUserHome");
        }

        private string GenerateLeftNavItems(Domain domain)
        {
            return GenerateFromTemplate(new DomainApiAdapter(domain), "ReactLeftNavItems");
        }

        private string GenerateApiClient(ApplicationType type, Domain domain)
        {
            var adapter = new ClientApiAdapter(type, domain);
            try
            {
                return GenerateFromTemplate(adapter, TemplateNames.ApiClient);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error generating API client for {TypeName}", type.Name);
                throw;
            }
        }

        private string GenerateClientApiModel(OperationAdapter op)
        {
            return GenerateFromTemplate(op, TemplateNames.ApiClientModel);
        }

        private string GenerateClientApiResultType(SimpleType type)
        {
            return GenerateFromTemplate(type, TemplateNames.ApiClientResult);
        }

        private string GetRelativePathFromTypeName(string typeName)
        {
            return "domain\\" + Util.KebabCase(typeName) + "\\";
        }
    }

    public class TemplateNames
    {
        public const string ApiClient = "ApiClient";
        public const string ApiClientModel = "ApiClientModel";
        public const string ApiClientResult = "ApiClientResult";
        public const string ReactListPage = "ReactListPage";
        public const string ReactPagedListPage = "ReactPagedListPage";
        public const string ReactListHeader = "ReactListHeader";
        public const string ReactListRow = "ReactListRow";
        public const string ReactListRendering = "ReactListRendering";
        public const string ReactSelectControl = "ReactSelectControl";
        public const string ReactComponent = "ReactComponent";
        public const string ReactDetailPage = "ReactDetailPage";
        public const string ReactDetailDisplay = "ReactDetailDisplay";
        public const string ReactAddEditPage = "ReactAddEditPage";
        public const string ReactAddEditPageRendering = "ReactAddEditPageRendering";
        public const string ModelValidator = "ModelValidator";
        public const string ReactConfirmDeletePage = "ReactConfirmDeletePage";
        public const string ReactSearchControl = "ReactSearchControl";
    }
}
