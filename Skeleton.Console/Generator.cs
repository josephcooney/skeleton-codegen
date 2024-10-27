using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using Skeleton.OpenApi;
using Skeleton.ProjectGeneration;
using Skeleton.Model;
using Skeleton.Templating.Classes;
using Skeleton.Templating.DatabaseFunctions;
using Skeleton.Templating.ReactClient;
using Skeleton.Templating.TestData;
using Serilog;

namespace Skeleton.Console
{
    public class Generator
    {
        private readonly IFileSystem _fs;
        private readonly Settings _settings;
        private readonly FileWriter _fileWriter;

        private const string DbScriptsRelativePath = ".\\Database\\";

        public Generator(IFileSystem fileSystem, Settings settings, FileWriter fileWriter)
        {
            _fs = fileSystem;
            _settings = settings;
            _fileWriter = fileWriter;
        }

        private string DatabaseScriptsDirectory => _fs.Path.Combine(CSharpDataAccessDirectory, DbScriptsRelativePath);

        private string CSharpDataAccessDirectory
        {
            get
            {
                if (!string.IsNullOrEmpty(_settings.DataDirectory))
                {
                    return _fs.Path.Combine(_settings.RootDirectory, _settings.DataDirectory);
                }
                
                return _fs.Path.Combine(_settings.RootDirectory, "Data");
            }
        }
        
        private string CSharpDataAccessTestDirectory
        {
            get
            {
                if (!string.IsNullOrEmpty(_settings.TestDataDirectory))
                {
                    return _fs.Path.Combine(_settings.RootDirectory, _settings.TestDataDirectory);
                }
                
                return _fs.Path.Combine(_settings.RootDirectory, "Data\\Test");
            }
        }

        public void Generate(ITypeProvider typeProvider)
        {
            Log.Information("Starting Code Generation in {RootDirectory}", _settings.RootDirectory);
            var oldDomain = typeProvider.GetDomain(_settings);
            typeProvider.GetOperations(oldDomain);
            if (_settings.AddGeneratedOptionsToDatabase)
            {
                typeProvider.DropGenerated(oldDomain);
                Log.Information("Finishing dropping generated operations");
            }

            var domain = typeProvider.GetDomain(_settings);
            
            Log.Information("Finished building domain");

            SetupRootDirectory();

            GenerateDbFunctions(domain, _settings.AddGeneratedOptionsToDatabase, typeProvider);
            Log.Information("Finished generating db functions");
            
            typeProvider.GetOperations(domain);
            Log.Information("Finished adding operations to domain");

            SanityCheckDomain(domain);

            GenerateDbDropStatements(oldDomain, domain, typeProvider);

            GenerateClasses(domain);
            Log.Information("Finished generating classes");

            GenerateRepositories(domain);
            Log.Information("Finished generating repositories");

            if (_settings.GenerateTestRepos && !string.IsNullOrEmpty(_settings.TestDataDirectory))
            {
                // this is very much a work-in-progress
                GenerateTestRepositories(domain);
                Log.Information("Finished generating test repositories");
            }
            
            if (domain.ResultTypes.Any(rt => !rt.Ignore))
            {
                GenerateReturnTypes(domain);
                Log.Information("Finished generating return types");
            }
            
            if (_settings.WebUIType == WebUIType.React)
            {
                GenerateWebApi(domain);
                GenerateWebApiModels(domain);
                
                if (!string.IsNullOrEmpty(_settings.OpenApiUri))
                {
                    var openApiDocProvider = new OpenApiDocumentProvider(_fs, _settings);
                    var openApiDomainProvider = new OpenApiDomainProvider(openApiDocProvider);
                    openApiDomainProvider.AugmentDomainFromOpenApi(domain);
                }
                
                GenerateClientServiceProxy(domain);
                GenerateClientApiModels(domain);
                GenerateClientPages(domain);
                Log.Information("Finished generating react UI");
            }

            if (_settings.ClientAppTypes.Contains(ClientAppUIType.Flutter))
            {
                var flutterGen = new Flutter.Generator(_fs, _settings);
                var flutterFiles = flutterGen.Generate(domain);
                _fileWriter.ApplyCodeFiles(flutterFiles, flutterGen.RootDirectory);
                Log.Information("Finished generating flutter UI");
            }

            if (_settings.ClientAppTypes.Contains(ClientAppUIType.ReactNative))
            {
                var rnGenerator = new ReactNative.Generator(_fs, _settings);
                var files = rnGenerator.Generate(domain);
                _fileWriter.ApplyCodeFiles(files, rnGenerator.RootDirectory);
                Log.Information("Finished generating React Native UI");
            }

            if (_settings.TestDataSize != null && _settings.TestDataSize > 0)
            {
                var testDataGen = new TestDataGenerator();
                var testData = testDataGen.Generate(domain);
                if (testData.Any())
                {
                    typeProvider.AddTestData(testData);
                }
            }

            if (_settings.DbSquash)
            {
                SquashDbFilesIntoCurrentDbDirectory();
            }
            
            Log.Information("Finished Code Generation");
            
            
        }

