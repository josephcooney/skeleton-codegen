using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Skeleton.Model.NamingConventions;
using Xunit;

namespace Skeleton.Tests.Templating.Util;

public class MakeDbNameTests
{
    [Fact]
    public void CanCreateSingularizedDbNameFromParts()
    {
        var domain = TestUtil.CreateTestDomain(new MockFileSystem(), new PascalCaseNamingConvention(new NamingConventionSettings(){SingularizeTypeNames = true}));
        Skeleton.Templating.Util.RegisterHelpers(domain);
        var name = Skeleton.Templating.Util.MakeDbName(new List<string>(){"Clients", "New"});
        name.Should().Be("ClientNew");
    }
    
    [Fact]
    public void CanCreateSingularizedDbNameFromCompositeParts()
    {
        var domain = TestUtil.CreateTestDomain(new MockFileSystem(), new PascalCaseNamingConvention(new NamingConventionSettings(){SingularizeTypeNames = true}));
        Skeleton.Templating.Util.RegisterHelpers(domain);
        var name = Skeleton.Templating.Util.MakeDbName(new List<string>(){"ClientAddresses", "New"});
        name.Should().Be("ClientAddressNew");
    }
    
    [Fact]
    public void CanCreateSingularizedDbNameFromCompositeSnakeCaseParts()
    {
        var domain = TestUtil.CreateTestDomain(new MockFileSystem(), new SnakeCaseNamingConvention(new NamingConventionSettings(){SingularizeTypeNames = true}));
        Skeleton.Templating.Util.RegisterHelpers(domain);
        var name = Skeleton.Templating.Util.MakeDbName(new List<string>(){"client_addresses", "new"});
        name.Should().Be("client_address_new");
    }
}