using System.IO.Abstractions.TestingHelpers;
using Shouldly;
using Skeleton.Model;
using Skeleton.Model.Operations;
using Skeleton.Templating.DatabaseFunctions.Adapters;
using Xunit;

namespace Skeleton.Postgres.Tests;

public class DbTypeAdapterTests : DbTestBase
{
    [Fact]
    public void SerialIdAndCreatedFieldsAreInsertable()
    {
        var testDbInfo = CreateTestDatabase(PostgresTypeProviderTests.TestDatbaseWithFunctionThatTakesCustomInsertTypeAsParam);
        try
        {
            var provider = new PostgresTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            var type = model.Types.First(t => t.Name == "government_area");
            var adapter = new DbTypeAdapter(type, new []{"insert"}, OperationType.Insert, model);
            
            // serial id fields are insertable, because postgres uses the value 'default' for them
            var idField = adapter.InsertFields.SingleOrDefault(f => f.Name == "id");
            idField.ShouldNotBeNull();
            var idFieldEx = idField as IParamterPrototype;
            idFieldEx?.Value.ShouldBe("DEFAULT");

            var createdField = adapter.InsertFields.SingleOrDefault(f => f.Name == "created");
            createdField.ShouldNotBeNull();
            
            adapter.FunctionName.ShouldBe("government_area_insert");
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }
}