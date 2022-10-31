using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Skeleton.Model.Operations;
using Skeleton.Templating.DatabaseFunctions.Adapters;
using Xunit;

namespace Skeleton.Tests.DbGenerationTests.Policy
{
    public class When_Type_Is_One_Degree_Removed_From_Owning_Type
    {
        private DbTypeAdapter adapter;

        public When_Type_Is_One_Degree_Removed_From_Owning_Type()
        {
            var domain = TestUtil.CreateTestDomain(new MockFileSystem());
            var orderType = domain.Types.Single(t => t.Name == "order");
            adapter = new DbTypeAdapter(orderType, null, OperationType.None, domain);
        }

        [Fact]
        public void DbTypeAdapter_LinkToOwershipType_Returns_Linking_Fields()
        {
            Assert.NotNull(adapter.LinkToOwershipType);
            Assert.Equal(2, adapter.LinkToOwershipType.Count);
            Assert.Equal("customer_id", adapter.LinkToOwershipType.First().Name);
            Assert.Equal("created_by", adapter.LinkToOwershipType.Last().Name);
        }
    }
}