using Skeleton.Model;
using Skeleton.Templating.DatabaseFunctions.Adapters;
using FluentAssertions;
using Xunit;

namespace Skeleton.Tests.DbGenerationTests.Aliasing
{
    public class When_Type_With_UpperCase_Name_Needs_An_Alias
    {
        private string alias = null;

        public When_Type_With_UpperCase_Name_Needs_An_Alias()
        {
            var type = new ApplicationType("FooBar", "test", null);
            var field = new Field(type) { Name = "baz" };
            var aliases = new FieldEntityAliasDictionary();
            alias = aliases.CreateAliasForTypeByField(field);
        }

        [Fact]
        public void The_Alias_Is_LowerCase()
        {
            alias.Should().Be("f");
        }
    }
}
