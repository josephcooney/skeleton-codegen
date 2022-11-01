using Shouldly;
using Skeleton.Model.NamingConventions;
using Xunit;

namespace Skeleton.Model.Tests.NamingConventions;

public class PascalCaseNameingConventionTests
{
    [Fact]
    public void CanSplitPascalCaseNameIntoParts()
    {
        var namingConvention = new PascalCaseNamingConvention(null);
        var parts = namingConvention.GetNameParts("AutomaticXMLFormat");
        parts.Length.ShouldBe(3);
        parts[0].ShouldBe("Automatic");
        parts[1].ShouldBe("XML");
        parts[2].ShouldBe("Format");
    }
    
    [Fact]
    public void CanSplitSimplePascalCaseNameIntoParts()
    {
        var namingConvention = new PascalCaseNamingConvention(null);
        var parts = namingConvention.GetNameParts("Stack");
        parts.Length.ShouldBe(1);
        parts[0].ShouldBe("Stack");
    }
}