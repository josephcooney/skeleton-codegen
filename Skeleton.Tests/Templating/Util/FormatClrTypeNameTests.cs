using System;
using FluentAssertions;
using Xunit;

namespace Skeleton.Tests.Templating.Util
{
    public class FormatClrTypeNameTests
    {
        [Fact]
        public void CanGetFormattedClrTypeNameOfCommonTypes()
        {
            Skeleton.Templating.Util.FormatClrType(typeof(int)).Should().Be("int");
            Skeleton.Templating.Util.FormatClrType(typeof(int?)).Should().Be("int?");
            Skeleton.Templating.Util.FormatClrType(typeof(string)).Should().Be("string");
            Skeleton.Templating.Util.FormatClrType(typeof(Guid)).Should().Be("Guid");
            Skeleton.Templating.Util.FormatClrType(typeof(Skeleton.Templating.Util)).Should().Be("Util");
        }
    }
}