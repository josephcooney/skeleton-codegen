using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using System.Xml;
using Skeleton.Model;
using Skeleton.Model.Operations;
using Skeleton.Templating;
using DbUp.Support;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Skeleton.Model.NamingConventions;

namespace Skeleton.SqlServer;

public class SqlServerTypeProvider : ITypeProvider
{
    internal class OperationTypes
    {
        internal const string Procedure = "PROCEDURE";
        internal const string Function = "FUNCTION";
    }
    
    private readonly string _connectionString;
    private string _databaseName = string.Empty;
    private INamingConvention _namingConvention;
    
    private readonly string[] _sqlReservedWords = new[]
    {
        "ADD",
        "EXTERNAL",
        "PROCEDURE",
        "ALL",
        "FETCH",
        "PUBLIC",
        "ALTER",
        "FILE",
        "RAISERROR",
        "AND",
        "FILLFACTOR",
        "READ",
        "ANY",
        "FOR",
        "READTEXT",
        "AS",
        "FOREIGN",
        "RECONFIGURE",
        "ASC",
        "FREETEXT",
        "REFERENCES",
        "AUTHORIZATION",
        "FREETEXTTABLE",
        "REPLICATION",
        "BACKUP",
        "FROM",
        "RESTORE",
        "BEGIN",
        "FULL",
        "RESTRICT",
        "BETWEEN",
        "FUNCTION",
        "RETURN",
        "BREAK",
        "GOTO",
        "REVERT",
        "BROWSE",
        "GRANT",
        "REVOKE",
        "BULK",
        "GROUP",
        "RIGHT",
        "BY",
        "HAVING",
        "ROLLBACK",
        "CASCADE",
        "HOLDLOCK",
        "ROWCOUNT",
        "CASE",
        "IDENTITY",
        "ROWGUIDCOL",
        "CHECK",
        "IDENTITY_INSERT",
        "RULE",
        "CHECKPOINT",
        "IDENTITYCOL",
        "SAVE",
        "CLOSE",
        "IF",
        "SCHEMA",
        "CLUSTERED",
        "IN",
        "SECURITYAUDIT",
        "COALESCE",
        "INDEX",
        "SELECT",
        "COLLATE",
        "INNER",
        "SEMANTICKEYPHRASETABLE",
        "COLUMN",
        "INSERT",
        "SEMANTICSIMILARITYDETAILSTABLE",
        "COMMIT",
        "INTERSECT",
        "SEMANTICSIMILARITYTABLE",
        "COMPUTE",
        "INTO",
        "SESSION_USER",
        "CONSTRAINT",
        "IS",
        "SET",
        "CONTAINS",
        "JOIN",
        "SETUSER",
        "CONTAINSTABLE",
        "KEY",
        "SHUTDOWN",
        "CONTINUE",
        "KILL",
        "SOME",
        "CONVERT",
        "LEFT",
        "STATISTICS",
        "CREATE",
        "LIKE",
        "SYSTEM_USER",
        "CROSS",
        "LINENO",
        "TABLE",
        "CURRENT",
        "LOAD",
        "TABLESAMPLE",
        "CURRENT_DATE",
        "MERGE",
        "TEXTSIZE",
        "CURRENT_TIME",
        "NATIONAL",
        "THEN",
        "CURRENT_TIMESTAMP",
        "NOCHECK",
        "TO",
        "CURRENT_USER",
        "NONCLUSTERED",
        "TOP",
        "CURSOR",
        "NOT",
        "TRAN",
        "DATABASE",
        "NULL",
        "TRANSACTION",
        "DBCC",
        "NULLIF",
        "TRIGGER",
        "DEALLOCATE",
        "OF",
        "TRUNCATE",
        "DECLARE",
        "OFF",
        "TRY_CONVERT",
        "DEFAULT",
        "OFFSETS",
        "TSEQUAL",
        "DELETE",
        "ON",
        "UNION",
        "DENY",
        "OPEN",
        "UNIQUE",
        "DESC",
        "OPENDATASOURCE",
        "UNPIVOT",
        "DISK",
        "OPENQUERY",
        "UPDATE",
        "DISTINCT",
        "OPENROWSET",
        "UPDATETEXT",
        "DISTRIBUTED",
        "OPENXML",
        "USE",
        "DOUBLE",
        "OPTION",
        "USER",
        "DROP",
        "OR",
        "VALUES",
        "DUMP",
        "ORDER",
        "VARYING",
        "ELSE",
        "OUTER",
        "VIEW",
        "END",
        "OVER",
        "WAITFOR",
        "ERRLVL",
        "PERCENT",
        "WHEN",
        "ESCAPE",
        "PIVOT",
        "WHERE",
        "EXCEPT",
        "PLAN",
        "WHILE",
        "EXEC",
        "PRECISION",
        "WITH",
        "EXECUTE",
        "PRIMARY",
        "WITHIN",
        "EXISTS",
        "PRINT",
        "WRITETEXT",
        "EXIT",
        "PROC"
    };

