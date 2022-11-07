using Npgsql;

namespace Skeleton.Postgres.Tests;

public class DbTestBase
{
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
}