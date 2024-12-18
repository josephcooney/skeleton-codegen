﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Skeleton.Model;
using Skeleton.Templating.Classes.Adapters;
using Skeleton.Templating.Classes.Repository;
using Skeleton.Templating.Classes.WebApi;
using Serilog;

namespace Skeleton.Templating.Classes
{
    public class ClassGenerator : GeneratorBase
    {
        public List<CodeFile> GenerateDomain(Domain domain)
        {
            Util.RegisterHelpers(domain);
            var files = new List<CodeFile>();

            foreach (var type in domain.Types)
            {
                if (!type.Ignore)
                {
                    var file = new CodeFile { Name = Util.CSharpNameFromName(type.Name) + ".cs", Contents = GenerateCode(type, domain) };
                    files.Add(file);
                }
            }

            return files;
        }

        public List<CodeFile> GenerateRepositories(Domain domain)
        {
            Util.RegisterHelpers(domain);
            var files = new List<CodeFile>();

            foreach (var type in domain.Types.OrderBy(t => t.Name))
            {
                if (!type.Ignore)
                {
                    var repo = new RepositoryAdapter(domain, type);
                    if (repo.Operations.Any())
                    {
                        var file = new CodeFile { Name = Util.CSharpNameFromName(type.Name) + "Repository.cs", Contents = GenerateRepo(repo, domain.TypeProvider) };
                        files.Add(file);
                    }
                }
            }

            return files;
        }
        
        public List<CodeFile> GenerateTestRepositories(Domain domain)
        {
            Util.RegisterHelpers(domain);
            var files = new List<CodeFile>();

            foreach (var type in domain.Types.OrderBy(t => t.Name))
            {
                if (!type.Ignore)
                {
                    var repo = new RepositoryAdapter(domain, type);
                    if (repo.Operations.Any())
                    {
                        var file = new CodeFile { Name = Util.CSharpNameFromName(type.Name) + "InMemoryRepository.cs", Contents = GenerateTestRepo(repo) };
                        files.Add(file);
                    }
                }
            }

            return files;
        }

        public List<CodeFile> GenerateRepositoryInfrastructure(Domain domain)
        {
            Util.RegisterHelpers(domain);
            var files = new List<CodeFile>();

            var file = new CodeFile { Name = "RepositoryBase.cs", Contents = GenerateRepoBase(domain) };
            files.Add(file);

            var module = new CodeFile { Name = "RepositoryModule.cs", Contents = GeneratRepositoryeModule(domain) };
            files.Add(module);

            return files;
        }

        public List<CodeFile> GenerateReturnTypes(Domain domain)
        {
            Util.RegisterHelpers(domain);
            var files = new List<CodeFile>();

            foreach (var type in domain.ResultTypes)
            {
                if (!type.Ignore)
                {
                    var file = new CodeFile { Name = Util.CSharpNameFromName(type.Name) + ".cs", Contents = GenerateReturnType(type, domain) };
                    files.Add(file);
                }
            }

            return files;
        }
        
        public List<CodeFile> GenerateWebApiControllers(Domain domain)
        {
            Util.RegisterHelpers(domain);
            var files = new List<CodeFile>();

            foreach (var type in domain.Types)
            {
                if (type.GenerateApi)
                {
                    var file = new CodeFile { Name = Util.CSharpNameFromName(type.Name) + "ApiController.cs", Contents = GenerateApiController(type, domain), RelativePath = FormatRelativePath(type, domain.Settings.ControllerDirectory) };
                    files.Add(file);
                }
            }

            return files;
        }

        
        public List<CodeFile> GenerateWebApiModels(Domain domain)
        {
            Util.RegisterHelpers(domain);
            var files = new List<CodeFile>();

            foreach (var type in domain.Types)
            {
                if (type.GenerateApi)
                {
                    var adapter = new ClassAdapter(type, domain);
                    if (adapter.Operations.Any())
                    {
                        foreach (var op in adapter.Operations)
                        {
                            if (op.UsesModel)
                            {
                                var file = new CodeFile { Name = Util.CSharpNameFromName(op.Name) + "Model.cs", Contents = GenerateApiModel(op), RelativePath = FormatRelativePath(type, domain.Settings.ModelDirectory) };
                                files.Add(file);
                            } else if (op.ChangesData && op.RelatedType.IsAttachment)
                            {
                                // generate special model for attachments, since the 
                                var file = new CodeFile { Name = Util.CSharpNameFromName(op.Name) + "Model.cs", Contents = GenerateAttachmentApiModel(op), RelativePath = FormatRelativePath(type, domain.Settings.ModelDirectory) };
                                files.Add(file);
                            }
                        }
                    }
                }
            }

            return files;
        }

        private string GenerateCode(ApplicationType applicationType, Domain domain)
        {
            return Util.GetCompiledTemplate("Class", Assembly.GetExecutingAssembly())(new ClassAdapter(applicationType, domain));
        }

        private string GenerateReturnType(SimpleType simpleType, Domain domain)
        {
            return Util.GetCompiledTemplateFromTypeProvider("ReturnType", domain.TypeProvider)(new ClassAdapter(simpleType, domain));
        }

        private string GenerateRepo(RepositoryAdapter adapter, ITypeProvider typeProvider)
        {
            var templateFunction =  Util.GetCompiledTemplateFromTypeProvider("Repository", typeProvider);
            try
            {
                return templateFunction(adapter);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing repository template for {TypeName}", adapter.Type.Name);
                throw;
            }
        }
        
        private string GenerateTestRepo(RepositoryAdapter adapter)
        {
            var templateFunction = Util.GetCompiledTemplate("TestRepository", Assembly.GetExecutingAssembly());
            try
            {
                return templateFunction(adapter);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing test repository template for {TypeName}", adapter.Type.Name);
                throw;
            }
        }
        private string GenerateRepoBase(Domain dom)
        {
            var templateFunction =  Util.GetCompiledTemplateFromTypeProvider("RepositoryBase", dom.TypeProvider);
            return templateFunction(dom);
        }

        private string GeneratRepositoryeModule(Domain domain)
        {
            var templateFunction =  Util.GetCompiledTemplateFromTypeProvider("RepositoryModule", domain.TypeProvider);
            return templateFunction(new DomainAdapter(domain));
        }

        private string GenerateController(ApplicationType applicationType, Domain domain)
        {
            return Util.GetCompiledTemplate("Controller", Assembly.GetExecutingAssembly())(new ControllerAdapter(applicationType, domain));
        }

        private string GenerateApiController(ApplicationType applicationType, Domain domain)
        {
            try
            {
                if (applicationType.IsAttachment)
                {
                    return Util.GetCompiledTemplate("ApiAttachmentController", Assembly.GetExecutingAssembly())(
                        new AttachmentControllerAdapter(applicationType, domain));
                }
                else
                {
                    return Util.GetCompiledTemplate("ApiController", Assembly.GetExecutingAssembly())(new ControllerAdapter(applicationType, domain));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unexpected error generating Api controller for application type {applicationType.Name}", ex);
                throw;
            }
        }

        private string GenerateApiModel(OperationAdapter operation)
        {
            return Util.GetCompiledTemplate("ApiRequestModel", Assembly.GetExecutingAssembly())(operation);
        }
        
        private string GenerateAttachmentApiModel(OperationAdapter operation)
        {
            return Util.GetCompiledTemplate("ApiAttachmentRequestModel", Assembly.GetExecutingAssembly())(operation.CustomType);
        }
        
        public override List<CodeFile> Generate(Domain domain)
        {
            return GenerateDomain(domain);
        }

        public override Assembly Assembly { get; }
    }
}