    // https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-data-type-mappings
    private static readonly Dictionary<string, SqlDbType> _sqlDbTypes = new Dictionary<string, SqlDbType>
    {
        ["bigint"] = SqlDbType.BigInt,
        ["binary"] = SqlDbType.Binary,
        ["bit"] = SqlDbType.Bit,
        ["char"] = SqlDbType.Char,
        ["date"] = SqlDbType.Date,
        ["datetime"] = SqlDbType.DateTime,
        ["datetime2"] = SqlDbType.DateTime2,
        ["datetimeoffset"] = SqlDbType.DateTimeOffset,
        ["decimal"] = SqlDbType.Decimal,
        ["float"] = SqlDbType.Float,
        ["int"] = SqlDbType.Int,
        ["money"] = SqlDbType.Money,
        ["nchar"] = SqlDbType.NChar,
        ["ntext"] = SqlDbType.NText,
        ["numeric"] = SqlDbType.Decimal,
        ["nvarchar"] = SqlDbType.NVarChar,
        ["real"] = SqlDbType.Real,
        ["rowversion"] = SqlDbType.Binary,
        ["smallint"] = SqlDbType.SmallInt,
        ["smallmoney"] = SqlDbType.SmallMoney,
        ["sql_variant"] = SqlDbType.Variant,
        ["text"] = SqlDbType.Text,
        ["time"] = SqlDbType.Time,
        ["timestamp"] = SqlDbType.Timestamp,
        ["tinyint"] = SqlDbType.TinyInt,
        ["uniqueidentifier"] = SqlDbType.UniqueIdentifier,
        ["varbinary"] = SqlDbType.VarBinary,
        ["varchar"] = SqlDbType.VarChar,
        ["xml"] = SqlDbType.Xml
    };

    private static readonly Dictionary<string, Type> SqlClrTypes = new Dictionary<string, Type>
    {
        ["bigint"] = typeof(long),
        ["binary"] = typeof(byte[]),
        ["bit"] = typeof(bool),
        ["char"] = typeof(string),
        ["date"] = typeof(DateTime),
        ["datetime"] = typeof(DateTime),
        ["datetime2"] = typeof(DateTime),
        ["datetimeoffset"] = typeof(DateTimeOffset),
        ["decimal"] = typeof(decimal),
        ["float"] = typeof(double),
        ["int"] = typeof(int),
        ["money"] = typeof(decimal),
        ["nchar"] = typeof(string),
        ["ntext"] = typeof(string),
        ["numeric"] = typeof(decimal),
        ["nvarchar"] = typeof(string),
        ["real"] = typeof(float),
        ["rowversion"] = typeof(byte[]),
        ["smallint"] = typeof(short),
        ["smallmoney"] = typeof(decimal),
        ["sql_variant"] = typeof(object),
        ["text"] = typeof(string),
        ["time"] = typeof(TimeSpan),
        ["timestamp"] = typeof(byte[]),
        ["tinyint"] = typeof(byte),
        ["uniqueidentifier"] = typeof(Guid),
        ["varbinary"] = typeof(byte[]),
        ["varchar"] = typeof(string),
        ["xml"] = typeof(XmlDocument)
    };

    public SqlServerTypeProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public string DatabaseName => _databaseName;
    
    public Domain GetDomain(Settings settings)
    {
        GetDbName();

        _namingConvention = CreateNamingConvention(settings);
        var domain = new Domain(settings, this, _namingConvention);
        domain.Types.AddRange(GetTypes(settings.ExcludedSchemas, domain));

        return domain;
    }

