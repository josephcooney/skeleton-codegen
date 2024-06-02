using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using Skeleton.Postgres;
using Skeleton.ProjectGeneration;
using Skeleton.Model;
using Skeleton.SqlServer;
using Microsoft.Extensions.Configuration;
using Mono.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Skeleton.Model.NamingConventions;

namespace Skeleton.Console
{
    class Program
    {
        private static IFileSystem _fileSystem = new FileSystem();

        static void Main(string[] args)
        {
            WriteLogo();
            var levelSwitch = new LoggingLevelSwitch();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console().MinimumLevel.ControlledBy(levelSwitch)
                .CreateLogger();
            
            var settings = ParseArguments(args);
            if (settings == null)
            {
                return;
            }

            CopySettingsFiles();
            var currentDir = _fileSystem.Directory.GetCurrentDirectory();
            var settingsFilePath = _fileSystem.Path.Combine(currentDir, settings.ConfigurationFile);
            if (!_fileSystem.File.Exists(settingsFilePath))
            {
                Log.Error("Configuration file {} could not be found, check the file name", settingsFilePath);
            }

            var builder = new ConfigurationBuilder()
                .SetBasePath(currentDir)
                .AddJsonFile(settings.ConfigurationFile, optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            if (!UpdateSettingsFromConfiguration(settings, configuration))
            {
                return;
            }
            
            if (settings.Debug)
            {
                if (!Debugger.IsAttached)
                {
                    Debugger.Launch();
                }
            }

            if (settings.Verbosity > 0)
            {
                levelSwitch.MinimumLevel = LogEventLevel.Verbose;
            }
            
            var provider = CreateTypeProvider(settings);
            var fileSystem = new FileSystem();
            var writer = new FileWriter(fileSystem, settings.RootDirectory);
            var generator = new Generator(fileSystem, settings, writer);

            if (settings.DeleteGeneratedFiles)
            {
                var cleaner = new GeneratedFileCleaner(fileSystem, settings);
                cleaner.ClearGeneratedFiles();
            }

            generator.Generate(provider);
        }

        private static void CopySettingsFiles()
        {
            // copy settings files from 'root' project dir into \bin\debug\ so you don't need to
            var parentPath = _fileSystem.Path.Combine(_fileSystem.Directory.GetCurrentDirectory(), "../../../");
            var files = _fileSystem.Directory.GetFiles(parentPath, "*.codegen.json");
            foreach (var file in files)
            {
                var fileInfo = _fileSystem.FileInfo.New(file);
                var target = _fileSystem.Path.Combine(_fileSystem.Directory.GetCurrentDirectory(), fileInfo.Name);
                _fileSystem.File.Copy(file, target, true);
                Log.Information("Copied {ConfigFile} to {UpdatedLocation}", file, target);
            }
        }

        private static ITypeProvider CreateTypeProvider(Settings settings)
        {
            switch (settings.DatabaseType)
            {
                case DatabaseType.Postgres:
                    return new PostgresTypeProvider(settings.ConnectionString);
                case DatabaseType.SqlServer:
                    return new SqlServerTypeProvider(settings.ConnectionString);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void WriteLogo()
        {
            var logo = @"
                     *&@@@@@@@@@@@@@@@@(.                   
                 /@@@@#,              /@@@@&                
              .@@@%                       *@@@(             
             @@@*                            &@@.           
            @@@                               (@@*          
           (@@.                                &@@          
           #@@     %@@@@@(          @@@@@@.    %@@          
           (@@.   #@@@@@@@#        @@@@@@@@.   &@@          
            @@@   (@@@@@@@/        @@@@@@@@   *@@(          
             @@@,   &@@@&     &*    ,@@@@*   %@@*           
              @@&            @@@/           *@@*            
              @@@           @@@@@*          /@@,            
              ,@@%                         ,@@@             
                #@@@@@@               (@@@@@@.              
                     *@@@   @*  &/  &@@&                    
                      *@@@&&@@&&@@&&@@@                     
                         **, ,** ,**                                                                        
";
            System.Console.WriteLine(logo);
            System.Console.WriteLine("Skeleton");
        }

        private static Settings ParseArguments(string[] args)
        {
            var s = new Settings(_fileSystem);
            s.WebUIType = WebUIType.React;

            var os = new OptionSet() {
                "Usage: dotnet Skeleton.Console.dll -r VALUE <Options>",
                "",
                "Options:",
                { "adm|admin-role=", "Name of Admin role. Defaults to 'admin'", r => s.AdminRoleName = r },
                { "c|config=", "JSON configuration file to use.", c => s.ConfigurationFile = c },
                { "data-dir|database-code-directory=", "the root directory to generate database code into.", m => s.DataDirectory = m },
                { "data-test-dir|database-test-directory=", "the root directory to generate database test helpers into.", m => s.TestDataDirectory = m },
                { "client-dir|client-code-directory=", "the directory to generate client code into.", m => s.ClientAppDirectory = m },
                { "dbg|debug", "Attach Debugger on start", d => s.Debug = d != null },
                { "db-squash", "Consolidate db files into current directory - useful if template changes have resulted in inadvertent drops", d => s.DbSquash = d != null },
                { "del", "delete generated files before re-generating", d => s.DeleteGeneratedFiles = d != null},
                { "flutter", "Generate a Flutter client for application", f => {if (f != null) s.ClientAppTypes.Add(ClientAppUIType.Flutter); } },
                { "h|?|help",  "show this message and exit", h => s.ShowHelp = h != null },
                { "name=", "Name of the application. Used for default C# namespace for generated items", n => s.ApplicationName = n },
                { "no-policy", "Globally disable generation of security policies", p => { if (p != null) s.GenerateSecurityPolicies = false; }  },
                { "no-test-repo", "Disable generation of test repositories", t => { if (t != null) s.GenerateTestRepos = false; }},
                { "r|root=", "the root directory to generate code into.", r => s.RootDirectory = r },
                { "react", "Set the web UI generated to be React", r => {if (r != null) s.WebUIType = WebUIType.React; } },
                { "react-native", "Generate a React Native client for application", r => {if (r != null) s.ClientAppTypes.Add(ClientAppUIType.ReactNative); } },
                { "test-data=", "Generate test data of the specified size for empty tables.", t => s.TestDataSize = int.Parse(t) },
                { "t|type=", "Only generate for a single type (for debugging)", t => s.TypeName = t },
                { "u|update-db-operations",  "Update database with generated operations", u => s.AddGeneratedOptionsToDatabase = u != null },
                { "v", "increase debug message verbosity", v => { if (v != null) ++s.Verbosity; } },
                { "x|exclude=", "Exclude schema", x => s.ExcludedSchemas.Add(x) },
            };

            var extra = os.Parse(args);
            if (extra.Any())
            {
                var message = string.Join(" ", extra.ToArray());
                Log.Warning("There were some un-recognized command-line arguments: " + message);
            }

            if (s.ShowHelp)
            {
                os.WriteOptionDescriptions(System.Console.Out);
                return null;
            }

            return s;
        }

        private static bool UpdateSettingsFromConfiguration(Settings settings, IConfigurationRoot configuration)
        {
            settings.ConnectionString = configuration.GetConnectionString("application-db");
            if (string.IsNullOrEmpty(settings.ConnectionString))
            {
                Log.Error("Connection string has not been configured. Provide an entry in the codegen.json");
                return false;
            }

            var dbType = configuration.GetValue<string>("db-type");
            if (!string.IsNullOrEmpty(dbType))
            {
                switch (dbType.ToLowerInvariant())
                {
                    case "sql-server":
                        settings.DatabaseType = DatabaseType.SqlServer;
                        break;
                    
                    case "postgres":
                        settings.DatabaseType = DatabaseType.Postgres;
                        break;
                    
                    default:
                        Log.Error("Unrecognised database type {DatabaseType}", dbType);
                        return false;
                }
            }
            
            if (string.IsNullOrEmpty(settings.RootDirectory))
            {
                settings.RootDirectory = configuration.GetValue<string>("root");
                if (string.IsNullOrEmpty(settings.RootDirectory))
                {
                    Log.Error("root directory has not been specified. You can provide an entry for 'root' in the codegen.json, or provide one using the -r command-line argument");
                    return false;
                }
            }

            if (string.IsNullOrEmpty(settings.ApplicationName))
            {
                // this setting is not required, so should not trigger an error
                settings.ApplicationName = configuration.GetValue<string>("name");
            }

            if (string.IsNullOrEmpty(settings.DataDirectory))
            {
                // this setting is not required, so should not trigger an error
                settings.DataDirectory = configuration.GetValue<string>("data-dir");
            }

            // optional setting for location of domain objects, helpful for EF co-existence
            var domainDir = configuration.GetValue<string>("domain-dir");
            if (!string.IsNullOrEmpty(domainDir))
            {
                settings.DomainDirectory = domainDir;
            }
            
            // optional setting for location of controllers
            var controllerDir = configuration.GetValue<string>("controller-dir");
            if (!string.IsNullOrEmpty(controllerDir))
            {
                settings.ControllerDirectory = controllerDir;
            }

            var modelDir = configuration.GetValue<string>("model-dir");
            if (!string.IsNullOrEmpty(modelDir))
            {
                settings.ModelDirectory = modelDir;
            }

            // also for ef-coexistence
            var domainNamespace = configuration.GetValue<string>("domain-namespace");
            if (!string.IsNullOrEmpty(domainNamespace))
            {
                settings.DomainNamespace = domainNamespace;
            }
            
            if (string.IsNullOrEmpty(settings.ClientAppDirectory))
            {
                // this setting is not required so should also not trigger an error
                settings.ClientAppDirectory = configuration.GetValue<string>("client-dir");
            }

            if (string.IsNullOrEmpty(settings.TestDataDirectory))
            {
                settings.TestDataDirectory = configuration.GetValue<string>("data-test-dir");
            }
            
            if (string.IsNullOrEmpty(settings.OpenApiUri))
            {
                settings.OpenApiUri = configuration.GetValue<string>("openapi-uri");
            }
            
            var namingConventions = configuration.GetSection("NamingConventions").Get<NamingConventionSettings>();
            if (namingConventions != null)
            {
                settings.NamingConventionSettings = namingConventions;
            }
            
            if (settings.ClientAppTypes.Contains(ClientAppUIType.Flutter))
            {
                settings.FlutterSettings = new FlutterSettings();
                configuration.GetSection("FlutterSettings").Bind(settings.FlutterSettings);
            }

            if (settings.ClientAppTypes.Contains(ClientAppUIType.ReactNative))
            {
                settings.ReactNativeSettings = new ReactNativeSettings();
                configuration.GetSection("ReactNativeSettings").Bind(settings.ReactNativeSettings);
            }
            
            return true;
        }
    }
}
