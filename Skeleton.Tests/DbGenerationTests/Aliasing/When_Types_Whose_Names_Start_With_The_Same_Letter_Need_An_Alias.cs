using Skeleton.Model;
using Skeleton.Templating.DatabaseFunctions.Adapters;
using FluentAssertions;
using Xunit;

namespace Skeleton.Tests.DbGenerationTests.Aliasing
{
    public class When_Types_Whose_Names_Start_With_The_Same_Letter_Need_An_Alias
    {
        private string alias = null;
        private string secondAlias = null;

        public When_Types_Whose_Names_Start_With_The_Same_Letter_Need_An_Alias()
        {
            var type = new ApplicationType("fooBar", "test", null);
            var f1 = new Field(type) { Name = "baz" };

            var secondType = new ApplicationType("food", "test", null);
            var f2 = new Field(secondType) {Name = "wahoo"};
            var aliases = new FieldEntityAliasDictionary();
            alias = aliases.CreateAliasForTypeByField(f1);
            secondAlias = aliases.CreateAliasForTypeByField(f2);
        }

        [Fact]
        public void The_Aliases_Are_Unique()
        {
            alias.Should().NotBe(secondAlias);
        }
    }
}