        public List<string> GetRelativeSqlFileNamesForDirectory(string directoryName)
        {
            return _fs.Directory.GetFiles(directoryName, "*.sql", SearchOption.AllDirectories).Select(fn => String.Join(_fs.Path.DirectorySeparatorChar, fn.Split(_fs.Path.DirectorySeparatorChar).Reverse().Take(2).Reverse())).Distinct().ToList();
        }
        
        private void SquashDbFilesIntoCurrentDbDirectory()
        {
            var filterEx = new Regex("^[0-9]");
            // get distinct file names
            var fileNames = GetRelativeSqlFileNamesForDirectory(DatabaseScriptsDirectory);
            fileNames = fileNames.Where(fn => !(filterEx.IsMatch(fn))).ToList();
            
            var childDirectories = _fs.Directory.EnumerateDirectories(DatabaseScriptsDirectory).OrderByDescending(n => n);
            var currentHeadDirectory = childDirectories.First();
            var headFiles = GetRelativeSqlFileNamesForDirectory(currentHeadDirectory);
            fileNames.RemoveAll(f => headFiles.Contains(f));    
            
            // start looking backward from current directory to find files
            foreach (var directory in childDirectories.Skip(1))
            {
                var dirFiles = GetRelativeSqlFileNamesForDirectory(directory);
                var filesFound = new List<string>();
                
                foreach (var fileName in fileNames)
                {
                    // when found copy them in
                    if (dirFiles.Contains(fileName))
                    {
                        var source = _fs.Path.Combine(directory, fileName);
                        var target = _fs.Path.Combine(currentHeadDirectory, fileName);
                        Log.Information("Squashing file {Source} into {Target}", source, target);
                        _fs.File.Copy(source, target);
                        filesFound.Add(fileName);
                    }
                }

                fileNames.RemoveAll(fn => filesFound.Contains(fn));
            }
        }

        private void GenerateDbDropStatements(Domain oldDomain, Domain domain, ITypeProvider typeProvider)
        {
            var dropFile = typeProvider.GenerateDropStatements(oldDomain, domain);
            if (!string.IsNullOrEmpty(dropFile.Contents))
            {
                _fileWriter.ApplyDatabaseFiles(new List<CodeFile>(){dropFile}, DatabaseScriptsDirectory, null);   
            }
        }

        private void FilterDomainToSingleType(Domain domain)
        {
            var selectedType = domain.Types.FirstOrDefault(t => t.Name == _settings.TypeName);
            if (selectedType == null)
            {
                throw new InvalidOperationException("Unable to find the type you specified to operate on " +
                                                    _settings.TypeName);
            }

            domain.Types.Clear();
            domain.Types.Add(selectedType);
            var ops = domain.Operations.Where(o =>
                o.Attributes?.applicationtype == selectedType.Name || o.Returns.SimpleReturnType == selectedType);
            domain.Operations.Clear();
            domain.Operations.AddRange(ops);
        }

        private void SetupRootDirectory()
        {
            if (!_fs.Directory.Exists(_settings.RootDirectory))
            {
                _fs.Directory.CreateDirectory(_settings.RootDirectory);
            }
        }

        private void GenerateClasses(Domain domain)
        {
            var generator = new ClassGenerator();
            var files = generator.GenerateDomain(domain);
            if (!string.IsNullOrEmpty(_settings.DomainDirectory))
            {
                var domainDir = _fs.Path.Combine(_settings.RootDirectory, _settings.DomainDirectory);
                _fileWriter.ApplyCSharpFiles(files, domainDir);
            }
            else
            {
                const string DomainObjectDirectoryName = "Domain";
                var dir = _fs.Path.Combine(CSharpDataAccessDirectory, DomainObjectDirectoryName);
                _fileWriter.ApplyCSharpFiles(files, dir);
            }
        }

        private void GenerateReturnTypes(Domain domain)
        {
            var generator = new ClassGenerator();
            var files = generator.GenerateReturnTypes(domain);
            const string directoryName = "Model";
            var dir = _fs.Path.Combine(CSharpDataAccessDirectory, directoryName);
            _fileWriter.ApplyCSharpFiles(files, dir);
        }

        private void GenerateDbFunctions(Domain domain, bool addGeneratedOperationsToDatabase, ITypeProvider typeProvider)
        {
            var generator = new DbFunctionGenerator();
            var files = generator.Generate(domain, _settings);

            _fileWriter.ApplyDatabaseFiles(files, DatabaseScriptsDirectory, file =>
            {
                if (addGeneratedOperationsToDatabase)
                {
                    typeProvider.AddGeneratedOperation(file.Contents);
                }
            });
        }