    private INamingConvention CreateNamingConvention(Settings settings)
    {
        if (settings.NamingConventionSettings == null)
        {
            return new PascalCaseNamingConvention(null);
        }

        switch (settings.NamingConventionSettings.DbNamingConvention)
        {
            case DbNamingConvention.ProviderDefault:
            case DbNamingConvention.PascalCase:
                return new PascalCaseNamingConvention(settings.NamingConventionSettings);

            case DbNamingConvention.SnakeCase:
                return new SnakeCaseNamingConvention(settings.NamingConventionSettings);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void GetDbName()
    {
        using (var cn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand("SELECT DB_NAME()", cn))
        {
            cn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                reader.Read();
                _databaseName = GetField<string>(reader, 0);
            }
        }    
    }

    public void GetOperations(Domain domain)
    {
        GetOperationsInternal(domain, true);
    }

    public void AddGeneratedOperation(string text)
    {
        ExecuteCommandText(text);
    }

    public void DropGeneratedOperations(Settings settings, StringBuilder sb)
    {
        var dom = new Domain(settings, this, CreateNamingConvention(settings));
        GetOperationsInternal(dom, false);
        foreach (var operation in dom.Operations.Where(a => a.IsGenerated))
        {
            DropGeneratedOperation(operation, sb);
        }
    }

    public void DropGeneratedTypes(Settings settings, StringBuilder sb)
    {
        using (var cn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand(ListTypesQuery, cn))
        {
            cn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var typeName = GetField<string>(reader, "type_name");
                    var ns = GetField<string>(reader, "schema_name");
                    var attributes = GetAttributes(ns, typeName, "TYPE");
                    if (!string.IsNullOrEmpty(attributes))
                    {
                        dynamic attribJson = ReadAttributes(attributes);
                        if (attribJson.generated == true)
                        {
                            var cmdText = $"DROP TYPE IF EXISTS {ns}.{typeName};";
                            sb.AppendLine(cmdText);
                            using (var dropCn = new SqlConnection(_connectionString))
                            using (var dropCmd = new SqlCommand(cmdText, dropCn))
                            {
                                dropCn.Open();
                                dropCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }
    }

    public string EscapeReservedWord(string name)
    {
        if (_sqlReservedWords.Contains(name.ToUpperInvariant()))
        {
            return $"[{name}]";
        }

        if (name.IndexOf(' ') > 0)
        {
            return $"[{name}]";
        }
        
        return name;
    }

    public string GetCsDbTypeFromDbType(string dbTypeName)
    {
        var type = GetSqlDbTypeFromSqlType(dbTypeName);

        return type.ToString();
    }

    public static SqlDbType GetSqlDbTypeFromSqlType(string sqlTypeName)
    {
        if (_sqlDbTypes.ContainsKey(sqlTypeName))
        {
            return _sqlDbTypes[sqlTypeName];
        }

        Log.Warning("SQL Server type provider doesn't have SqlDbType registered for {SqlTypeName}", sqlTypeName);
        return SqlDbType.Variant;
    }
    
    public string GetSqlName(string entityName)
    {
        return entityName;
    }

    public bool CustomTypeExists(string customTypeName)
    {
        return false;
    }

    public bool IsDateOnly(string typeName)
    {
        return typeName.ToLowerInvariant() == "date";
    }

    public bool IsTimeOnly(string typeName)
    {
        return typeName.ToLowerInvariant() == "time";
    }

    public void AddTestData(List<CodeFile> scripts)
    {
        throw new NotImplementedException();
    }

    public string DefaultNamespace => "dbo";
    public string GetTemplate(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return Util.GetTemplate(templateName, assembly);
    }

    public bool GenerateCustomTypes => false;
    public string FormatOperationParameterName(string operationName, string name)
    {
        return $"@{name.Replace(" ", "")}";
    }

    public string OperationTimestampFunction()
    {
        return "GETUTCDATE()";
    }

    public IParamterPrototype CreateFieldAdapter(Field field, IOperationPrototype operationPrototype)
    {
        return new SqlFieldAdapter(field, operationPrototype, this);
    }

    public bool IncludeIdentityFieldsInInsertStatements => false;

    private void DropGeneratedOperation(Operation op, StringBuilder sb)
    {
        var cmdText = $"DROP {op.ProviderType} IF EXISTS {op.Namespace}.{GetSqlName(op.Name)};";
        sb.AppendLine(cmdText);
        ExecuteCommandText(cmdText);
    }
    
    private IEnumerable<ApplicationType> GetTypes(List<string> excluededSchemas, Domain domain)
    {
        var types = new List<ApplicationType>();

            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                var tables = cn.GetSchema("Tables");

                foreach (DataRow row in tables.Rows)
                {
                    var catalog = row["table_catalog"].ToString();
                    var ns = row["table_schema"].ToString();
                    var name = row["table_name"].ToString();
                    var type = row["table_type"].ToString(); // views will return "VIEW" here
                    var t = new ApplicationType(name, ns, domain);

                    if (!excluededSchemas.Contains(ns) && type == "BASE TABLE")
                    {
                        using (var cmd = new SqlCommand($"select * from {ns}.\"{name}\"", cn))
                        {
                            using (var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
                            {
                                var tableInfo = reader.GetSchemaTable();
                                foreach (DataRow fieldRow in tableInfo.Rows)
                                {
                                    // the IsKey column in this table is incorrect
                                    
                                    var fieldName = SanitizeFieldName(fieldRow["ColumnName"].ToString());
                                    var order = int.Parse(fieldRow["ColumnOrdinal"].ToString());
                                    var providerTypeName = fieldRow["DataTypeName"].ToString();
                                    var size = int.Parse(fieldRow["ColumnSize"].ToString());
                                    var isNullable = (bool)fieldRow["AllowDBNull"];
                                    var isIdentity = (bool)fieldRow["IsIdentity"];
                                    var isExpression = fieldRow["IsExpression"];
                                    var isReadOnly = fieldRow["IsReadOnly"];
                                    Type clrType = typeof(object);
                                    if (fieldRow["DataType"] != DBNull.Value)
                                    {
                                        clrType = (System.Type)fieldRow["DataType"];
                                        if (providerTypeName == "xml")
                                        {
                                            // for some reason the mapping above considers XML to be "string"
                                            // so we need to correct it
                                            clrType = GetClrTypeForSqlType(providerTypeName);
                                        }
                                    }
                                    else
                                    {
                                        Log.Warning("Field {FieldName} on table {TableName} of db type {DbTypeName} does not have a direct .NET type representation", fieldName, name, providerTypeName);
                                    }
                                    
                                    if (isNullable && !ClrTypeIsNullable(clrType))
                                    {
                                        // we need to change the CLR type to make it nullable
                                        clrType = MakeClrTypeNullable(clrType);
                                    }
                                    
                                    t.Fields.Add(new Field(t) { Name = fieldName, Order = order, Size = SanitizeSize(size, providerTypeName), ProviderTypeName = providerTypeName, ClrType = clrType, IsRequired = !isNullable, IsGenerated = isIdentity});
                                }
                            }
                        }
                    
                        GetTypeAttributes(t);
                        
                        GetAdditionalFieldInfoFromInformationSchema(catalog, ns, name, cn, t);
                        GetPrimaryKeyInfoFromInformationSchema(catalog, ns, name, cn, t);
                        
                        types.Add(t);
                    }
                    else
                    {
                        Log.Debug("{Schema}.{TableName} was excluded because the schema is being excluded", ns, name);
                    }
                }

                // now that we've done an initial pass on loading the types we need to construct the relationships between them
                foreach (var applicationType in types)
                {
                    GetForeignKeyInfoFromInformationSchema(cn, applicationType, types);
                }
            }

            return types;
    }

    private void GetAdditionalFieldInfoFromInformationSchema(string? catalog, string ns, string? name, SqlConnection cn, ApplicationType applicationType)
    {
        using (var cmd = new SqlCommand("select * from sys.columns c where object_id = OBJECT_ID(@fullName)", cn))
        {
            cmd.Parameters.Add(new SqlParameter("@fullName", $"{ns}.{name}"));
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var fieldName = GetField<string>(reader, "name");
                    var isComputed = GetField<bool>(reader, "is_computed");
                    var field = applicationType.GetFieldByName(fieldName);
                    field.IsComputed = isComputed;
                }
            }
        }
    }

    private void GetTypeAttributes(ApplicationType type)
    {
        var attributesJson = GetAttributes(type.Namespace, type.Name, "table");
        var attributes = ReadAttributes(attributesJson);
        if (attributes != null)
        {
            type.Attributes = attributes;
        }

        foreach (var field in type.Fields)
        {
            var fieldAttributes = GetFieldAttributes(field);
            if (fieldAttributes != null)
            {
                field.Attributes = fieldAttributes;
            }
        }
    }

    private dynamic GetFieldAttributes(Field field)
    {
        return ReadAttributes(GetFieldAttributesString(field));
    }

    private string? GetOperationAttributes(Operation op, string operationType)
    {
        return GetAttributes(op.Namespace, op.Name, operationType);
    }

    private string? GetAttributes(string nameSpace, string name, string entityType)
    {
        using (var cn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand(
                   "select * FROM fn_listextendedproperty('codegen_meta', 'schema', @schema, @type, @name, default, default);",
                   cn))
        {
            cn.Open();

            cmd.Parameters.Add(new SqlParameter("@schema", nameSpace));
            cmd.Parameters.Add(new SqlParameter("@type", entityType));
            cmd.Parameters.Add(new SqlParameter("@name", name));

            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var attributes = reader["value"].ToString();
                return attributes;
            }

            return null;
        }
    }
    
    private string? GetFieldAttributesString(Field field)
    {
        using (var cn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand(
                   "select * FROM fn_listextendedproperty('codegen_meta', 'schema', @schema, 'TABLE', @name, 'COLUMN', @fieldName);",
                   cn))
        {
            cn.Open();

            cmd.Parameters.Add(new SqlParameter("@schema", field.Type.Namespace));
            cmd.Parameters.Add(new SqlParameter("@fieldName", field.Name ));
            cmd.Parameters.Add(new SqlParameter("@name", field.Type.Name));

            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var attributes = reader["value"].ToString();
                return attributes;
            }

            return null;
        }
    }

