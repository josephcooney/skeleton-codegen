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
    
    [Fact]
    public void CanCreateResultTypeAsParameterForStoredProcedure()
    {
        var testDbInfo = CreateTestDatabase(TestDatbaseWithFunctionThatTakesCustomInsertTypeAsParam);
        try
        {
            var provider = new PostgresTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            provider.GetOperations(model);
            model.Operations.Count.ShouldBe(1);

            model.ResultTypes.Count.ShouldBe(1);
            
            var op = model.Operations.First();
            op.Parameters.Count.ShouldBe(3);
            var customTypeParam = op.Parameters.Single(p => p.Name == "government_area_to_add");
            customTypeParam.ProviderTypeName.ShouldBe("government_area_new");
            customTypeParam.ClrType.ShouldBe(typeof(ResultType));
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

    private const string TestDatbaseWithFunctionThatTakesCustomInsertTypeAsParam = @"
    CREATE TABLE ""user"" (
        id serial primary key NOT NULL,
        ""name"" text NULL,
        is_system bool NOT NULL,
        user_name text NOT NULL,
        created timestamp with time zone not null NOT NULL,
        created_by int NOT NULL references ""user""(id),
        CONSTRAINT user_name_unique UNIQUE (user_name)
    );

    create table government_area (
         id serial primary key not null,
         name text not null,
         color char(7),
         city_town text not null,
         state varchar(100) not null,
         country varchar(100) not null,
         created_by int not null references ""user""(id),
        created timestamp with time zone not null,
        modified_by int references ""user""(id),
        modified timestamp with time zone
    );

    CREATE TYPE public.government_area_new AS (
	    ""name"" text,
        logo_id int4,
        olor bpchar(7),
        city_town text,
        state varchar(100),
        country varchar(100)
    );

CREATE OR REPLACE FUNCTION public.government_area_insert(security_user_id_param integer, created_by integer, government_area_to_add government_area_new)
 RETURNS integer
 LANGUAGE plpgsql
AS $function$
    DECLARE new_id integer;

    BEGIN

        IF (security_user_id_param is not null) THEN
		    perform set_config('app.user_id', security_user_id_param::text, true);
        END IF;

        insert into government_area (
            id,
            ""name"",
            logo_id,
            color,
            city_town,
            ""state"",
            country,
            created_by,
            created
        )
        VALUES
        (
            DEFAULT,
            government_area_to_add.name,
            government_area_to_add.logo_id,
            government_area_to_add.color,
            government_area_to_add.city_town,
            government_area_to_add.state,
            government_area_to_add.country,
            government_area_insert.created_by,
            clock_timestamp()
        );

        new_id = currval(pg_get_serial_sequence('government_area', 'id'));
        return new_id;
    END
    $function$
    ;
";
}