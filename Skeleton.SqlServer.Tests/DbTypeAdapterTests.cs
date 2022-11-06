using System.IO.Abstractions.TestingHelpers;
using System.Net.Http.Headers;
using Skeleton.Model;
using Skeleton.Model.Operations;
using Skeleton.Templating.DatabaseFunctions.Adapters;
using Shouldly;
using Xunit;

namespace Skeleton.SqlServer.Tests;

public class DbTypeAdapterTests : DbTestBase
{
    [Fact]
    public void IdentityFieldsAreNotInsertable()
    {
        var testDbInfo = CreateTestDatabase(SqlServerTypeProviderTests.TestDbWithRelatedEntities);
        try
        {
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            var type = model.Types.First(t => t.Name == "Product");
            var adapter = new DbTypeAdapter(type, "insert", OperationType.Insert, model);
            var idField = adapter.InsertFields.SingleOrDefault(f => f.Name == "Id");
            idField.ShouldBeNull();
            adapter.FunctionName.ShouldBe("ProductInsert");
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }
}