    private void GetOperationsInternal(Domain domain, bool getAllDetails)
    {
        using (var cn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand(ProceduresQuery, cn))
        {
            cn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var name = reader["SPECIFIC_NAME"].ToString();
                    try
                    {
                        var ns = reader["SPECIFIC_SCHEMA"].ToString();
                        if (!domain.ExcludedSchemas.Contains(ns))
                        {
                            var resultType = reader["DATA_TYPE"].ToString();
                            var routineType = reader["ROUTINE_TYPE"].ToString();
                            
                            var op = new Operation {Name = name, Namespace = ns, ProviderType = routineType};
                            var attributes = GetOperationAttributes(op, routineType);
                            PopulateOperationAttributes(op, attributes);

                            if (getAllDetails)
                            {
                                if (op.Attributes?.applicationtype != null)
                                {
                                    SetOperationRelatedType(op, domain);
                                }
                                
                                op.Parameters.AddRange(ReadParameters(domain, op));
                                if (op.RelatedType != null)
                                {
                                    UpdateParameterNullabilityFromApplicationType(op);
                                }

                                op.Returns = GetReturnForOperation(resultType, domain, op, routineType);
                            }

                            domain.Operations.Add(op);
                        }
                        else
                        {
                            Log.Debug("{Schema}.{OperationName} is being excluded because of the schema", ns, name);
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Unexpected error reading operation {name}\n{ex}");
                        throw;
                    }
                }
            }
        }
    }
    
    private void UpdateParameterNullabilityFromApplicationType(Operation op)
    {
        if (op.RelatedType == null)
        {
            return;
        }
            
        foreach (var prm in op.Parameters)
        {
            var trimmedName = prm.Name.TrimStart('@');
            var fld = op.RelatedType.Fields.FirstOrDefault(f => f.Name == trimmedName);
            if (fld != null)
            {
                prm.UpdateFromField(fld);
            }
            else
            {
                if (_namingConvention.IsSecurityUserIdParameterName(prm.Name) && !prm.IsNullable)
                {
                    // we make the current user Id nullable in case there is no current user (AKA anon) and assume that any db functions will do the right thing if this parameter is not provided
                    // and either return an error, or return data appropriate for an unauthenticated user
                    prm.MakeClrTypeNullable();
                }
                else
                {
                    // since this matching above is done by name, it misses some things e.g. where the parameter is called id_param or IdParam and the field is called id.
                    // here we 'fall back' to try to fix that
                    var paramFld = op.RelatedType.Fields.FirstOrDefault(f => _namingConvention.CreateParameterNameFromFieldName(f.Name) == trimmedName);
                    if (paramFld != null)
                    {
                        prm.UpdateFromField(paramFld);
                    }
                }
            }
        }
    }
    

    private OperationReturn GetReturnForOperation(string? resultType, Domain domain, Operation op, string routineType)
    {
        
        if (routineType == OperationTypes.Procedure)
        {
            var fields = GetReturnFieldsForProcedure(op);
            if (fields.Count == 1)
            {
                if (op.Attributes?.single_result == true)
                {
                    return new OperationReturn()
                    {
                        ReturnType = ReturnType.Primitive,
                        ClrReturnType = fields.First().ClrType,
                        Multiple = false
                    };
                }
                else
                {
                    return new OperationReturn()
                    {
                        ReturnType = ReturnType.Primitive,
                        ClrReturnType = fields.First().ClrType, // should this be 'array' of is multiple enough to cover it
                        Multiple = true
                    };
                }
            }
            else
            {
                return GetReturnForOperationFromFields(domain, op, fields);
            }
        }
        else
        {
            if (resultType == "TABLE")
            {
                var fields = GetReturnFieldsForFunction(op);
                if (fields.Count == 1)
                {
                    if (op.Attributes?.single_result == true)
                    {
                        return new OperationReturn()
                        {
                            ReturnType = ReturnType.Primitive,
                            ClrReturnType = fields.First().ClrType,
                            Multiple = false
                        };
                    }
                    else
                    {
                        return new OperationReturn()
                        {
                            ReturnType = ReturnType.Primitive,
                            ClrReturnType = fields.First().ClrType, // should this be 'array' of is multiple enough to cover it
                            Multiple = true
                        };
                    }
                }
                else
                {
                    return GetReturnForOperationFromFields(domain, op, fields);
                }
            }
            else
            {
                if (SqlClrTypes.ContainsKey(resultType.ToLowerInvariant()))
                {
                    return new OperationReturn()
                    {
                        ReturnType = ReturnType.Primitive,
                        ClrReturnType = GetClrTypeForSqlType(resultType),
                        Multiple = false
                    };
                }
            }
        }
        
        return null;
    }

    private List<Field> GetReturnFieldsForProcedure(Operation op)
    {
        var fields = new List<Field>();

        using (var cn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand(@"exec Sp_describe_first_result_set @tsql", cn))
        {
            cmd.Parameters.Add(new SqlParameter("@tsql", $"[{op.Namespace}].[{op.Name}]"));
            cn.Open();
            
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var name = reader["name"].ToString();
                var providerDataType = SanitizeStoredProcedureSystemTypeName(reader["system_type_name"].ToString());
                var order = (int)reader["column_ordinal"];
                var isNullable = (bool)reader["is_nullable"]; 
                int? maxLength = reader["max_length"] == DBNull.Value
                    ? null
                    : (short)reader["max_length"];
                maxLength = SanitizeSize(maxLength, providerDataType);
                var clrType = GetClrTypeForSqlType(providerDataType);

                fields.Add(new Field(null)
                {
                    Name = name, ProviderTypeName = providerDataType, Order = order, IsRequired = !isNullable,
                    Size = maxLength, ClrType = clrType
                });
            }
        }            
        
        return fields;
    }
    
    private string SanitizeStoredProcedureSystemTypeName(string? systemTypeName)
    {
        if (!string.IsNullOrEmpty(systemTypeName) && systemTypeName.IndexOf('(') > 0)
        {
            return systemTypeName.Substring(0, systemTypeName.IndexOf('('));
        }

        return systemTypeName;
    }

    private static int? SanitizeSize(int? size, string providerDataType)
    {
        // for parameters the size for varchar/varbinary max will be reported as -1
        if (size == -1)
        {
            return null;
        }

        // for fields the size for varchar/varbinary max will be reported as 2147483647
        if (size == 2147483647)
        {
            return null;
        }
        
        // we only care about the max length for some types - for others like int and datetime it is just telling us how much storage the type uses
        // which we don't really need to concern ourselves with
        var sizeSpecificTypes = new string[]
            { "char", "binary", "nchar", "nvarchar", "varbinary", "varchar" };
        if (sizeSpecificTypes.Contains(providerDataType))
        {
            return size;
        }

        return null;
    }
    
    private static OperationReturn GetReturnForOperationFromFields(Domain domain, Operation op, List<Field> fields)
    {
        var existingType = domain.FindTypeByFields(fields, op, true);
        if (existingType != null)
        {
            return new OperationReturn()
            {
                ReturnType = ReturnType.ApplicationType,
                SimpleReturnType = existingType
            };
        }
        else
        {
            // instead of using an attribute here we could do some fancy inferencing too
            var name = domain.NamingConvention.CreateResultTypeNameForOperation(op.Name);
            if (op.CustomReturnTypeName != null)
            {
                name = op.CustomReturnTypeName;
                // TODO - we could check that the fields match here too?
            }

            var existingReturnType = domain.ResultTypes.SingleOrDefault(t => t.Name == name);
            if (existingReturnType == null)
            {
                // possibly inaccurate since it just picks the related type of the operation it is returned by
                var result = new ResultType(name, op.Namespace, op.RelatedType, false, domain);
                result.Operations.Add(op);

                var newFields = fields.Select(f => new Field(result)
                {
                    // re-create the fields now that we know who the parent type will be
                    Attributes = f.Attributes,
                    Name = f.Name,
                    ProviderTypeName = f.ProviderTypeName,
                    ClrType = f.ClrType,
                    Order = f.Order,
                    IsRequired = f.IsRequired,
                    Size = f.Size
                });
                result.Fields.AddRange(newFields);

                if (op.Attributes?.applicationtype != null)
                {
                    domain.UpdateResultFieldPropertiesFromApplicationType(op, result);
                }

                domain.ResultTypes.Add(result);
                
                return new OperationReturn()
                {
                    ReturnType = ReturnType.CustomType,
                    SimpleReturnType = result,
                    Multiple = true
                };
            }
            else
            {
                existingReturnType.Operations.Add(op);

                {
                    return new OperationReturn()
                    {
                        ReturnType = ReturnType.CustomType,
                        SimpleReturnType = existingReturnType,
                        Multiple = true
                    };
                }
            }
        }
    }

    private List<Field> GetReturnFieldsForFunction(Operation op)
    {
        var fields = new List<Field>();
        
        const string commandText = @"SELECT r.ROUTINE_NAME AS FunctionName,
       r.DATA_TYPE AS FunctionReturnType,
       rc.COLUMN_NAME,
       rc.DATA_TYPE as COL_DATA_TYPE,
       rc.ORDINAL_POSITION,
       rc.IS_NULLABLE ,
       rc.CHARACTER_MAXIMUM_LENGTH 
FROM   INFORMATION_SCHEMA.ROUTINES r
LEFT JOIN INFORMATION_SCHEMA.ROUTINE_COLUMNS rc ON rc.TABLE_NAME = r.ROUTINE_NAME
WHERE r.DATA_TYPE = 'TABLE'
AND r.ROUTINE_NAME = @name
AND r.SPECIFIC_SCHEMA= @schema
ORDER BY r.ROUTINE_NAME, rc.ORDINAL_POSITION;";

        using (var cn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand(commandText, cn))
        {
            cn.Open();
            cmd.Parameters.Add(new SqlParameter("@name", op.Name));
            cmd.Parameters.Add(new SqlParameter("@schema", op.Namespace));

            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var name = reader["COLUMN_NAME"].ToString();
                var providerDataType = reader["COL_DATA_TYPE"].ToString();
                var order = (int)reader["ORDINAL_POSITION"];
                var isNullable = reader["IS_NULLABLE"].ToString(); // this comes back as a yes/no string
                int? maxLength = reader["CHARACTER_MAXIMUM_LENGTH"] == DBNull.Value
                    ? null
                    : (int)reader["CHARACTER_MAXIMUM_LENGTH"];
                var clrType = GetClrTypeForSqlType(providerDataType);

                fields.Add(new Field(null)
                {
                    Name = name, ProviderTypeName = providerDataType, Order = order, IsRequired = isNullable == "NO",
                    Size = maxLength, ClrType = clrType
                });
            }
        }

        return fields;
    }

    private IEnumerable<Parameter> ReadParameters(Domain domain, Operation op)
    {
        var parameters = new List<Parameter>();
        
        using (var cn = new SqlConnection(_connectionString))
        {
            cn.Open();
            using (var cmd = new SqlCommand(
                       "SELECT * FROM INFORMATION_SCHEMA.PARAMETERS p WHERE p.SPECIFIC_SCHEMA = @schema and p.SPECIFIC_NAME = @name and p.PARAMETER_MODE = 'IN' ORDER BY p.ORDINAL_POSITION",
                       cn))
            {
                cmd.Parameters.Add(new SqlParameter("@schema", op.Namespace));
                cmd.Parameters.Add(new SqlParameter("@name", op.Name));
                
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = GetField<string>(reader, "PARAMETER_NAME").TrimStart('@');
                        var order = GetField<int>(reader, "ORDINAL_POSITION");
                        var dataType = GetField<string>(reader, "DATA_TYPE");
                        var clrType = GetClrTypeForSqlType(dataType);

                        if (dataType == "table type")
                        {
                            var customTypeSchema = GetField<string>(reader, "USER_DEFINED_TYPE_SCHEMA");
                            dataType = GetField<string>(reader, "USER_DEFINED_TYPE_NAME");
                            clrType = typeof(ResultType);

                            var resultType = domain.ResultTypes.SingleOrDefault(t =>
                                t.Name == dataType && t.Namespace == customTypeSchema);
                            if (resultType == null)
                            {
                                ReadCustomOperationType(dataType, customTypeSchema, domain, op);
                            }
                            else
                            {
                                resultType.Operations.Add(op);
                            }
                        }
                        
                        var parameter = new Parameter(domain, op, name, clrType, dataType) { Order = order };
                        parameters.Add(parameter);
                    }
                }
            }
        }

        return parameters;
    }

    private void ReadCustomOperationType(string dataType, string customTypeSchema, Domain domain, Operation operation)
    {
        var attributes = GetAttributes(customTypeSchema, dataType, "TYPE");
        var appType = operation.RelatedType;
        dynamic attribJson = null;
        if (!string.IsNullOrEmpty(attributes))
        {
            attribJson = ReadAttributes(attributes);
            if (!string.IsNullOrEmpty(attribJson?.applicationtype.ToString()))
            {
                appType = domain.Types.SingleOrDefault(t => t.Name == attribJson?.applicationtype.ToString());
            }
        }
        
        var result = new ResultType(dataType, customTypeSchema, operation.RelatedType, true, domain);
        result.Attributes = attribJson;
        
        using (var cn = new SqlConnection(_connectionString))
        {
            cn.Open();
            using (var cmd = new SqlCommand(UserDefinedTypeColumnQuery, cn))
            {
                cmd.Parameters.Add(new SqlParameter("@Schema", customTypeSchema));
                cmd.Parameters.Add(new SqlParameter("@TypeName", dataType));
                
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var fld = new Field(result);
                        fld.Name = SanitizeFieldName(GetField<string>(reader, "name"));
                        fld.Order = GetField<int>(reader, "column_id");
                        fld.ProviderTypeName = GetField<string>(reader, "system_type_name");
                        fld.ClrType = GetClrTypeForSqlType(fld.ProviderTypeName);
                        int? maxLength = reader["max_length"] == DBNull.Value
                            ? null
                            : (short)reader["max_length"];
                        fld.Size = SanitizeSize(maxLength, fld.ProviderTypeName);
                        
                        result.Fields.Add(fld);
                    }
                }
            }
        }

        if (operation.Attributes?.applicationtype != null)
        {
            domain.UpdateResultFieldPropertiesFromApplicationType(operation, result);
        }
        
        result.Operations.Add(operation);
        domain.ResultTypes.Add(result);
    }

    private const string UserDefinedTypeColumnQuery = @"select
    c.*,
    type_name(c.system_type_id) as system_type_name
    from sys.table_types t
    inner join sys.schemas AS s
        ON t.[schema_id] = s.[schema_id]
    inner join sys.columns c
        on c.[object_id] = t.type_table_object_id
    where is_user_defined = 1
        AND s.name = @Schema
        AND t.name = @TypeName
    ";
    
    private dynamic? ReadAttributes(string attributes)
    {
        if (string.IsNullOrEmpty(attributes))
        {
            return null;
        }

        try
        {
            return JToken.Parse(attributes);
        }
        catch (JsonReaderException)
        {
            if (!string.IsNullOrEmpty(attributes) && attributes.StartsWith('{'))
            {
                Log.Warning("attribute string {Attributes} was not valid JSON", attributes);
            }
            return null; // description was not valid JSON
        }
    }
    
    private void PopulateOperationAttributes(Operation op, string description)
    {
        if (!string.IsNullOrEmpty(description))
        {
            var attributes = ReadAttributes(description);
            if (attributes != null)
            {
                op.Attributes = attributes;
            }
        }
    }

    private string SanitizeFieldName(string? fieldName)
    {
        return fieldName; // TODO - any SQL-server-specific name sanitization here
    }
    
    private static bool ClrTypeIsNullable(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }

    private static Type MakeClrTypeNullable(Type type)
    {
        return typeof(Nullable<>).MakeGenericType(type);
    }

    private static Type GetClrTypeForSqlType(string sqlType)
    {
        var canonicalType = sqlType.ToLowerInvariant();
        if (SqlClrTypes.ContainsKey(canonicalType))
        {
            return SqlClrTypes[canonicalType];
        }

        return typeof(object);
    }
    
    private void SetOperationRelatedType(Operation op, Domain domain)
    {
        var appTypeName = op.Attributes.applicationtype.ToString();
        var appType = domain.Types.FirstOrDefault(t => t.Name == appTypeName);
        if (appType == null)
        {
            Log.Error($"Unable to find application type {appTypeName} for operation {op.Name}");
        }

        op.RelatedType = appType;
    }
    
    protected virtual T GetField<T>(DbDataReader reader, string fieldName)
    {
        if (reader[fieldName] == DBNull.Value)
        {
            return default(T);
        }
        return (T)reader[fieldName];
    }

    protected virtual T GetField<T>(DbDataReader reader, int position)
    {
        if (reader[position] == DBNull.Value)
        {
            return default(T);
        }
        return (T)reader[position];
    }
    
    private static void GetPrimaryKeyInfoFromInformationSchema(string catalog, string ns, string name,
        SqlConnection cn, ApplicationType type)
    {
        using (var cmd = new SqlCommand(
                   $@"select kcu.column_name, c.constraint_type, c.table_name from information_schema.TABLE_CONSTRAINTS c
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
ON c.table_name = kcu.table_name
AND c.table_schema= kcu.table_schema
AND c.constraint_name = kcu.constraint_name
AND c.constraint_catalog = kcu.constraint_catalog
AND c.table_catalog = kcu.table_catalog
AND c.table_name = '{name}'
AND c.table_catalog = '{catalog}'
AND c.table_schema = '{ns}'
",
                   cn))
        {
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var fieldName = reader["column_name"].ToString();
                    var field = type.Fields.FirstOrDefault(f => f.Name == fieldName);
                    if (field == null)
                    {
                        throw new DataException("Can't find field " + fieldName);
                    }
                    if (reader["constraint_type"].ToString() == "PRIMARY KEY")
                    {
                        field.IsKey = true;
                    }
                }
            }
        }
    }
    
    private static void GetForeignKeyInfoFromInformationSchema(SqlConnection cn, ApplicationType type, List<ApplicationType> allTypes)
        {
            using (var cmd = new SqlCommand(
                $@"
SELECT
KCU1.CONSTRAINT_NAME AS FK_CONSTRAINT_NAME
, KCU1.TABLE_NAME AS FK_TABLE_NAME
, KCU1.COLUMN_NAME AS FK_COLUMN_NAME
, KCU1.ORDINAL_POSITION AS FK_ORDINAL_POSITION
, KCU2.CONSTRAINT_NAME AS REFERENCED_CONSTRAINT_NAME
, KCU2.TABLE_SCHEMA AS REFERENCED_TABLE_SCHEMA
, KCU2.TABLE_NAME AS REFERENCED_TABLE_NAME
, KCU2.COLUMN_NAME AS REFERENCED_COLUMN_NAME
, KCU2.ORDINAL_POSITION AS REFERENCED_ORDINAL_POSITION
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS RC

INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KCU1
ON KCU1.CONSTRAINT_CATALOG = RC.CONSTRAINT_CATALOG
AND KCU1.CONSTRAINT_SCHEMA = RC.CONSTRAINT_SCHEMA
AND KCU1.CONSTRAINT_NAME = RC.CONSTRAINT_NAME

INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KCU2
ON KCU2.CONSTRAINT_CATALOG = RC.UNIQUE_CONSTRAINT_CATALOG
AND KCU2.CONSTRAINT_SCHEMA = RC.UNIQUE_CONSTRAINT_SCHEMA
AND KCU2.CONSTRAINT_NAME = RC.UNIQUE_CONSTRAINT_NAME
AND KCU2.ORDINAL_POSITION = KCU1.ORDINAL_POSITION
WHERE  KCU1.TABLE_NAME = '{type.Name}'
AND KCU1.TABLE_SCHEMA = '{type.Namespace}'
",
                cn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var fieldName = reader["fk_column_name"].ToString();
                        var field = type.GetFieldByName(fieldName);
                        if (field == null)
                        {
                            throw new DataException("Can't find field " + fieldName + " when creating foreign key relationship");
                        }

                        var refTypeName = reader["referenced_table_name"].ToString();
                        var refNs = reader["REFERENCED_TABLE_SCHEMA"].ToString();
                        var refType = allTypes.FirstOrDefault(a => a.Name == refTypeName && a.Namespace == refNs);
                        if (refType == null)
                        {
                            throw new DataException($"Can't find related type {refTypeName} in Namespace {refNs} when creating foreign key relationship");
                        }

                        var refFieldName = reader["REFERENCED_COLUMN_NAME"].ToString();
                        var refField = refType.GetFieldByName(refFieldName);

                        if (refField == null)
                        {
                            throw new DataException($"Can't find related type field {refFieldName} when creating relationship");
                        }

                        field.ReferencesType = refType;
                        field.ReferencesTypeField = refField;
                    }
                }
            }
        }

    private void ExecuteCommandText(string text, bool log = true)
    {
        Log.Debug("Executing {Sql}", text);
        try
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                var splitter = new SqlCommandSplitter();
                var commands = splitter.SplitScriptIntoCommands(text);
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
        catch (Exception ex)
        {
            if (log)
            {
                Log.Error(ex, ex.Message + Environment.NewLine + "-----------------------------------------" + Environment.NewLine + "Attempted To Execute: " + Environment.NewLine + text + Environment.NewLine);
            }
            throw;
        }
    }
    
    private const string ProceduresQuery = "Select * from Information_Schema.ROUTINES";

    private const string ListTypesQuery = @"
select st.name as 'type_name', s.name as 'schema_name'  from sys.types st
inner join sys.schemas s 
on st.schema_id = s.schema_id 
where st.is_user_defined = 1 and st.is_table_type = 1
";
}