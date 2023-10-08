using System.Text;
using Skeleton.Model;
using Skeleton.Model.NamingConventions;
using Skeleton.Model.Operations;

namespace Skeleton.SQLite;

public class SQLiteTypeProvider : ITypeProvider
{
    public Domain GetDomain(Settings settings)
    {
        throw new NotImplementedException();
    }

    public void GetOperations(Domain domain)
    {
        // TODO - we need to define some kind of code-gen meta-data table for storing this
        throw new NotImplementedException();
    }

    public void AddGeneratedOperation(string text)
    {
        // this is a no-op in SQLite
    }

    public void DropGenerated(Domain domain)
    {
        throw new NotImplementedException();
    }

    public CodeFile GenerateDropStatements(Domain oldDomain, Domain newDomain)
    {
        throw new NotImplementedException();
    }

    public string EscapeReservedWord(string name)
    {
        throw new NotImplementedException();
    }

    public string GetCsDbTypeFromDbType(string dbTypeName)
    {
        throw new NotImplementedException();
    }

    public string GetSqlName(string entityName)
    {
        throw new NotImplementedException();
    }

    public bool CustomTypeExists(string customTypeName)
    {
        throw new NotImplementedException();
    }

    public bool IsDateOnly(string typeName)
    {
        throw new NotImplementedException();
    }

    public bool IsTimeOnly(string typeName)
    {
        throw new NotImplementedException();
    }

    public void AddTestData(List<CodeFile> scripts)
    {
        throw new NotImplementedException();
    }

    public string DefaultNamespace { get; }
    public string GetTemplate(string templateName)
    {
        throw new NotImplementedException();
    }

    public bool GenerateCustomTypes { get; }
    public string FormatOperationParameterName(string operationName, string name)
    {
        throw new NotImplementedException();
    }

    public string OperationTimestampFunction()
    {
        throw new NotImplementedException();
    }

    public IParamterPrototype CreateFieldAdapter(Field field, IOperationPrototype operationPrototype)
    {
        throw new NotImplementedException();
    }

    public bool IncludeIdentityFieldsInInsertStatements { get; }
    public string GetProviderTypeForClrType(Type type)
    {
        throw new NotImplementedException();
    }

    public ISortField CreateSortField(IPseudoField field, IOperationPrototype operationPrototype)
    {
        throw new NotImplementedException();
    }

    public IPseudoField CreateSortParameter(INamingConvention namingConvention)
    {
        throw new NotImplementedException();
    }
}