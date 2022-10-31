using System.Data;
using System.Data.SqlClient;
using DbUp.Support;

namespace Skeleton.SqlServer.Tests;

public class DbTestBase
{
    protected (string connectionString, string dbName) CreateTestDatabase(string script)
    {
        var testDbName = "testdb_" + Guid.NewGuid().ToString().Replace("-", "_");
        var createConnectionStgring = CreateConnectionString("master");
        var connectionString = CreateConnectionString(testDbName);

        using (var cn = new SqlConnection(createConnectionStgring))
        {
            cn.Open();
            using (var cmd = new SqlCommand($"create database {testDbName}", cn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        RunMultiStatementSql(connectionString, script);

        return (connectionString, testDbName);
    }

    protected void RunMultiStatementSql(string connectionString, string script)
    {
        using (var cn = new SqlConnection(connectionString))
        {
            cn.Open();
            var splitter = new SqlCommandSplitter();
            var commands = splitter.SplitScriptIntoCommands(script);
            foreach (var command in commands)
            {
                using (var cmd = new SqlCommand(command, cn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
    
    protected void DestroyTestDb(string dbName)
    {
        var createConnectionStgring = CreateConnectionString("master");

        using (var cn = new SqlConnection(createConnectionStgring))
        {
            cn.Open();
            using (var cmd = new SqlCommand($"alter database {dbName} set single_user with rollback immediate", cn))
            {
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new SqlCommand($"drop database {dbName}", cn))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
    
    private string CreateConnectionString(string dbName)
    {
        return $"Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog={dbName};Integrated Security=SSPI;";
    }
    
    protected void AdditionalSchemaChanges(string connectionString, string schemaChange)
    {
        RunMultiStatementSql(connectionString, schemaChange);
    }
}