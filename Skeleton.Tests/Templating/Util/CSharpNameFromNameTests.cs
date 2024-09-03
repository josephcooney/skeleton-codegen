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

    [Theory]
    [InlineData("CustomerAddresses", "CustomerAddress")] // compound word that is pluralised
    [InlineData("Customers", "Customer")] // single word that is pluralised
    [InlineData("Document", "Document")] // not plural
    public void CanCreateSingularNameFromPluralTableName(string pluralisedName, string expectedSingularisedName)
    {
        var domain = TestUtil.CreateTestDomain(new MockFileSystem(), new PascalCaseNamingConvention(new NamingConventionSettings(){SingularizeTypeNames = true}));
        Skeleton.Templating.Util.RegisterHelpers(domain);
        var name = Skeleton.Templating.Util.CSharpNameFromName(pluralisedName);
        name.Should().Be(expectedSingularisedName);
    }
}