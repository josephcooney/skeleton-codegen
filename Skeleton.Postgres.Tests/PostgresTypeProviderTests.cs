using System.IO.Abstractions.TestingHelpers;
using Npgsql;
using Shouldly;
using Skeleton.Model;
using Skeleton.Model.NamingConventions;
using Xunit;

namespace Skeleton.Postgres.Tests;

public class PostgresTypeProviderTests : DbTestBase
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
            createdField.IsTrackingDate.ShouldBe(true);
            createdField.IsCreatedDate.ShouldBe(true);
            
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
            model.ResultTypes.Count(t => t.Name == customTypeParam.ProviderTypeName).ShouldBe(1);
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }

    [Fact]
    public void CanCreateOperationWhenParameterNameDoesNotMatchFieldOnDomainObject()
    {
        var testDbInfo = CreateTestDatabase(DbSchemaWithFunction);
        try
        {
            var provider = new PostgresTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            provider.GetOperations(model);
            model.Operations.Count.ShouldBe(1);
            var op = model.Operations.First();
            op.Parameters.Count.ShouldBe(2);

        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }

    [Fact]
    public void CanBuildDomainForPascalCaseSchema()
    {
        var testDbInfo = CreateTestDatabase(PascalCaseSchemaScript);
        try
        {
            var provider = new PostgresTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()){NamingConventionSettings = new NamingConventionSettings(){DbNamingConvention = DbNamingConvention.PascalCase}});
            var lookupType = model.Types.SingleOrDefault(t => t.Name == "SimpleLookupTable");
            lookupType.ShouldNotBeNull();
            lookupType.Fields.Count.ShouldBe(4);
            lookupType.IsReferenceData.ShouldBe(true);
            
            // check id field
            var idField = lookupType.GetFieldByName("Id");
            idField.ShouldNotBeNull();
            idField.IsKey.ShouldBeTrue();
            idField.IsGenerated.ShouldBeTrue();
            idField.ClrType.ShouldBe(typeof(int));
            idField.IsRequired.ShouldBeTrue();
            idField.Size.ShouldBeNull();
            
            // check name field
            var nameField = lookupType.GetFieldByName("Name");
            nameField.ShouldNotBeNull();
            nameField.IsKey.ShouldBeFalse();
            nameField.IsRequired.ShouldBeTrue();
            nameField.IsGenerated.ShouldBeFalse();
            nameField.ClrType.ShouldBe(typeof(string));
            
            // check created field
            var createdField = lookupType.GetFieldByName("Created");
            createdField.ShouldNotBeNull();
            createdField.IsKey.ShouldBeFalse();
            createdField.IsRequired.ShouldBeTrue();
            createdField.IsGenerated.ShouldBeFalse();
            createdField.ClrType.ShouldBe(typeof(DateTime));
            createdField.IsTrackingDate.ShouldBe(true);
            createdField.IsCreatedDate.ShouldBe(true);
            
            // check modified field
            var modifiedField = lookupType.GetFieldByName("Modified");
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
    public void IdentifiesGuidPrimaryKeyAsNotUserProvided()
    {
        var testDbInfo = CreateTestDatabase(TestDbScriptWithGuid);
        try
        {
            var provider = new PostgresTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()){NamingConventionSettings = new NamingConventionSettings(){DbNamingConvention = DbNamingConvention.PascalCase}});
            var lookupType = model.Types.SingleOrDefault(t => t.Name == "simple_lookup_table");
            lookupType.ShouldNotBeNull();
            lookupType.Fields.Count.ShouldBe(4);
            
            // check id field
            var idField = lookupType.GetFieldByName("id");
            idField.ShouldNotBeNull();
            idField.IsKey.ShouldBeTrue();
            idField.IsGenerated.ShouldBeTrue();
            idField.ClrType.ShouldBe(typeof(Guid));
            idField.IsRequired.ShouldBeTrue();
            idField.Size.ShouldBeNull();
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
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
    
    private const string TestDbScriptWithGuid = @"
        CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";
        create table simple_lookup_table (
            id uuid primary key not null DEFAULT uuid_generate_v4(),
            name text not null,
            created timestamp not null,
            modified timestamp
        );
    ";

    public const string TestDatbaseWithFunctionThatTakesCustomInsertTypeAsParam = @"
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
AS $$
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
    $$
    ;
";

    public const string DbSchemaWithFunction = @"

create table task_type (
   id serial primary key not null,
   name text not null,
   instructions_template text,
   step_order int,
   created timestamp with time zone not null,
   modified timestamp with time zone
);

create table process (
    id serial primary key not null,
    created timestamp with time zone not null
);

create table process_task (
  id serial primary key not null,
  process_id int not null references process(id),
  task_type_id int not null references task_type(id),
  instructions text,
  created timestamp with time zone not null,
  modified timestamp with time zone
);

create function public.change_task_type(id integer, new_type_id integer)
returns integer 
LANGUAGE plpgsql
as $$
declare item_count integer;
begin
	update process_task set task_type_id = change_task_type.new_type_id
	where id = change_task_type.id;
	get diagnostics item_count = row_count;

	return update_count;
end
$$;

COMMENT ON FUNCTION change_task_type ( integer, integer)
    IS '{""applicationtype"":""process_task"", ""changesData"":true }';

";

    const string PascalCaseSchemaScript = @"
        create table ""SimpleLookupTable"" (
            ""Id"" serial primary key not null,
            ""Name"" text not null,
            ""Created"" timestamp not null,
            ""Modified"" timestamp
        );

        COMMENT ON TABLE ""SimpleLookupTable"" IS '{""type"":""reference""}';
    ";

}