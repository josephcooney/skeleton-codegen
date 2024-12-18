﻿using System.Collections.Generic;
using System.IO.Abstractions;
using Skeleton.Model.NamingConventions;

namespace Skeleton.Model
{
    public class Settings
    {
        private readonly IFileSystem _fs;
        private string _configurationFile;
        private const string JsonExtension = ".codegen.json";

        public Settings(IFileSystem fs)
        {
            _fs = fs;
            ConfigurationFile = "codegen.json";
            ClientAppTypes = new List<ClientAppUIType>();
            GenerateSecurityPolicies = true;
            ExcludedSchemas = new List<string>();
            GenerateTestRepos = false;
            DatabaseType = DatabaseType.Postgres;
            UseDbRoleForSecurity = true;
        }

        public string ConfigurationFile
        {
            get { return _configurationFile; }
            set
            {
                _configurationFile = value;

                if (!_fs.File.Exists(_configurationFile))
                {
                    if (!_configurationFile.ToLowerInvariant().EndsWith(JsonExtension))
                    {
                        _configurationFile += JsonExtension;
                    }    
                }
            }
        }
        public string RootDirectory { get; set; }
        public int Verbosity { get; set; } 
        public bool ShowHelp { get; set; }
        public bool AddGeneratedOptionsToDatabase { get; set; }
        public string ApplicationName { get; set; }
        public string DataDirectory { get; set; }
        
        public string DomainDirectory { get; set; }

        public string ControllerDirectory { get; set; } = "Controllers";

        public string ModelDirectory { get; set; } = "Models";
        
        public string DomainNamespace { get; set; }
        
        public string ClientAppDirectory { get; set; }
        public WebUIType WebUIType { get; set; }
        public string TypeName { get; set; } // used for debugging purposes to do things for just a single type
        public bool Debug { get; set; }
        public string ConnectionString { get; set; }
        
        public DatabaseType DatabaseType { get; set; }
        public bool DeleteGeneratedFiles { get; set; }  
        
        public string OpenApiUri { get; set; }

        public List<ClientAppUIType> ClientAppTypes { get; }
        
        public bool GenerateSecurityPolicies { get; set; }
        
        public List<string> ExcludedSchemas { get; }
        
        public bool GenerateTestRepos { get; set; }
            
        public string TestDataDirectory { get; set; }
        
        public int? TestDataSize { get; set; }
        
        public string AdminRoleName { get; set; }
        
        public bool DbSquash { get; set; }
        
        public bool UseDbRoleForSecurity { get; set; }
        
        public NamingConventionSettings NamingConventionSettings { get; set; }

        public string ResolveClientAppDirectory() => !string.IsNullOrEmpty(ClientAppDirectory)
            ? ClientAppDirectory
            : @"ClientApp\src\components\";

        public FlutterSettings FlutterSettings { get; set; }
        
        public ReactNativeSettings ReactNativeSettings { get; set; }
    }

    public class FlutterSettings
    {
        public string FlutterRootDirectory { get; set; }
    }

    public class ReactNativeSettings
    {
        public string RootDirectory { get; set; }
    }
    
    public enum WebUIType
    {
        Unknown,
        MVC,
        React
    }

    public enum ClientAppUIType
    {
        Flutter,
        ReactNative
    }

    public enum DatabaseType
    {
        Postgres = 0,
        SqlServer = 1
    }
}