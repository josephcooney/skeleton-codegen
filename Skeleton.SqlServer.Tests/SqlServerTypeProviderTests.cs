using System.IO.Abstractions.TestingHelpers;
using System.Xml;
using Skeleton.Model;
using Shouldly;
using Xunit;

namespace Skeleton.SqlServer.Tests;

public class SqlServerTypeProviderTests : DbTestBase
{
    [Fact]
    public void CanBuildBasicDomain()
    {
        var testDbInfo = CreateTestDatabase(TestDbScript);
        try
        {
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
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
    public void CanBuildBasicDomainFromCamelCaseDb()
    {
        var testDbInfo = CreateTestDatabase(TestDbWithCamelCaseNames);
        try
        {
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            var lookupType = model.Types.SingleOrDefault(t => t.Name == "SimpleLookupTable");
            lookupType.ShouldNotBeNull();
            lookupType.Fields.Count.ShouldBe(4);
            
            // check Id field
            var idField = lookupType.GetFieldByName("Id");
            idField.ShouldNotBeNull();
            idField.IsKey.ShouldBeTrue();
            idField.IsGenerated.ShouldBeTrue();
            idField.ClrType.ShouldBe(typeof(int));
            idField.IsRequired.ShouldBeTrue();
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }

    [Fact]
    public void CanBuildDomainWithNonPkIdentityColumn()
    {
        var testDbInfo = CreateTestDatabase(TestDbWithNonPkIdentityColumn);
        try
        {
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            var lookupType = model.Types.SingleOrDefault(t => t.Name == "SimpleLookupTable");
            lookupType.ShouldNotBeNull();
            lookupType.Fields.Count.ShouldBe(5);
            
            // check Id field
            var idField = lookupType.GetFieldByName("Id");
            idField.ShouldNotBeNull();
            idField.IsKey.ShouldBeTrue();
            idField.IsGenerated.ShouldBeFalse();
            idField.ClrType.ShouldBe(typeof(int));
            idField.IsRequired.ShouldBeTrue();
            
            var seqField = lookupType.GetFieldByName("SequenceNo");
            seqField.ShouldNotBeNull();
            seqField.IsKey.ShouldBeFalse();
            seqField.IsGenerated.ShouldBeTrue();
            seqField.ClrType.ShouldBe(typeof(int));
            seqField.IsRequired.ShouldBeTrue();
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }
    
    [Fact]
    public void CanBuildDomainWithRelatedEntities()
    {
        var testDbInfo = CreateTestDatabase(TestDbWithRelatedEntities);
        try
        {
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            var productCategory = model.Types.SingleOrDefault(t => t.Name == "ProductCategory");
            productCategory.ShouldNotBeNull();
            var productCategoryId = productCategory.GetFieldByName("Id");
            productCategoryId.ShouldNotBeNull();
            productCategoryId.IsKey.ShouldBeTrue();
            
            var product = model.Types.SingleOrDefault(t => t.Name == "Product");
            product.ShouldNotBeNull();
            var materials = product.GetFieldByName("Materials");
            materials.ShouldNotBeNull();
            materials.ClrType.ShouldBe(typeof(XmlDocument));

            var categoryField = product.Fields.SingleOrDefault(f => f.Name == "Category");
            categoryField.ShouldNotBeNull();
            categoryField.ReferencesType.ShouldBe(productCategory); 
            categoryField.ReferencesTypeField.ShouldBe(productCategoryId);
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }
    
    [Fact]
    public void CanBuildDomainWithCalculatedColumns()
    {
        var testDbInfo = CreateTestDatabase(TestDbWithCalculatedColumn);
        try
        {
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            var productOrder = model.Types.SingleOrDefault(t => t.Name == "ProductOrder");
            productOrder.ShouldNotBeNull();

            var price = productOrder.GetFieldByName("Price");
            price.ShouldNotBeNull();
            price.IsComputed.ShouldBeTrue();
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }

    [Fact]
    public void ViewsAreNotIncludedInDomainEntities()
    {
        var testDbInfo = CreateTestDatabase(TestDbWithRelatedEntities);
        try
        {
            AdditionalSchemaChanges(testDbInfo.connectionString, AndViewToSchema);
            
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            model.Types.Count.ShouldBe(2);
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }

    [Fact]
    public void OperationsAreAddedToDomainModel()
    {
        var testDbInfo = CreateTestDatabase(TestDbWithRelatedEntities);
        try
        {
            AdditionalSchemaChanges(testDbInfo.connectionString, AddFunctionToSchema);
            
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            provider.GetOperations(model);
            model.Operations.Count.ShouldBe(1);
            
            var operation = model.Operations.SingleOrDefault(op => op.Name == "ProductsByCategory");
            operation.ShouldNotBeNull();
            operation.ProviderType.ShouldBe("FUNCTION");
            operation.Parameters.Count.ShouldBe(1);

            var parameter = operation.Parameters.First();
            parameter.Name.ShouldBe("categoryId");
            parameter.ProviderTypeName.ShouldBe("int");
            parameter.ClrType.ShouldBe(typeof(int));

            operation.Returns.ShouldNotBeNull();
            operation.Returns.Multiple.ShouldBe(true);
            operation.Returns.ReturnType.ShouldBe(ReturnType.CustomType);
            operation.Returns.SimpleReturnType.Fields.Count.ShouldBe(2);
            model.ResultTypes.ShouldContain(operation.Returns.SimpleReturnType);
            var idField = operation.Returns.SimpleReturnType.GetFieldByName("Id");
            idField.ShouldNotBeNull();
            idField.ClrType.ShouldBe(typeof(int));
            var nameField = operation.Returns.SimpleReturnType.GetFieldByName("Name");
            nameField.ShouldNotBeNull();
            nameField.ClrType.ShouldBe(typeof(string));
            nameField.Size.ShouldBe(250);
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }

    [Fact]
    public void OperationWithScalarReturnValueIsAddedCorrectlyToDomainModel()
    {
        var testDbInfo = CreateTestDatabase(TestDbWithRelatedEntities);
        try
        {
            AdditionalSchemaChanges(testDbInfo.connectionString, AddScalarFunctionToSchema);
            
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            provider.GetOperations(model);
            model.Operations.Count.ShouldBe(1);
            
            var operation = model.Operations.First();
            operation.Parameters.Count.ShouldBe(2);
            
            operation.Returns.ShouldNotBeNull();
            operation.Returns.Multiple.ShouldBe(false);
            operation.Returns.ClrReturnType.ShouldBe(typeof(decimal));
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }
    
    [Fact]
    public void CanReadAttributesForDomainTypes()
    {
        var testDbInfo = CreateTestDatabase(TestDbWithRelatedEntities);
        try
        {
            AdditionalSchemaChanges(testDbInfo.connectionString, AddAttributeToTable);
            
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            var productCategory = model.Types.SingleOrDefault(t => t.Name == "ProductCategory");
            productCategory.ShouldNotBeNull();
            productCategory.IsReferenceData.ShouldBeTrue();
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }
    
    [Fact]
    public void CanReadAttributesForOperations()
    {
        var testDbInfo = CreateTestDatabase(TestDbWithRelatedEntities);
        try
        {
            AdditionalSchemaChanges(testDbInfo.connectionString, AddFunctionToSchema);
            AdditionalSchemaChanges(testDbInfo.connectionString, AddAttributeToFunction);
            
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            provider.GetOperations(model);
            var operation = model.Operations.SingleOrDefault(op => op.Name == "ProductsByCategory");
            operation.ShouldNotBeNull();
            operation.IsGenerated.ShouldBeTrue();
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }

    [Fact]
    public void SimpleStoredProcedureCanBeAddedAsOperationToDomainModel()
    {
        var testDbInfo = CreateTestDatabase(TestDbWithRelatedEntities);
        try
        {
            AdditionalSchemaChanges(testDbInfo.connectionString, AddStoredProcedureToSchema);
            
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            provider.GetOperations(model);
            model.Operations.Count.ShouldBe(1);
            var op = model.Operations.First();
            op.Name.ShouldBe("GetDbName");
            op.ProviderType.ShouldBe("PROCEDURE");
            op.Parameters.Count.ShouldBe(0);
            
            op.Returns.ShouldNotBeNull();
            op.Returns.SimpleReturnType.ShouldBeNull();
            op.Returns.ClrReturnType.ShouldBe(typeof(string));
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }

    [Fact]
    public void CanAugmentStoredProcedureReturnInformationWithDetailsFromDomainType()
    {
        var testDbInfo = CreateTestDatabase(TestDbWithRelatedEntitiesAndFunctions);
        try
        {
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            provider.GetOperations(model);
            model.Operations.Count.ShouldBe(3);
            var op = model.Operations.First(o => o.Name == "ProductSelectAllForDisplay");
            op.RelatedType.ShouldBe(model.Types.SingleOrDefault(t => t.Name == "Product"));
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }
    
    [Fact]
    public void CanAugmentStoredProcedureParameterInformationWithDetailsFromDomainType()
    {
        var testDbInfo = CreateTestDatabase(TestDbWithRelatedEntitiesAndFunctions);
        try
        {
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            provider.GetOperations(model);
            model.Operations.Count.ShouldBe(3);
            
            var op = model.Operations.First(o => o.Name == "ProductSelectAllForDisplayByCategoryId");
            op.RelatedType.ShouldBe(model.Types.SingleOrDefault(t => t.Name == "Product"));
            op.Parameters.Count.ShouldBe(1);
            op.Parameters[0].RelatedTypeField.ShouldNotBeNull();
            op.Parameters[0].Name.ShouldBe("Category");
            
            var product = model.Types.First(t => t.Name == "Product");
            var categoryField = product.GetFieldByName("Category");
            op.Parameters[0].RelatedTypeField.ShouldBe(categoryField);

        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }
    
    [Fact]
    public void CanAugmentSimpleStoredProcedureReturnInformationWithDetailsFromDomainType()
    {
        var testDbInfo = CreateTestDatabase(TestDbWithRelatedEntitiesAndFunctions);
        try
        {
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            provider.GetOperations(model);
            model.Operations.Count.ShouldBe(3);
            var op = model.Operations.First(o => o.Name == "ProductSelectAll");
            op.RelatedType.ShouldBe(model.Types.SingleOrDefault(t => t.Name == "Product"));
            var product = model.Types.First(t => t.Name == "Product");
            op.Returns.SimpleReturnType.ShouldBe(product);
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }

    [Fact]
    public void CanCreateResultTypeAsParameterForStoredProcedure()
    {
        var testDbInfo = CreateTestDatabase(TestDatabaseWithStoredProcThatTakesCustomInsertTypeAsParam);
        try
        {
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            provider.GetOperations(model);

            var validationType = model.Types.SingleOrDefault(t => t.Name == "ValidationStatus");
            validationType.ShouldNotBeNull();
            var nameUnderlyingTypeField = validationType.GetFieldByName("Name");
            nameUnderlyingTypeField.IsRequired.ShouldBe(true);
            
            model.Operations.Count.ShouldBe(1);
            var op = model.Operations.First();
            op.Parameters.Count.ShouldBe(3);
            var customTypeParam = op.Parameters.Single(p => p.Name == "ValidationStatusToAdd");
            customTypeParam.ProviderTypeName.ShouldBe("ValidationStatusNew");
            
            model.ResultTypes.Count(t => t.Name == customTypeParam.ProviderTypeName).ShouldBe(1);
            var resultType = model.ResultTypes.Single(t => t.Name == customTypeParam.ProviderTypeName);
            resultType.Ignore.ShouldBe(false);
            var nameField = resultType.GetFieldByName("Name");
            nameField.ShouldNotBeNull();
            nameField.IsRequired.ShouldBe(true);
            
            op.SingleResult.ShouldBe(true);
            op.Returns.ClrReturnType.ShouldBe(typeof(int));
            op.BareName.ShouldBe("Insert");
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }

    [Fact]
    public void CanReadAttributesFromFields()
    {
        var testDbInfo = CreateTestDatabase(TableWithFieldAttributes);
        try
        {
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            var logoType = model.Types.First();
            logoType.Name.ShouldBe("CompanyLogo");
            logoType.IsAttachment.ShouldBe(true);

            var mimeType = logoType.GetFieldByName("MimeType");
            mimeType.ShouldNotBeNull();
            mimeType.IsAttachmentContentType.ShouldBe(true);
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }

    [Fact]
    public void UpdateParametersAreCorrectlyRepresentedInModel()
    {
        var testDbInfo = CreateTestDatabase(TestDatabaseWithUpdateStoredProc);
        try
        {
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            provider.GetOperations(model);

            var op = model.Operations.SingleOrDefault(o => o.Name == "ValidationStatusUpdate");
            op.ShouldNotBeNull();
            op.Parameters.Count.ShouldBe(5);
            op.UserProvidedParameters.Count.ShouldBe(3);
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }

    [Fact]
    public void ReturnedDataFromFunctionsCaseInvariantIsMatchedWithExistingDomainAndReturnTypes()
    {
        var testDbInfo = CreateTestDatabase(TestDbWithRelatedEntities);
        try
        {
            // the capitalisation of field names in the 'select product' function is different to
            // the capitalisation of the field names in the underlying table, but because SQL Server
            // doesn't care about capitalisation they should still match.
            AdditionalSchemaChanges(testDbInfo.connectionString, AddSelectAllFunctionToProductDb);
            var provider = new SqlServerTypeProvider(testDbInfo.connectionString);
            var model = provider.GetDomain(new Settings(new MockFileSystem()));
            provider.GetOperations(model);

            var productType = model.Types.SingleOrDefault(t => t.Name == "Product");    
            
            var op = model.Operations.SingleOrDefault(o => o.Name == "SelectAllProducts");
            op?.Returns.SimpleReturnType.ShouldBe(productType);
        }
        finally
        {
            DestroyTestDb(testDbInfo.dbName);
        }
    }
    
    private const string TestDbScript = @"
        create table simple_lookup_table (
            id int identity primary key not null,
            name text not null,
            created datetime not null,
            modified datetime
        );
    ";
    
    private const string TestDbWithCamelCaseNames = @"
        create table SimpleLookupTable (
            Id int identity primary key not null,
            Name text not null,
            Created datetime not null,
            Modified datetime
        );
    ";
    
    private const string TestDbWithNonPkIdentityColumn = @"
        create table SimpleLookupTable (
            Id int primary key not null,
            SequenceNo int identity not null,
            Name text not null,
            Created datetime not null,
            Modified datetime
        );
    ";
    
    public const string TestDbWithRelatedEntities = @"
        create table ProductCategory (
            Id int primary key not null,
            Name varchar(100) not null,
            Created datetime not null,
            Modified datetime
        );

        create table Product (
            Id int identity primary key not null,
            Category int not null references ProductCategory(id),
            Name varchar(250) not null,
            Description text,
            Materials xml,
            UnitPrice decimal,
            Created datetimeoffset not null
        );
    ";
    
    public const string TestDbWithRelatedEntitiesAndFunctions = @"
        create table ProductCategory (
            Id int primary key not null,
            Name varchar(100) not null,
            Created datetime not null,
            Modified datetime
        );

        create table Product (
            Id int identity primary key not null,
            Category int not null references ProductCategory(id),
            Name varchar(250) not null,
            Description text,
            Materials xml,
            UnitPrice decimal,
            Created datetime not null
        );
        GO
    
        create procedure ProductSelectAllForDisplay AS
            BEGIN
                SELECT p.Id,
                p.Category,
                pc.Name as Category_display,
                p.Name,
                p.Description,
                p.Materials,
                p.UnitPrice,
                p.Created
                FROM Product p inner join ProductCategory PC on p.Category = PC.Id
            END;
        GO
        
        EXEC sys.sp_addextendedproperty 'codegen_meta', N'{""applicationtype"":""Product""}', 'schema', N'dbo', 'procedure', N'ProductSelectAllForDisplay';
        GO
        
        create procedure ProductSelectAll AS
            BEGIN
                SELECT p.Id,
                p.Category,
                p.Name,
                p.Description,
                p.Materials,
                p.UnitPrice,
                p.Created
                FROM Product p
            END;
        GO
        
        EXEC sys.sp_addextendedproperty 'codegen_meta', N'{""applicationtype"":""Product""}', 'schema', N'dbo', 'procedure', N'ProductSelectAll';
        GO
            
        create procedure ProductSelectAllForDisplayByCategoryId 
            @Category int
            AS
            BEGIN
                SELECT p.Id,
                p.Category,
                pc.Name as CategoryName,
                p.Name,
                p.Description,
                p.Materials,
                p.UnitPrice,
                p.Created
                FROM Product p inner join ProductCategory PC on p.Category = PC.Id
                WHERE p.Category = @Category
            END;
        GO
        
        EXEC sys.sp_addextendedproperty 'codegen_meta', N'{""applicationtype"":""Product""}', 'schema', N'dbo', 'procedure', N'ProductSelectAllForDisplayByCategoryId';
        GO
    ";
    
    public const string TestDbWithCalculatedColumn = @"
        create table Product (
            Id int primary key not null,
            Name varchar(100) not null,
            Created datetime not null,
            Modified datetime
        );

        create table ProductOrder (
            Id int identity primary key not null,
            Product int not null references Product(id),
            Address varchar(250) not null,
            UnitPrice decimal not null,
            Quantity int not null,
            Price as (UnitPrice * Quantity)
        );
    ";

    private const string TestDatabaseWithStoredProcThatTakesCustomInsertTypeAsParam = @"
CREATE TABLE [User] (
    Id int identity primary key NOT NULL,
    Name text NULL,
    IsSystem bit NOT NULL,
    UserName nvarchar(250) NOT NULL,
    Created datetimeoffset not null,
    CreatedBy int NOT NULL references [User](Id),
    CONSTRAINT UserName_Unique UNIQUE (UserName)
);
GO

create table ValidationStatus (
	   Id int identity primary key not null,
	   Name varchar(50) not null,
	   CreatedBy int not null references [User](id),
	   Created datetimeoffset not null,
	   ModifiedBy int references [User](id),
	   Modified datetimeoffset
);
GO

CREATE TYPE ValidationStatusNew AS TABLE (
	Name varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Created datetimeoffset NULL,
	Modified datetimeoffset NULL
);
GO

CREATE   PROCEDURE dbo.ValidationStatusInsert 
    @SecurityUserIdParam int,
    @CreatedBy int,
    @ValidationStatusToAdd dbo.ValidationStatusNew READONLY
AS
    BEGIN


        insert into ValidationStatus (
            Name,
            CreatedBy,
            Created,
            Modified
        )

        SELECT 
        Name,
        @CreatedBy,
        Created,
        Modified
        FROM @ValidationStatusToAdd

        SELECT cast(scope_identity() as integer);
    END;
GO

EXEC sys.sp_addextendedproperty 'codegen_meta', N'{""applicationtype"":""ValidationStatus"", ""single_result"":true}', 'schema', N'dbo', 'procedure', N'ValidationStatusInsert';
GO
";
    
    
    private const string TestDatabaseWithUpdateStoredProc = @"
CREATE TABLE [User] (
    Id int identity primary key NOT NULL,
    Name text NULL,
    IsSystem bit NOT NULL,
    UserName nvarchar(250) NOT NULL,
    Created datetimeoffset not null,
    CreatedBy int NOT NULL references [User](Id),
    CONSTRAINT UserName_Unique UNIQUE (UserName)
);
GO

EXEC sp_addextendedproperty 'codegen_meta', N'{""isSecurityPrincipal"":true}', 'schema', N'dbo', 'table', N'User';
GO

create table ValidationStatus (
	   Id int identity primary key not null,
	   Name varchar(50) not null,
	   Code char(7) not null,
	   CreatedBy int not null references [User](id),
	   Created datetimeoffset not null,
	   ModifiedBy int references [User](id),
	   Modified datetimeoffset
);
GO

CREATE   PROCEDURE dbo.ValidationStatusUpdate 
    @SecurityUserIdParam int,
    @ModifiedBy int,
    @Name varchar(50),
    @Code char(7),
    @Id int
AS
    BEGIN

    update ValidationStatus
    set Name = @Name,
    Code = @Code,
    ModifiedBy = @ModifiedBy
    WHERE Id = @Id
    
    END;
GO

EXEC sys.sp_addextendedproperty 'codegen_meta', N'{""applicationtype"":""ValidationStatus"", ""changesData"":true, ""friendlyName"":""Edit""}', 'schema', N'dbo', 'procedure', N'ValidationStatusUpdate';
GO
";

    private const string TableWithFieldAttributes = @"
create table CompanyLogo (
	  Id int identity primary key not null,
	  Name text not null,
	  MimeType text not null,
	  Contents varbinary(max) not null,
	  Thumbnail varbinary(max) not null,
	  Created datetimeoffset not null,
	  Modified datetimeoffset	
);
GO

EXEC sp_addextendedproperty 'codegen_meta', N'{""isAttachment"":true, ""security"":{""anon"":[""read""]}}', 'schema', N'dbo', 'table', N'CompanyLogo';
GO

EXEC sp_addextendedproperty   
    @name = N'codegen_meta',   
    @value = '{""isContentType"": true}',  
    @level0type = N'Schema', @level0name = 'dbo',  
    @level1type = N'Table',  @level1name = 'CompanyLogo',  
    @level2type = N'Column', @level2name = 'MimeType';  
GO    
";
    
    private const string AndViewToSchema = @"        
        create view ProductsCreatedToday AS
            select * from Product
            where Created > cast (Getdate() as Date);
    ";

    private const string AddFunctionToSchema = @"
        create function ProductsByCategory(@categoryId int)
        returns table 
        as return (Select Id, Name from Product as P where P.Category = @categoryId);
    ";

    private const string AddAttributeToTable = "EXEC sys.sp_addextendedproperty 'codegen_meta', N'{\"type\":\"reference\"}', 'schema', N'dbo', 'table', N'ProductCategory';";

    private const string AddAttributeToFunction =
        "EXEC sys.sp_addextendedproperty 'codegen_meta', N'{\"generated\":true}', 'schema', N'dbo', 'function', N'ProductsByCategory';";

    private const string AddScalarFunctionToSchema = @"
        create function CalculatePrice (@itemPrice decimal, @quantity int)
        RETURNS decimal
        AS
        BEGIN
            return @itemPrice * @quantity;
        END;   
    ";

    private const string AddStoredProcedureToSchema = @"
        CREATE PROC GetDbName
        AS
        SELECT DB_NAME() AS ThisDB;
    ";

    private const string AddSelectAllFunctionToProductDb = @"
    create function SelectAllProducts() 
    returns table
    as
    return select id, category, name, description, materials, unitprice, created from Product;
    GO
    EXEC sys.sp_addextendedproperty 'codegen_meta', N'{""applicationtype"":""Product""}', 'schema', N'dbo', 'function', N'SelectAllProducts';
    ";
}

