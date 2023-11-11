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
        public static readonly string[] SelectAllForDisplayFunctionName = new []{"select", "all", "for", "display"};
        public static readonly string[] SelectForDisplayFunctionName = new []{"select", "for", "display"};
        public const string SearchFunctionName = "search";
        public const string InsertFunctionName = "insert";
        public const string UpdateFunctionName = "update";
        public const string DeleteOperationName = "delete";


        public List<CodeFile> Generate(Domain domain, Settings settings)
        {
            Util.RegisterHelpers(domain);

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
                        files.Add(GenerateSecurityPolicy(type, domain));
                    }

                    var adapter = new DbTypeAdapter(type, new []{UpdateFunctionName}, OperationType.Update, domain);
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
                    
                    if (adapter.UpdateFields.Any() && !adapter.UnderlyingType.IsLink) // for linking types the insert operation is more of a logical "upsert"
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

        private CodeFile GenerateTemplateFromAdapter(DbTypeAdapter adapter, string templateName, string namePrefix = null)
        {
            try
            {
                return new CodeFile
                {
                    Name = namePrefix + adapter.FunctionName + SqlExtension,
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
            var adapter = new SelectByFieldsDbTypeAdapter(type, new []{"select", "by", field.Name}, new List<Field> { field }, OperationType.Select, domain, false);
            return GenerateTemplateFromAdapter(adapter, "SelectByForeignKeyTemplate");
        }
        
        private CodeFile GeneratePagedSelectByRelatedTypeFunction(ApplicationType type, Field field, Domain domain)
        {
            var adapter = new SelectPagedByFieldsDbTypeAdapter(type, new []{"select", "paged", "by", field.Name}, new List<Field> { field }, OperationType.Select, domain);
            return GenerateTemplateFromAdapter(adapter, "SelectPagedByForeignKey");
        }

        private CodeFile GenerateSelectAllForDisplayByRelatedTypeFunction(ApplicationType type, Field field, Domain domain)
        {
            var adapter = new SelectByFieldsForDisplayDbTypeAdapter(type, new []{"select", "for", "display", "by", field.Name}, new List<Field> { field }, domain);
            return GenerateTemplateFromAdapter(adapter, "SelectAllForDisplayByForeignKeyTemplate");
        }
        
        private CodeFile GeneratePagedSelectForDisplayByRelatedTypeFunction(ApplicationType type, Field field, Domain domain)
        {
            var adapter = new SelectPagedByFieldsForDisplayDbTypeAdapter(type, new []{"select", "paged", "for", "display", "by", field.Name}, new List<Field> { field }, domain);
            return GenerateTemplateFromAdapter(adapter, "SelectPagedForDisplayByForeignKey");
        }

        private CodeFile GenerateSelectByPrimaryKeyFunction(ApplicationType type, Field field, Domain domain)
        {
            var adapter = new SelectByFieldsDbTypeAdapter(type, new []{"select", "by", field.Name}, new List<Field> {field}, OperationType.Select, domain, true);
            return GenerateTemplateFromAdapter(adapter, "SelectByForeignKeyTemplate");
        }

        private CodeFile GenerateSelectByConstraint(ApplicationType type, Constraint constraint, Domain domain)
        {
            var adapter = new SelectByFieldsDbTypeAdapter(type, new []{"select", "by", constraint.Name}, constraint.Fields, OperationType.Select, domain, false);
            return GenerateTemplateFromAdapter(adapter, "SelectByForeignKeyTemplate");
        }

        private CodeFile GenerateInsertFunction(ApplicationType applicationType, Domain domain)
        {
            var adapter = new DbTypeAdapter(applicationType, new []{"insert"}, OperationType.Insert, domain);
            if (adapter.AddMany)
            {
                return GenerateTemplateFromAdapter(adapter, DbTemplates.InsertMany);
            }
            else
            {
                if (adapter.UnderlyingType.IsLink)
                {
                    return GenerateTemplateFromAdapter(adapter, DbTemplates.UpsertLink);
                }
                else
                {
                    return GenerateTemplateFromAdapter(adapter, DbTemplates.Insert);
                }
            }
        }

        private CodeFile GenerateSelectAllFunction(ApplicationType applicationType, Domain domain)
        {
            var adapter = new DbTypeAdapter(applicationType, new []{"select", "all"}, OperationType.Select, domain);
            return GenerateTemplateFromAdapter(adapter, "SelectAllTemplate");
        }

        private CodeFile GeneratePagedOrderedSelectFunction(ApplicationType applicationType, Domain domain)
        {
            var adapter = new PagedDbTypeAdapter(applicationType, OperationType.Select, domain);
            return GenerateTemplateFromAdapter(adapter, "SelectPaged");
        }
        
        private CodeFile GenerateDisplayType(ApplicationType type, Domain domain)
        {
            var adapter = new SelectForDisplayDbTypeAdapter(type, new []{"display"} , domain);
            return GenerateTemplateFromAdapter(adapter, "DisplayType", "_"); // underscore prefix allows fine-tuning of order that scripts are executed by dbup
        }

        private CodeFile GenerateResultType(ApplicationType applicationType, Domain domain)
        {
            var adapter = new DbTypeAdapter(applicationType, new []{"result"} , OperationType.None, domain);
            return GenerateTemplateFromAdapter(adapter, "ResultType", "_");
        }
        
        private CodeFile GenerateInsertType(ApplicationType applicationType, Domain domain)
        {
            var adapter = new DbTypeAdapter(applicationType, new []{"new"}, OperationType.Insert, domain);
            return GenerateTemplateFromAdapter(adapter, "InsertType", "_");
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
            var adapter = new SelectForDisplayDbTypeAdapter(applicationType, new []{SearchFunctionName}, domain);
            return GenerateTemplateFromAdapter(adapter, "SearchTemplate");
        }

        private CodeFile GenerateDeleteFunction(ApplicationType applicationType, Domain domain)
        {
            var adapter = new DbTypeAdapter(applicationType, new []{DeleteOperationName}, OperationType.Delete, domain);
            return GenerateTemplateFromAdapter(adapter, "DeleteTemplate");
        }

        private CodeFile GenerateSoftDeleteFunction(ApplicationType type, Domain domain)
        {
            var adapter = new DbTypeAdapter(type, new []{DeleteOperationName}, OperationType.Delete, domain);
            return GenerateTemplateFromAdapter(adapter, DbTemplates.DeleteSoft);
        }

        private CodeFile GenerateSecurityPolicy(ApplicationType type, Domain domain)
        {
            var adapter = new SecureDbTypeAdapter(type, domain);
            return new CodeFile
            {
                Name = type.Name + "_policy" + SqlExtension,
                Contents = Util.GetCompiledTemplateFromTypeProvider(DbTemplates.SecurityPolicy, domain.TypeProvider)(adapter),
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
            public const string UpsertLink = "UpsertLink";
        }
    }
}