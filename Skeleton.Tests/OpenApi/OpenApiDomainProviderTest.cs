using System.IO.Abstractions.TestingHelpers;
using Skeleton.OpenApi;
using Moq;
using Xunit;

namespace Skeleton.Tests.OpenApi
{
    public class OpenApiDomainProviderTest
    {
        [Fact]
        public void CanAugmentDomainFromOpenApiDocument()
        {
            var domain = TestUtil.CreateTestDomain(new MockFileSystem());
            var openApiDocProvider = new Mock<IOpenApiDocumentProvider>();
            
            // TODO - set this up to return something
            
            var openApiProvider = new OpenApiDomainProvider(openApiDocProvider.Object);
            openApiProvider.AugmentDomainFromOpenApi(domain);
        }
    }
}