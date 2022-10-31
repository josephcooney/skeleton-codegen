using FluentAssertions;
using Xunit;

namespace Skeleton.Tests.Templating.Util
{
    public class TypescriptTypeFromClrTypeTests
    {
        [Fact]
        public void CanGetTypescriptTypesFromClrTypes()
        {
            Skeleton.Templating.Util.GetTypeScriptTypeForClrType(typeof(string[])).Should().Be("string[]");
            Skeleton.Templating.Util.GetTypeScriptTypeForClrType(typeof(byte[])).Should().Be("File"); // we special-case this
        }
    }
}