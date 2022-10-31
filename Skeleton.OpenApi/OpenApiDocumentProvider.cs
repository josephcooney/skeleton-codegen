using System.IO.Abstractions;
using System.Net;
using Skeleton.Model;
using NSwag;
using Serilog;

namespace Skeleton.OpenApi
{
    public class OpenApiDocumentProvider : IOpenApiDocumentProvider
    {
        private readonly IFileSystem _fileSystem;
        private readonly Settings _settings;

        public OpenApiDocumentProvider(IFileSystem fileSystem, Settings settings)
        {
            _fileSystem = fileSystem;
            _settings = settings;
        }
        
        public OpenApiDocument GetOpenApiDocument()
        {
            Log.Information("Fetching OpenApi information");

            string openApiDocText = null;
            
            using (var webClient = new System.Net.WebClient())
            {
                try
                {
                    openApiDocText = webClient.DownloadString(_settings.OpenApiUri);
                    var doc = OpenApiDocument.FromJsonAsync(openApiDocText).Result;
                    if (!string.IsNullOrEmpty(_settings.ConfigurationFile))
                    {
                        SaveOfflineVersionOfOpenApi(_settings, _fileSystem, openApiDocText);
                    }

                    return doc;
                }
                catch (WebException)
                {
                    var fallbackDoc = LoadCachedOpenApiDoc(_settings, _fileSystem);
                    return fallbackDoc;
                }
            }    
        }
        
        private OpenApiDocument LoadCachedOpenApiDoc(Settings settings, IFileSystem fileSystem)
        {
            if (!string.IsNullOrEmpty(settings.ConfigurationFile))
            {
                var openApiCacheFile = GetOpenApiCacheFileName(settings);
                if (fileSystem.File.Exists(openApiCacheFile))
                {
                    var fileContents = fileSystem.File.ReadAllText(openApiCacheFile);
                    var doc = OpenApiDocument.FromJsonAsync(fileContents).Result;
                    return doc;
                }
            }

            Log.Error("Your app seems to be off-line - the OpenApi spec could not be retried from {OpenApiUri}, and no cached copy of the OpenApi specification for your app could be found.", settings.OpenApiUri);
            return null;
        }
        
        private string GetOpenApiCacheFileName(Settings settings)
        {
            return settings.ConfigurationFile.Replace(".json", ".openApi.json");
        }

        private void SaveOfflineVersionOfOpenApi(Settings settings, IFileSystem fileSystem, string openApiDocText)
        {
            var cacheFileName = GetOpenApiCacheFileName(settings);
            if (fileSystem.File.Exists(cacheFileName))
            {
                fileSystem.File.Delete(cacheFileName);
            }
            fileSystem.File.WriteAllText(cacheFileName, openApiDocText);
        }
    }
}