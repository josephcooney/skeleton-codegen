using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Skeleton.Model.NamingConventions;
using Xunit;

namespace Skeleton.Tests.Templating.Util;

public class CSharpNameFromNameTests
{
    [Fact]
    public void CanGetCsNameFromDbNameWithCamelCaseNamingConvention()
    {
        var domain = TestUtil.CreateTestDomain(new MockFileSystem(), new PascalCaseNamingConvention(null));
        Skeleton.Templating.Util.RegisterHelpers(domain);
        var name = Skeleton.Templating.Util.CSharpNameFromName("Supervisor_display");
        name.Should().Be("SupervisorDisplay");
    }
}