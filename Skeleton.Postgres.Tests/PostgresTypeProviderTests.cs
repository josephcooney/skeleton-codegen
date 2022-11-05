using System.IO.Abstractions.TestingHelpers;
using Npgsql;
using Shouldly;
using Skeleton.Model;
using Xunit;

namespace Skeleton.Postgres.Tests;

public class PostgresTypeProviderTests
{
    [Fact]
    public void CanBuildBasicDomain()
    {
        var testDbInfo = CreateTestDatabase(TestDbScript);
        try
        {
            var provider = new PostgresTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            var lookupType = model.Types.SingleOrDefault(t => t.Name == "simple_lookup_table");
            lookupType.ShouldNotBeNull();
            lookupType.Fields.Count.ShouldBe(4);
            
            // check id field
            var idField = lookupType.GetFieldByName("id");
            idField.ShouldNotBeNull();
            idField.IsKey.ShouldBeTrue();
            idField.IsGenerated.ShouldBeTrue();
            idField.ClrType.ShouldBe(typeof(int));
            idField.IsRequired.ShouldBeTrue();
            idField.Size.ShouldBeNull();
            
            // check name field
            var nameField = lookupType.GetFieldByName("name");
            nameField.ShouldNotBeNull();
            nameField.IsKey.ShouldBeFalse();
            nameField.IsRequired.ShouldBeTrue();
            nameField.IsGenerated.ShouldBeFalse();
            nameField.ClrType.ShouldBe(typeof(string));
            
            // check created field
            var createdField = lookupType.GetFieldByName("created");
            createdField.ShouldNotBeNull();
            createdField.IsKey.ShouldBeFalse();
            createdField.IsRequired.ShouldBeTrue();
            createdField.IsGenerated.ShouldBeFalse();
            createdField.ClrType.ShouldBe(typeof(DateTime));
            
            // check modified field
            var modifiedField = lookupType.GetFieldByName("modified");
            modifiedField.ShouldNotBeNull();
            modifiedField.IsKey.ShouldBeFalse();
            modifiedField.IsRequired.ShouldBeFalse();
            modifiedField.IsGenerated.ShouldBeFalse();
            modifiedField.ClrType.ShouldBe(typeof(DateTime?));

        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }
    
    protected (string connectionString, string dbName) CreateTestDatabase(string script)
    {
        var testDbName = "testdb_" + Guid.NewGuid().ToString().Replace("-", "_");
        var createConnectionStgring = CreateConnectionString("postgres");
        var connectionString = CreateConnectionString(testDbName);

        using (var cn = new NpgsqlConnection(createConnectionStgring))
        {
            cn.Open();
            using (var cmd = new NpgsqlCommand($"create database {testDbName}", cn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        
        using (var cn = new NpgsqlConnection(connectionString))
        {
            cn.Open();
            using (var cmd = new NpgsqlCommand(script, cn))
            {
                cmd.ExecuteNonQuery();
            }
        }
        return (connectionString, testDbName);
    }
    
    private string CreateConnectionString(string dbName)
    {
        return $"Server=127.0.0.1;Port=5432;Database={dbName};User Id=code_gen_test_user;Password=RT8mPd6emoV4srQs;";
    }
    
    protected void DestroyTestDb(string dbName)
    {
        var createConnectionStgring = CreateConnectionString("postgres");

        using (var cn = new NpgsqlConnection(createConnectionStgring))
        {
            cn.Open();
            var disconnect = $"SELECT pid, pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{dbName}';";
            using (var cmd = new NpgsqlCommand(disconnect, cn))
            {
                cmd.ExecuteNonQuery();
            }
            
            using (var cmd = new NpgsqlCommand($"drop database {dbName}", cn))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }

    
    private const string TestDbScript = @"
        create table simple_lookup_table (
            id serial primary key not null,
            name text not null,
            created timestamp not null,
            modified timestamp
        );
    ";
}