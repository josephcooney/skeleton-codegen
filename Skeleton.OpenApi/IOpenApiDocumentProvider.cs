using NSwag;

namespace Skeleton.OpenApi
{
    public interface IOpenApiDocumentProvider
    {
        OpenApiDocument GetOpenApiDocument();
    }
}