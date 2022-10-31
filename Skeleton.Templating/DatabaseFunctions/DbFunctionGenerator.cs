using System;
using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;
using Skeleton.Model.Operations;
using Skeleton.Templating.DatabaseFunctions.Adapters;
using Serilog;
using Constraint = Skeleton.Model.Constraint;

namespace Skeleton.Templating.DatabaseFunctions
{
    public class DbFunctionGenerator
    {
        public const string SqlExtension = ".sql";
        public const string SelectAllForDisplayFunctionName = "select_all_for_display";
        public const string SelectForDisplayFunctionName = "select_for_display";
        public const string SearchFunctionName = "search";
        public const string InsertFunctionName = "insert";
        public const string UpdateFunctionName = "update";
        public const string DeleteOperationName = "delete";


        public List<CodeFile> Generate(Domain domain, Settings settings)
        {
            Util.RegisterHelpers(domain.TypeProvider);

            var files = new List<CodeFile>();

            var skipPolicyGeneration = domain.UserType == null;
            if (skipPolicyGeneration)
            {
                Log.Warning("Skipping security policy generation because no domain user type is defined");
            }
            
            foreach (var type in domain.FilteredTypes)
            {
                if (!type.Ignore)
                {
                    if (settings.GenerateSecurityPolicies && type.Attributes?.createPolicy != false && !skipPolicyGeneration)
                    {
                        files.Add(GenerateSecurityPoicy(type, domain));
                    }

                    var adapter = new DbTypeAdapter(type, UpdateFunctionName, OperationType.Update, domain);
                    if (domain.TypeProvider.GenerateCustomTypes && adapter.HasExcludedFields)
                    {
                        files.Add(GenerateResultType(type, domain));
                    }

                    if (adapter.UsesCustomInsertType && !domain.TypeProvider.CustomTypeExists(adapter.NewTypeName))
                    {
                        files.Add(GenerateInsertType(type, domain));
                    }
                    
                    files.Add(GenerateInsertFunction(type, domain));

                    if (domain.TypeProvider.GenerateCustomTypes)
                    {
                        files.Add(GenerateDisplayType(type, domain));
                    }
                    
                    files.Add(GenerateSelectAllFunction(type, domain));
                    files.Add(GenerateSelectAllForDisplayFunction(type, domain));

                    if (type.Paged)
                    {
                        files.Add(GeneratePagedOrderedSelectFunction(type, domain));
                        files.Add(GenerateSelectPagedForDisplayFunction(type, domain));
                    }
                    
                    if (adapter.UpdateFields.Any())
                    {
                        files.Add(GenerateUpdateFunction(adapter));
                    }

                    if (type.Fields.Count(f => f.IsKey) == 1)
                    {
                        var identityField = type.Fields.FirstOrDefault(f => f.IsKey);
                        if (identityField != null)
                        {
                            files.Add(GenerateSelectByPrimaryKeyFunction(type, identityField, domain));
                            files.Add(GenerateSelectAllForDisplayByRelatedTypeFunction(type, identityField, domain));
                        }
                    }

                    if (type.Fields.Any(f => f.ReferencesType != null))
                    {
                        foreach (var field in type.Fields.Where(f => f.ReferencesType != null))
                        {
                            files.Add(GenerateSelectByRelatedTypeFunction(type, field, domain));
                            files.Add(GenerateSelectAllForDisplayByRelatedTypeFunction(type, field, domain));
                            
                            if (type.Paged)
                            {
                                files.Add(GeneratePagedSelectByRelatedTypeFunction(type, field, domain));
                                files.Add(GeneratePagedSelectForDisplayByRelatedTypeFunction(type, field, domain));
                            }
                        }
                    }

                    if (type.Constraints.Any())
                    {
                        foreach (var constraint in type.Constraints)
                        {
                            files.Add(GenerateSelectByConstraint(type, constraint, domain));
                        }
                    }

                    if (type.DeleteType == DeleteType.Hard)
                    {
                        files.Add(GenerateDeleteFunction(type, domain));
                    }

                    if (type.DeleteType == DeleteType.Soft)
                    {
                        files.Add(GenerateSoftDeleteFunction(type, domain));
                    }

                    if (type.Fields.Any(f => f.IsSearch))
                    {
                        files.Add(GenerateSearchFunction(type, domain));
                    }

                    if (!type.IsSecurityPrincipal)
                    {
                        // find all the link types that reference this type
                        var linkTypesReferencingCurrentType = domain.FilteredTypes.Where(t => t.IsLink && t.Fields.Any(f => f.HasReferenceType && f.ReferencesType == type));
                        if (linkTypesReferencingCurrentType.Any())
                        {
                            foreach (var linkingType in linkTypesReferencingCurrentType)
                            {
                                var linkAdapter = new SelectForDisplayViaLinkDbTypeAdapter(type, SelectAllForDisplayFunctionName, linkingType, domain);
                                if (linkAdapter.LinkingTypeField != null && linkAdapter.LinkTypeOtherField != null)
                                {
                                    files.Add(GenerateTemplateFromAdapter(linkAdapter, "SelectAllForDisplayViaLinkTemplate"));
                                    
                                    if (type.Paged)
                                    {
                                        var pagedLinkAdapter = new SelectPagedForDisplayViaLinkDbTypeAdapter(type,
                                            SelectAllForDisplayFunctionName, linkingType, domain);
                                        files.Add(GenerateTemplateFromAdapter(pagedLinkAdapter, "SelectPagedForDisplayViaLink"));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return files;
        }

        private CodeFile GenerateTemplateFromAdapter(DbTypeAdapter adapter, string templateName)
        {
            try
            {
                return new CodeFile
                {
                    Name = adapter.FunctionName + SqlExtension,
                    Contents = Util.GetCompiledTemplateFromTypeProvider(templateName, adapter.Domain.TypeProvider)(adapter),
                    RelativePath = ".\\" + adapter.Name + "\\"
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unable to generate template {templateName} for type {adapter.Name}", ex);
            }
        }

        private CodeFile GenerateUpdateFunction(DbTypeAdapter adapter)
        {
            return GenerateTemplateFromAdapter(adapter, DbTemplates.Update);
        }

        private CodeFile GenerateSelectByRelatedTypeFunction(ApplicationType type, Field field, Domain domain)
        {
            var adapter = new SelectByFieldsDbTypeAdapter(type, $"select_by_{field.Name}", new List<Field> { field }, OperationType.Select, domain, false);
            return GenerateTemplateFromAdapter(adapter, "SelectByForeignKeyTemplate");
        }
        
        private CodeFile GeneratePagedSelectByRelatedTypeFunction(ApplicationType type, Field field, Domain domain)
        {
            var adapter = new SelectPagedByFieldsDbTypeAdapter(type, $"select_paged_by_{field.Name}", new List<Field> { field }, OperationType.Select, domain);
            return GenerateTemplateFromAdapter(adapter, "SelectPagedByForeignKey");
        }

        private CodeFile GenerateSelectAllForDisplayByRelatedTypeFunction(ApplicationType type, Field field, Domain domain)
        {
            var adapter = new SelectByFieldsForDisplayDbTypeAdapter(type, $"select_for_display_by_{field.Name}", new List<Field> { field }, domain);
            return GenerateTemplateFromAdapter(adapter, "SelectAllForDisplayByForeignKeyTemplate");
        }
        
        private CodeFile GeneratePagedSelectForDisplayByRelatedTypeFunction(ApplicationType type, Field field, Domain domain)
        {
            var adapter = new SelectPagedByFieldsForDisplayDbTypeAdapter(type, $"select_paged_for_display_by_{field.Name}", new List<Field> { field }, domain);
            return GenerateTemplateFromAdapter(adapter, "SelectPagedForDisplayByForeignKey");
        }

        private CodeFile GenerateSelectByPrimaryKeyFunction(ApplicationType type, Field field, Domain domain)
        {
            var adapter = new SelectByFieldsDbTypeAdapter(type, $"select_by_{field.Name}", new List<Field> {field}, OperationType.Select, domain, true);
            return GenerateTemplateFromAdapter(adapter, "SelectByForeignKeyTemplate");
        }

        private CodeFile GenerateSelectByConstraint(ApplicationType type, Constraint constraint, Domain domain)
        {
            var adapter = new SelectByFieldsDbTypeAdapter(type, $"select_by_{constraint.Name}", constraint.Fields, OperationType.Select, domain, false);
            return GenerateTemplateFromAdapter(adapter, "SelectByForeignKeyTemplate");
        }

        private CodeFile GenerateInsertFunction(ApplicationType applicationType, Domain domain)
        {
            var adapter = new DbTypeAdapter(applicationType, "insert", OperationType.Insert, domain);
            if (adapter.AddMany)
            {
                return GenerateTemplateFromAdapter(adapter, DbTemplates.InsertMany);
            }
            else
            {
                return GenerateTemplateFromAdapter(adapter, DbTemplates.Insert);
            }
        }

        private CodeFile GenerateSelectAllFunction(ApplicationType applicationType, Domain domain)
        {
            var adapter = new DbTypeAdapter(applicationType, "select_all", OperationType.Select, domain);
            return GenerateTemplateFromAdapter(adapter, "SelectAllTemplate");
        }

        private CodeFile GeneratePagedOrderedSelectFunction(ApplicationType applicationType, Domain domain)
        {
            var adapter = new PagedDbTypeAdapter(applicationType, OperationType.Select, domain);
            return GenerateTemplateFromAdapter(adapter, "SelectPaged");
        }
        
        private CodeFile GenerateDisplayType(ApplicationType type, Domain domain)
        {
            var adapter = new SelectForDisplayDbTypeAdapter(type, "display", domain);
            return GenerateTemplateFromAdapter(adapter, "DisplayType");
        }

        private CodeFile GenerateResultType(ApplicationType applicationType, Domain domain)
        {
            var adapter = new DbTypeAdapter(applicationType, "result", OperationType.None, domain);
            return GenerateTemplateFromAdapter(adapter, "ResultType");
        }
        
        private CodeFile GenerateInsertType(ApplicationType applicationType, Domain domain)
        {
            var adapter = new DbTypeAdapter(applicationType, "new", OperationType.Insert, domain);
            return GenerateTemplateFromAdapter(adapter, "InsertType");
        }

        private CodeFile GenerateSelectAllForDisplayFunction(ApplicationType applicationType, Domain domain)
        {
            var adapter = new SelectForDisplayDbTypeAdapter(applicationType, SelectAllForDisplayFunctionName, domain);
            return GenerateTemplateFromAdapter(adapter, "SelectAllForDisplayTemplate");
        }
        
        private CodeFile GenerateSelectPagedForDisplayFunction(ApplicationType applicationType, Domain domain)
        {
            var adapter = new SelectPagedForDisplayDbTypeAdapter(applicationType, domain);
            return GenerateTemplateFromAdapter(adapter, "SelectPagedForDisplay");
        }

        private CodeFile GenerateSearchFunction(ApplicationType applicationType, Domain domain)
        {
            var adapter = new SelectForDisplayDbTypeAdapter(applicationType, SearchFunctionName, domain);
            return GenerateTemplateFromAdapter(adapter, "SearchTemplate");
        }

        private CodeFile GenerateDeleteFunction(ApplicationType applicationType, Domain domain)
        {
            var adapter = new DbTypeAdapter(applicationType, DeleteOperationName, OperationType.Delete, domain);
            return GenerateTemplateFromAdapter(adapter, "DeleteTemplate");
        }

        private CodeFile GenerateSoftDeleteFunction(ApplicationType type, Domain domain)
        {
            var adapter = new DbTypeAdapter(type, DeleteOperationName, OperationType.Delete, domain);
            return GenerateTemplateFromAdapter(adapter, DbTemplates.DeleteSoft);
        }

        private CodeFile GenerateSecurityPoicy(ApplicationType type, Domain domain)
        {
            var adapter = new SecureDbTypeAdapter(type, domain);
            return new CodeFile
            {
                Name = type.Name + "_policy" + SqlExtension,
                Contents = Util.GetCompiledTemplate(DbTemplates.SecurityPolicy)(adapter),
                RelativePath = type.Name
            };
        }

        private static class DbTemplates
        {
            public const string Insert = "InsertTemplate";
            public const string InsertMany = "InsertManyTemplate";
            public const string Update = "UpdateTemplate";
            public const string SecurityPolicy = "SecurityPolicyTemplate";
            public const string DeleteSoft = "DeleteSoftTemplate";
        }
    }
}