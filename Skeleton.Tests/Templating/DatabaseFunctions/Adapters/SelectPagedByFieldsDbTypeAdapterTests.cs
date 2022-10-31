using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Skeleton.Model;
using Skeleton.Model.Operations;
using Skeleton.Templating.DatabaseFunctions.Adapters;
using FluentAssertions;
using Xunit;

namespace Skeleton.Tests.Templating.DatabaseFunctions.Adapters
{
    public class SelectPagedByFieldsDbTypeAdapterTests
    {
        [Fact]
        public void SelectFieldsAreReturnedWithIndex()
        {
            var domain = TestUtil.CreateTestDomain(new MockFileSystem());
            var orderType = domain.Types.Single(t => t.Name == "order");
            var adapter = new SelectPagedByFieldsDbTypeAdapter(orderType, "test", new List<Field>(orderType.Fields),
                OperationType.Select, domain);

            adapter.SelectFieldsWithIndices.Count.Should().BeGreaterThan(1);
            
            for (var i = 0; i < adapter.SelectFieldsWithIndices.Count; i++)
            {
                adapter.SelectFieldsWithIndices[i].Index.Should().Be(i + 1);    
            }
        }
    }
}