        private void GenerateRepositories(Domain domain)
        {
            var generator = new ClassGenerator();
            var files = generator.GenerateRepositories(domain);

            var infra = generator.GenerateRepositoryInfrastructure(domain);
            files.AddRange(infra);

            const string RepoDirectoryName = "Repository";
            var path = _fs.Path.Combine(CSharpDataAccessDirectory, RepoDirectoryName);

            _fileWriter.ApplyCSharpFiles(files, path);
        }
        
        private void GenerateTestRepositories(Domain domain)
        {
            var generator = new ClassGenerator();
            var files = generator.GenerateTestRepositories(domain);
            
            const string RepoDirectoryName = "Repository";
            var path = _fs.Path.Combine(CSharpDataAccessTestDirectory, RepoDirectoryName);

            _fileWriter.ApplyCSharpFiles(files, path);
        }

        private void GenerateWebApi(Domain domain)
        {
            var generator = new ClassGenerator();
            var files = generator.GenerateWebApiControllers(domain);
            _fileWriter.ApplyCodeFiles(files, null);
        }

        private void GenerateWebApiModels(Domain domain)
        {
            var generator = new ClassGenerator();
            var files = generator.GenerateWebApiModels(domain);
            _fileWriter.ApplyCodeFiles(files, null);
        }
        
        private void GenerateClientServiceProxy(Domain domain)
        {
            var clientGenerator = new ReactClientGenerator();
            var files = clientGenerator.Generate(domain);
            _fileWriter.ApplyCodeFiles(files, _settings.ResolveClientAppDirectory());
        }

        private void GenerateClientApiModels(Domain domain)
        {
            var clientGenerator = new ReactClientGenerator();
            var files = clientGenerator.GenerateClientModels(domain);
            _fileWriter.ApplyCodeFiles(files, _settings.ResolveClientAppDirectory());
        }

        private void GenerateClientPages(Domain domain)
        {
            var generator = new ReactClientGenerator();
            var listPages = generator.GenerateComponents(domain);
            _fileWriter.ApplyCodeFiles(listPages, _settings.ResolveClientAppDirectory());
        }

        private void SanityCheckDomain(Domain domain)
        {
            if (domain.Types.Count == 0)
            {
                Log.Error("There are no types in domain");
            }

            if (domain.Operations.Count == 0)
            {
                Log.Error("There are no operations in domain");
            }

            foreach (var applicationType in domain.FilteredTypes)
            {
                if (applicationType.Fields.Count == 0)
                {
                    Log.Warning("Type {ApplicationType} has no fields", applicationType.Name);
                }

                if (applicationType.Fields.Any(f => string.IsNullOrEmpty(f.Name)))
                {
                    Log.Warning("Type {ApplicationType} has a field with no name", applicationType.Name);
                }

                if (string.IsNullOrEmpty(applicationType.Namespace))
                {
                    Log.Warning("Type {ApplicationType} has no namespace", applicationType.Name);
                }

                if (!applicationType.Ignore)
                {
                    var operations = domain.Operations.Where(o => o.Returns.SimpleReturnType == applicationType);
                    if (operations.Count() == 0)
                    {
                        Log.Warning("Type {ApplicationType} is not returned by any operations", applicationType.Name);
                    }
                }
            }

            foreach (var resultType in domain.ResultTypes)
            {
                if (resultType.Fields.Count == 0)
                {
                    Log.Warning("Result type {ResultType} has no fields", resultType.Name);
                }
                
                if (resultType.Fields.Any(f => string.IsNullOrEmpty(f.Name)))
                {
                    Log.Warning("Result type {ResultType} has a field with no name", resultType.Name);
                }

                var operations = domain.Operations.Where(o => o.Returns.SimpleReturnType == resultType);
                var parameters = domain.Operations.SelectMany(o => o.Parameters).Where(p =>
                    p.ClrType == typeof(ResultType) && p.ProviderTypeName == resultType.Name);
                if (operations.Count() == 0 && parameters.Count() == 0)
                {
                    Log.Warning("Result Type {ResultType} is not returned by, or used as a parameter in any operations", resultType.Name);
                }
            }

            foreach (var op in domain.Operations)
            {
                if (op.Returns == null)
                {
                    Log.Warning("Operation {OperationName} does not return anything", op.Name);
                }

                if (op.Parameters.Any(p => string.IsNullOrEmpty(p.Name)))
                {
                    Log.Warning("Operation {OperationName} has a parameter with no name", op.Name);
                }
            }
        }
    }
}
