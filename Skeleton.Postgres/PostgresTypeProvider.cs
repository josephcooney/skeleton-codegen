using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Skeleton.Model;
using Skeleton.Model.Operations;
using Skeleton.Templating;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using Serilog;
using Skeleton.Model.NamingConventions;
using Constraint = Skeleton.Model.Constraint;

namespace Skeleton.Postgres
{
    public class PostgresTypeProvider : ITypeProvider
    {
        private readonly string _connectionString;
        private static Dictionary<string, NpgsqlDbType> _postgresNpgSqlTypes;
        private INamingConvention _namingConvention;        

        static PostgresTypeProvider()
        {
            _postgresNpgSqlTypes = new Dictionary<string, NpgsqlDbType>
            {
                ["boolean"] = NpgsqlDbType.Boolean,
                ["smallint"] = NpgsqlDbType.Smallint,
                ["integer"] = NpgsqlDbType.Integer,
                ["bigint"] = NpgsqlDbType.Bigint,
                ["real"] = NpgsqlDbType.Real,
                ["double precision"] = NpgsqlDbType.Double,
                ["numeric"] = NpgsqlDbType.Numeric,
                ["money"] = NpgsqlDbType.Money,
                ["text"] = NpgsqlDbType.Text,
                ["character varying"] = NpgsqlDbType.Varchar,
                ["character"] = NpgsqlDbType.Char,
                ["citext"] = NpgsqlDbType.Citext,
                ["json"] = NpgsqlDbType.Json,
                ["jsonb"] = NpgsqlDbType.Jsonb,
                ["xml"] = NpgsqlDbType.Xml,
                ["point"] = NpgsqlDbType.Point,
                ["lseg"] = NpgsqlDbType.LSeg,
                ["path"] = NpgsqlDbType.Path,
                ["polygon"] = NpgsqlDbType.Polygon,
                ["line"] = NpgsqlDbType.Line,
                ["circle"] = NpgsqlDbType.Circle,
                ["box"] = NpgsqlDbType.Box,
                ["hstore"] = NpgsqlDbType.Hstore,
                ["uuid"] = NpgsqlDbType.Uuid,
                ["cidr"] = NpgsqlDbType.Cidr,
                ["inet"] = NpgsqlDbType.Inet,
                ["macaddr"] = NpgsqlDbType.MacAddr,
                ["tsquery"] = NpgsqlDbType.TsQuery,
                ["tsvector"] = NpgsqlDbType.TsVector,
                ["date"] = NpgsqlDbType.Date,
                ["interval"] = NpgsqlDbType.Interval,
                ["timestamp"] = NpgsqlDbType.Timestamp,
                ["timestamp without time zone"] = NpgsqlDbType.Timestamp,
                ["timestamp with time zone"] = NpgsqlDbType.TimestampTz,
                ["time"] = NpgsqlDbType.Time,
                ["time with time zone"] = NpgsqlDbType.TimeTz,
                ["bytea"] = NpgsqlDbType.Bytea,
                ["oid"] = NpgsqlDbType.Oid,
                ["xid"] = NpgsqlDbType.Xid,
                ["cid"] = NpgsqlDbType.Cid,
                ["oidvector"] = NpgsqlDbType.Oidvector,
            };
        }
        
        public PostgresTypeProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Domain GetDomain(Settings settings)
        {
            _namingConvention = CreateNamingConvention(settings);
            var domain = new Domain(settings, this, _namingConvention);
            domain.Types.AddRange(GetTypes(settings.ExcludedSchemas, domain));

            return domain;
        }

        private INamingConvention CreateNamingConvention(Settings settings)
        {
            if (settings.NamingConventionSettings == null)
            {
                return new SnakeCaseNamingConvention(null);
            }

            switch (settings.NamingConventionSettings.DbNamingConvention)
            {
                case DbNamingConvention.ProviderDefault:
                case DbNamingConvention.SnakeCase:
                    return new SnakeCaseNamingConvention(settings.NamingConventionSettings);
                case DbNamingConvention.PascalCase:
                    return new PascalCaseNamingConvention(settings.NamingConventionSettings);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private List<ApplicationType> GetTypes(List<string> excluededSchemas, Domain domain)
        {
            var types = new List<ApplicationType>();

            using (var cn = new NpgsqlConnection(_connectionString))
            {
                cn.Open();
                var tables = cn.GetSchema("Tables");

                foreach (DataRow row in tables.Rows)
                {
                    var catalog = row["table_catalog"].ToString();
                    var ns = row["table_schema"].ToString();
                    var name = row["table_name"].ToString();
                    var t = new ApplicationType(name, ns, domain);

                    if (!excluededSchemas.Contains(ns))
                    {
                        using (var cmd = new NpgsqlCommand($"select * from {ns}.\"{name}\"", cn))
                        {
                            using (var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
                            {
                                var tableInfo = reader.GetSchemaTable();
                                foreach (DataRow fieldRow in tableInfo.Rows)
                                {
                                    var fieldName = SanitizeFieldName(fieldRow["ColumnName"].ToString());
                                    var order = int.Parse(fieldRow["ColumnOrdinal"].ToString());
                                    var providerTypeName = fieldRow["DataTypeName"].ToString();
                                    var size = int.Parse(fieldRow["ColumnSize"].ToString());
                                    var clrType = (System.Type)fieldRow["DataType"];

                                    // AllowDbNull doesn't seem to be accurate for postgres
                                    // IsIdentity also doesn't seem accurate, however it does accord with what information_schema.columns contains for that table 

                                    t.Fields.Add(new Field(t) { Name = fieldName, Order = order, Size = GetFieldSize(size), ProviderTypeName = providerTypeName, ClrType = clrType});
                                }
                            }
                        }
                    
                        GetAdditionalFieldInfoFromInformationSchema(catalog, ns, name, cn, t);
                        GetPrimaryKeyInfoFromInformationSchema(catalog, ns, name, cn, t);
                        GetUniqueConstraintsFromInformationSchema(catalog, ns, name, cn, t);

                        types.Add(t);
                    }
                    else
                    {
                        Log.Debug("{Schema}.{TableName} was excluded because the schema is being excluded", ns, name);
                    }
                }

                GetTypeAttributes(types);

                // now that we've done an initial pass on loading the types we need to construct the relationships between them
                foreach (var applicationType in types)
                {
                    GetForeignKeyInfoFromInformationSchema(cn, applicationType, types);
                }
            }


            return types;
        }

        private string SanitizeFieldName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            // fix up quoted names
            if (name.StartsWith("\""))
            {
                return name.Replace("\"", "");
            }

            return name;
        }
        
        private string SanitizeObjectName(string name)
        {

            // fix up quoted names
            if (name.StartsWith("\""))
            {
                return name.Replace("\"", "");
            }

            return name;
        }

        private int? GetFieldSize(int size)
        {
            if (size < 0)
            {
                return null;
            }

            return size;
        }

        public void GetOperations(Domain domain)
        {
            GetOperationsInternal(domain, true);
        }
        
        private void GetOperationsInternal(Domain domain, bool getAllDetails)
        {
            using (var cn = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand(ProceduresQuery, cn))
            {
                cn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader["name"].ToString();
                        try
                        {
                            var ns = reader["schema"].ToString();
                            if (!domain.ExcludedSchemas.Contains(ns))
                            {
                                var resultType = reader["result_type"].ToString();

                                var op = new Operation {Name = name, Namespace = ns};
                                var description = GetField<string>(reader, "description");

                                PopulateOperationAttributes(op, description);

                                if (getAllDetails)
                                {
                                    if (op.Attributes?.applicationtype != null)
                                    {
                                        SetOperationRelatedType(op, domain);
                                    }
                                    
                                    var parameters = reader["argument_types"].ToString();
                                    if (!string.IsNullOrEmpty(parameters))
                                    {
                                        op.Parameters.AddRange(ReadParameters(parameters, domain, op));
                                        if (op.RelatedType != null)
                                        {
                                            UpdateParameterNullabilityFromApplicationType(op);
                                        }
                                    }

                                    op.Returns = GetReturnForOperation(resultType, domain, op);
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

            GetAttributesForResultTypes(domain);
        }

        private void GetAttributesForResultTypes(Domain domain)
        {
            using (var cn = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand(ListTypesQuery, cn))
            {
                cn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var ns = GetField<string>(reader, "nspname");
                        var typeName = SanitizeObjectName(GetField<string>(reader, "obj_name"));
                        var attributes = GetField<string>(reader, "description");

                        var resultType = domain.ResultTypes.SingleOrDefault(rt => rt.Namespace == ns && rt.Name == typeName);
                        if (resultType == null)
                        {
                            Log.Warning("Custom type {TypeName} was found in the database but was not found in the domain", typeName);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(attributes))
                            {
                                dynamic attribJson = ReadAttributes(attributes, typeName);
                                resultType.Attributes = attribJson;
                            }
                        }
                    }
                }
            }
        }

        public void AddGeneratedOperation(string text)
        {
            ExecuteCommandText(text);
        }

        public void DropGenerated(Domain domain)
        {
            foreach (var operation in domain.Operations.Where(a => a.IsGenerated))
            {
                DropGeneratedOperation(operation);
            }            
            DropGeneratedTypes(domain);
        }

        public CodeFile GenerateDropStatements(Domain oldDomain, Domain newDomain)
        {
            var codeFile = new CodeFile() { Name = "drop_generated.sql" };
            var sb = new StringBuilder();
            foreach (var resultType in oldDomain.ResultTypes)
            {
                if (resultType.IsGenerated && !newDomain.ResultTypes.Any(rt => rt.Namespace == resultType.Namespace && rt.Name == resultType.Name))
                {
                    sb.AppendLine($"-- dropping {resultType.Name}");
                    sb.AppendLine(GetDropTypeCommandText(resultType));
                    sb.AppendLine("");
                }
            }

            foreach (var operation in oldDomain.Operations)
            {
                if (operation.IsGenerated &&
                    !newDomain.Operations.Any(o => o.Namespace == operation.Namespace && o.Name == operation.Name))
                {
                    sb.AppendLine($"-- dropping {operation.Name}");
                    sb.AppendLine(GetDropOperationCommandText(operation));
                    sb.AppendLine("");
                }
            }
            
            codeFile.Contents = sb.ToString();
            return codeFile;
        }

        private void DropGeneratedTypes(Domain domain)
        {
            foreach (var resultType in domain.ResultTypes.Where(rt => rt.IsGenerated))
            {
                Log.Debug("Dropping Type {TypeName}", resultType.Name);
                var cmdText = GetDropTypeCommandText(resultType);
                using (var dropCn = new NpgsqlConnection(_connectionString))
                using (var dropCmd = new NpgsqlCommand(cmdText, dropCn))
                {
                    dropCn.Open();
                    dropCmd.ExecuteNonQuery();
                }
                
            }
        }

        private string GetDropTypeCommandText(ResultType type)
        {
            return $"DROP TYPE IF EXISTS {type.Namespace}.{type.Name} CASCADE;";
        }

        public string EscapeReservedWord(string name)
        {
            if (SqlReservedWords.Contains(name.ToUpperInvariant()) || name.ToLowerInvariant() != name)
            {
                return $"\"{name}\"";
            }

            return name;
        }

        public string EscapeSqlName(string name)
        {
            if (name.ToLowerInvariant() != name)
            {
                return $"\"{name}\"";
            }

            return name;
        }

        public string GetCsDbTypeFromDbType(string dbTypeName)
        {
            var result = GetNpgsqlDbTypeFromPostgresType(dbTypeName);

            if (result == NpgsqlDbType.TimestampTz)
            {
                // the capitalisation of Tz keeps getting messed up, and emitted as TZ because they both have the same enum value
                return "TimestampTz"; 
            }

            return result.ToString();
        }

        // postgres has a name length limit of 63 bytes. This assumes you're not using unicode characters for db entity names
        private const int NameLengthLimit = 63;
        public string GetSqlName(string entityName)
        {
            if (entityName.Length > NameLengthLimit)
            {
                return GetSqlName(entityName.Substring(0, NameLengthLimit));
            }

            if (entityName.ToLowerInvariant() == entityName)
            {
                return entityName;
            }
            else
            {
                return $"\"{entityName}\"";
            }
        }

        public bool CustomTypeExists(string customTypeName)
        {
            using (var cn = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand(IndividualCustomTypeQuery, cn))
            {
                cmd.Parameters.AddWithValue("typeName", NpgsqlDbType.Text, customTypeName);
                
                cn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Log.Debug("Custom Type {CustomTypeName} already exists.", customTypeName);
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsDateOnly(string typeName)
        {
            return PostgresType.IsDateOnly(typeName);
        }

        public bool IsTimeOnly(string typeName)
        {
            return PostgresType.IsTimeOnly(typeName);
        }

        public void AddTestData(List<CodeFile> scripts)
        {
            var failedScripts = new List<CodeFile>();

            foreach (var script in scripts)
            {
                try
                {
                    ExecuteCommandText(script.Contents, false);
                }
                catch (PostgresException)
                {
                    failedScripts.Add(script);
                }
            }

            if (failedScripts.Any())
            {
                Log.Information("Attempting to re-run {Count} failed data scripts", failedScripts.Count);
                foreach (var script in failedScripts)
                {
                    try
                    {
                        ExecuteCommandText(script.Contents);
                    }
                    catch (PostgresException pgEx)
                    {
                        Log.Warning("Test data script {ScriptName} failed with error {Error}", script.Name, pgEx);
                    }
                }
            }
        }

        public string DefaultNamespace => "public";
        public string GetTemplate(string templateName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            return Util.GetTemplate(templateName, assembly);
        }

        public bool GenerateCustomTypes => true;
        public string FormatOperationParameterName(string operationName, string name)
        {
            return EscapeSqlName(operationName) + "." + EscapeSqlName(name);
        }

        public string OperationTimestampFunction()
        {
            return "clock_timestamp()";
        }

        public IParamterPrototype CreateFieldAdapter(Field field, IOperationPrototype operationPrototype)
        {
            return new PostgresFieldAdapter(field, operationPrototype, this);
        }

        public bool IncludeIdentityFieldsInInsertStatements => true;
        public string GetProviderTypeForClrType(Type clrType)
        {
            return PostgresType.ProviderTypeNameFromClrType(clrType);
        }

        public ISortField CreateSortField(IPseudoField field, IOperationPrototype prototype)
        {
            return new PostgresSortField(field, prototype, this);
        }

        public IPseudoField CreateSortParameter(INamingConvention namingConvention)
        {
            return new PostgresSortParameter(namingConvention);
        }

        public static NpgsqlDbType GetNpgsqlDbTypeFromPostgresType(string postgresTypeName)
        {
            if (_postgresNpgSqlTypes.ContainsKey(postgresTypeName))
            {
                return _postgresNpgSqlTypes[postgresTypeName];
            }

            return NpgsqlDbType.Unknown;
        }
        
        public static string[] SqlReservedWords { get; } = new[]
        {
            "A",
            "ABORT",
            "ABS",
            "ABSOLUTE",
            "ACCESS",
            "ACTION",
            "ADA",
            "ADD",
            "ADMIN",
            "AFTER",
            "AGGREGATE",
            "ALIAS",
            "ALL",
            "ALLOCATE",
            "ALSO",
            "ALTER",
            "ALWAYS",
            "ANALYSE",
            "ANALYZE",
            "AND",
            "ANY",
            "ARE",
            "ARRAY",
            "AS",
            "ASC",
            "ASENSITIVE",
            "ASSERTION",
            "ASSIGNMENT",
            "ASYMMETRIC",
            "AT",
            "ATOMIC",
            "ATTRIBUTE",
            "ATTRIBUTES",
            "AUTHORIZATION",
            "AVG",
            "BACKWARD",
            "BEFORE",
            "BEGIN",
            "BERNOULLI",
            "BETWEEN",
            "BIGINT",
            "BINARY",
            "BIT",
            "BITVAR",
            "BIT_LENGTH",
            "BLOB",
            "BOOLEAN",
            "BOTH",
            "BREADTH",
            "BY",
            "C",
            "CACHE",
            "CALL",
            "CALLED",
            "CARDINALITY",
            "CASCADE",
            "CASCADED",
            "CASE",
            "CAST",
            "CATALOG",
            "CATALOG_NAME",
            "CEIL",
            "CEILING",
            "CHAIN",
            "CHAR",
            "CHARACTER",
            "CHARACTERISTICS",
            "CHARACTERS",
            "CHARACTER_LENGTH",
            "CHARACTER_SET_CATALOG",
            "CHARACTER_SET_NAME",
            "CHARACTER_SET_SCHEMA",
            "CHAR_LENGTH",
            "CHECK",
            "CHECKED",
            "CHECKPOINT",
            "CLASS",
            "CLASS_ORIGIN",
            "CLOB",
            "CLOSE",
            "CLUSTER",
            "COALESCE",
            "COBOL",
            "COLLATE",
            "COLLATION",
            "COLLATION_CATALOG",
            "COLLATION_NAME",
            "COLLATION_SCHEMA",
            "COLLECT",
            "COLUMN",
            "COLUMN_NAME",
            "COMMAND_FUNCTION",
            "COMMAND_FUNCTION_CODE",
            "COMMENT",
            "COMMIT",
            "COMMITTED",
            "COMPLETION",
            "CONDITION",
            "CONDITION_NUMBER",
            "CONNECT",
            "CONNECTION",
            "CONNECTION_NAME",
            "CONSTRAINT",
            "CONSTRAINTS",
            "CONSTRAINT_CATALOG",
            "CONSTRAINT_NAME",
            "CONSTRAINT_SCHEMA",
            "CONSTRUCTOR",
            "CONTAINS",
            "CONTINUE",
            "CONVERSION",
            "CONVERT",
            "COPY",
            "CORR",
            "CORRESPONDING",
            "COUNT",
            "COVAR_POP",
            "COVAR_SAMP",
            "CREATE",
            "CREATEDB",
            "CREATEROLE",
            "CREATEUSER",
            "CROSS",
            "CSV",
            "CUBE",
            "CUME_DIST",
            "CURRENT",
            "CURRENT_DATE",
            "CURRENT_DEFAULT_TRANSFORM_GROUP",
            "CURRENT_PATH",
            "CURRENT_ROLE",
            "CURRENT_TIME",
            "CURRENT_TIMESTAMP",
            "CURRENT_TRANSFORM_GROUP_FOR_TYPE",
            "CURRENT_USER",
            "CURSOR",
            "CURSOR_NAME",
            "CYCLE",
            "DATA",
            "DATABASE",
            "DATE",
            "DATETIME_INTERVAL_CODE",
            "DATETIME_INTERVAL_PRECISION",
            "DAY",
            "DEALLOCATE",
            "DEC",
            "DECIMAL",
            "DECLARE",
            "DEFAULT",
            "DEFAULTS",
            "DEFERRABLE",
            "DEFERRED",
            "DEFINED",
            "DEFINER",
            "DEGREE",
            "DELETE",
            "DELIMITER",
            "DELIMITERS",
            "DENSE_RANK",
            "DEPTH",
            "DEREF",
            "DERIVED",
            "DESC",
            "DESCRIBE",
            "DESCRIPTOR",
            "DESTROY",
            "DESTRUCTOR",
            "DETERMINISTIC",
            "DIAGNOSTICS",
            "DICTIONARY",
            "DISABLE",
            "DISCONNECT",
            "DISPATCH",
            "DISTINCT",
            "DO",
            "DOMAIN",
            "DOUBLE",
            "DROP",
            "DYNAMIC",
            "DYNAMIC_FUNCTION",
            "DYNAMIC_FUNCTION_CODE",
            "EACH",
            "ELEMENT",
            "ELSE",
            "ENABLE",
            "ENCODING",
            "ENCRYPTED",
            "END",
            "END-EXEC",
            "EQUALS",
            "ESCAPE",
            "EVERY",
            "EXCEPT",
            "EXCEPTION",
            "EXCLUDE",
            "EXCLUDING",
            "EXCLUSIVE",
            "EXEC",
            "EXECUTE",
            "EXISTING",
            "EXISTS",
            "EXP",
            "EXPLAIN",
            "EXTERNAL",
            "EXTRACT",
            "FALSE",
            "FETCH",
            "FILTER",
            "FINAL",
            "FIRST",
            "FLOAT",
            "FLOOR",
            "FOLLOWING",
            "FOR",
            "FORCE",
            "FOREIGN",
            "FORTRAN",
            "FORWARD",
            "FOUND",
            "FREE",
            "FREEZE",
            "FROM",
            "FULL",
            "FUNCTION",
            "FUSION",
            "G",
            "GENERAL",
            "GENERATED",
            "GET",
            "GLOBAL",
            "GO",
            "GOTO",
            "GRANT",
            "GRANTED",
            "GREATEST",
            "GROUP",
            "GROUPING",
            "HANDLER",
            "HAVING",
            "HEADER",
            "HIERARCHY",
            "HOLD",
            "HOST",
            "HOUR",
            "IDENTITY",
            "IGNORE",
            "ILIKE",
            "IMMEDIATE",
            "IMMUTABLE",
            "IMPLEMENTATION",
            "IMPLICIT",
            "IN",
            "INCLUDING",
            "INCREMENT",
            "INDEX",
            "INDICATOR",
            "INFIX",
            "INHERIT",
            "INHERITS",
            "INITIALIZE",
            "INITIALLY",
            "INNER",
            "INOUT",
            "INPUT",
            "INSENSITIVE",
            "INSERT",
            "INSTANCE",
            "INSTANTIABLE",
            "INSTEAD",
            "INT",
            "INTEGER",
            "INTERSECT",
            "INTERSECTION",
            "INTERVAL",
            "INTO",
            "INVOKER",
            "IS",
            "ISNULL",
            "ISOLATION",
            "ITERATE",
            "JOIN",
            "K",
            "KEY",
            "KEY_MEMBER",
            "KEY_TYPE",
            "LANCOMPILER",
            "LANGUAGE",
            "LARGE",
            "LAST",
            "LATERAL",
            "LEADING",
            "LEAST",
            "LEFT",
            "LENGTH",
            "LESS",
            "LEVEL",
            "LIKE",
            "LIMIT",
            "LISTEN",
            "LN",
            "LOAD",
            "LOCAL",
            "LOCALTIME",
            "LOCALTIMESTAMP",
            "LOCATION",
            "LOCATOR",
            "LOCK",
            "LOGIN",
            "LOWER",
            "M",
            "MAP",
            "MATCH",
            "MATCHED",
            "MAX",
            "MAXVALUE",
            "MEMBER",
            "MERGE",
            "MESSAGE_LENGTH",
            "MESSAGE_OCTET_LENGTH",
            "MESSAGE_TEXT",
            "METHOD",
            "MIN",
            "MINUTE",
            "MINVALUE",
            "MOD",
            "MODE",
            "MODIFIES",
            "MODIFY",
            "MODULE",
            "MONTH",
            "MORE",
            "MOVE",
            "MULTISET",
            "MUMPS",
            "NAME",
            "NAMES",
            "NATIONAL",
            "NATURAL",
            "NCHAR",
            "NCLOB",
            "NESTING",
            "NEW",
            "NEXT",
            "NO",
            "NOCREATEDB",
            "NOCREATEROLE",
            "NOCREATEUSER",
            "NOINHERIT",
            "NOLOGIN",
            "NONE",
            "NORMALIZE",
            "NORMALIZED",
            "NOSUPERUSER",
            "NOT",
            "NOTHING",
            "NOTIFY",
            "NOTNULL",
            "NOWAIT",
            "NULL",
            "NULLABLE",
            "NULLIF",
            "NULLS",
            "NUMBER",
            "NUMERIC",
            "OBJECT",
            "OCTETS",
            "OCTET_LENGTH",
            "OF",
            "OFF",
            "OFFSET",
            "OIDS",
            "OLD",
            "ON",
            "ONLY",
            "OPEN",
            "OPERATION",
            "OPERATOR",
            "OPTION",
            "OPTIONS",
            "OR",
            "ORDER",
            "ORDERING",
            "ORDINALITY",
            "OTHERS",
            "OUT",
            "OUTER",
            "OUTPUT",
            "OVER",
            "OVERLAPS",
            "OVERLAY",
            "OVERRIDING",
            "OWNER",
            "PAD",
            "PARAMETER",
            "PARAMETERS",
            "PARAMETER_MODE",
            "PARAMETER_NAME",
            "PARAMETER_ORDINAL_POSITION",
            "PARAMETER_SPECIFIC_CATALOG",
            "PARAMETER_SPECIFIC_NAME",
            "PARAMETER_SPECIFIC_SCHEMA",
            "PARTIAL",
            "PARTITION",
            "PASCAL",
            "PASSWORD",
            "PATH",
            "PERCENTILE_CONT",
            "PERCENTILE_DISC",
            "PERCENT_RANK",
            "PLACING",
            "PLI",
            "POSITION",
            "POSTFIX",
            "POWER",
            "PRECEDING",
            "PRECISION",
            "PREFIX",
            "PREORDER",
            "PREPARE",
            "PREPARED",
            "PRESERVE",
            "PRIMARY",
            "PRIOR",
            "PRIVILEGES",
            "PROCEDURAL",
            "PROCEDURE",
            "PUBLIC",
            "QUOTE",
            "RANGE",
            "RANK",
            "READ",
            "READS",
            "REAL",
            "RECHECK",
            "RECURSIVE",
            "REF",
            "REFERENCES",
            "REFERENCING",
            "REGR_AVGX",
            "REGR_AVGY",
            "REGR_COUNT",
            "REGR_INTERCEPT",
            "REGR_R2",
            "REGR_SLOPE",
            "REGR_SXX",
            "REGR_SXY",
            "REGR_SYY",
            "REINDEX",
            "RELATIVE",
            "RELEASE",
            "RENAME",
            "REPEATABLE",
            "REPLACE",
            "RESET",
            "RESTART",
            "RESTRICT",
            "RESULT",
            "RETURN",
            "RETURNED_CARDINALITY",
            "RETURNED_LENGTH",
            "RETURNED_OCTET_LENGTH",
            "RETURNED_SQLSTATE",
            "RETURNS",
            "REVOKE",
            "RIGHT",
            "ROLE",
            "ROLLBACK",
            "ROLLUP",
            "ROUTINE",
            "ROUTINE_CATALOG",
            "ROUTINE_NAME",
            "ROUTINE_SCHEMA",
            "ROW",
            "ROWS",
            "ROW_COUNT",
            "ROW_NUMBER",
            "RULE",
            "SAVEPOINT",
            "SCALE",
            "SCHEMA",
            "SCHEMA_NAME",
            "SCOPE",
            "SCOPE_CATALOG",
            "SCOPE_NAME",
            "SCOPE_SCHEMA",
            "SCROLL",
            "SEARCH",
            "SECOND",
            "SECTION",
            "SECURITY",
            "SELECT",
            "SELF",
            "SENSITIVE",
            "SEQUENCE",
            "SERIALIZABLE",
            "SERVER_NAME",
            "SESSION",
            "SESSION_USER",
            "SET",
            "SETOF",
            "SETS",
            "SHARE",
            "SHOW",
            "SIMILAR",
            "SIMPLE",
            "SIZE",
            "SMALLINT",
            "SOME",
            "SOURCE",
            "SPACE",
            "SPECIFIC",
            "SPECIFICTYPE",
            "SPECIFIC_NAME",
            "SQL",
            "SQLCODE",
            "SQLERROR",
            "SQLEXCEPTION",
            "SQLSTATE",
            "SQLWARNING",
            "SQRT",
            "STABLE",
            "START",
            "STATE",
            "STATEMENT",
            "STATIC",
            "STATISTICS",
            "STDDEV_POP",
            "STDDEV_SAMP",
            "STDIN",
            "STDOUT",
            "STORAGE",
            "STRICT",
            "STRUCTURE",
            "STYLE",
            "SUBCLASS_ORIGIN",
            "SUBLIST",
            "SUBMULTISET",
            "SUBSTRING",
            "SUM",
            "SUPERUSER",
            "SYMMETRIC",
            "SYSID",
            "SYSTEM",
            "SYSTEM_USER",
            "TABLE",
            "TABLESAMPLE",
            "TABLESPACE",
            "TABLE_NAME",
            "TEMP",
            "TEMPLATE",
            "TEMPORARY",
            "TERMINATE",
            "THAN",
            "THEN",
            "TIES",
            "TIME",
            "TIMESTAMP",
            "TIMEZONE_HOUR",
            "TIMEZONE_MINUTE",
            "TO",
            "TOAST",
            "TOP_LEVEL_COUNT",
            "TRAILING",
            "TRANSACTION",
            "TRANSACTIONS_COMMITTED",
            "TRANSACTIONS_ROLLED_BACK",
            "TRANSACTION_ACTIVE",
            "TRANSFORM",
            "TRANSFORMS",
            "TRANSLATE",
            "TRANSLATION",
            "TREAT",
            "TRIGGER",
            "TRIGGER_CATALOG",
            "TRIGGER_NAME",
            "TRIGGER_SCHEMA",
            "TRIM",
            "TRUE",
            "TRUNCATE",
            "TRUSTED",
            "TYPE",
            "UESCAPE",
            "UNBOUNDED",
            "UNCOMMITTED",
            "UNDER",
            "UNENCRYPTED",
            "UNION",
            "UNIQUE",
            "UNKNOWN",
            "UNLISTEN",
            "UNNAMED",
            "UNNEST",
            "UNTIL",
            "UPDATE",
            "UPPER",
            "USAGE",
            "USER",
            "USER_DEFINED_TYPE_CATALOG",
            "USER_DEFINED_TYPE_CODE",
            "USER_DEFINED_TYPE_NAME",
            "USER_DEFINED_TYPE_SCHEMA",
            "USING",
            "VACUUM",
            "VALID",
            "VALIDATOR",
            "VALUE",
            "VALUES",
            "VARCHAR",
            "VARIABLE",
            "VARYING",
            "VAR_POP",
            "VAR_SAMP",
            "VERBOSE",
            "VIEW",
            "VOLATILE",
            "WHEN",
            "WHENEVER",
            "WHERE",
            "WIDTH_BUCKET",
            "WINDOW",
            "WITH",
            "WITHIN",
            "WITHOUT",
            "WORK",
            "WRITE",
            "YEAR",
            "ZONE"
        };
        
        private void DropGeneratedOperation(Operation op)
        {
            Log.Debug("Dropping {OperationName}", op.Name);
            var cmdText = GetDropOperationCommandText(op);
            ExecuteCommandText(cmdText);
        }

        private string GetDropOperationCommandText(Operation op)
        {
            return $"DROP FUNCTION IF EXISTS {op.Namespace}.{GetSqlName(op.Name)};";
        }
        
        private void ExecuteCommandText(string text, bool log = true)
        {
            try
            {
                using (var cn = new NpgsqlConnection(_connectionString))
                using (var cmd = new NpgsqlCommand(text, cn))
                {
                    cmd.CommandType = CommandType.Text;
                    cn.Open();
                    cmd.ExecuteNonQuery();
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

        private void PopulateOperationAttributes(Operation op, string description)
        {
            if (!string.IsNullOrEmpty(description))
            {
                op.Attributes = ReadAttributes(description, op.Name);
            }
        }

        private dynamic GetDbCustomTypeAttributes(string name, string ns)
        {
            using (var cn = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand($"SELECT description FROM pg_catalog.pg_description WHERE objoid = '\"{ns}\".\"{name}\"'::regtype;", cn))
            {
                cn.Open();
                var result = cmd.ExecuteScalar();
                if (result != DBNull.Value && result != null)
                {
                    var attributes = result.ToString();
                    if (!string.IsNullOrEmpty(attributes))
                    {
                        return ReadAttributes(attributes, name);
                    }
                }
            }

            return null;
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

        private void GetTypeAttributes(List<ApplicationType> types)
        {
            using (var cn = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand(TypeAttributesQuery, cn))
            {
                cn.Open();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var schema = reader.GetString(0);
                    var table = SanitizeObjectName(reader.GetString(1));
                    var col = SanitizeFieldName(GetField<string>(reader, 2));
                    var attributes = reader.GetString(3);

                    var type = types.FirstOrDefault(t => t.Name == table && t.Namespace == schema);
                    if (type != null)
                    {
                        if (col == null)
                        {
                            type.Attributes = ReadAttributes(attributes, type.Name);
                        }
                        else
                        {
                            var field = type.Fields.FirstOrDefault(f => f.Name == col);
                            if (field != null)
                            {
                                field.Attributes = ReadAttributes(attributes, type.Name + "." + col);
                            }
                        }
                    }
                }
            }
        }

        private dynamic ReadAttributes(string attributes, string objectName)
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
                    Log.Warning("attribute string {Attributes} was not valid JSON for {ObjectName}", attributes, objectName);
                }
                return null; // description was not valid JSON
            }
        }

        private OperationReturn GetReturnForOperation(string resultType, Domain domain, Operation operation)
        {
            var pgType = new PostgresType(resultType);
            
            if (pgType.IsVoid)
            {
                return new OperationReturn {ReturnType = ReturnType.None};
            }

            if (resultType.StartsWith("TABLE"))
            {
                if (ParseTableResultType(resultType, domain, operation, out var operationReturn)) return operationReturn;
            }

            if (resultType.StartsWith("SETOF"))
            {
                var typeName = resultType.Replace("SETOF", "").Trim().Trim('"');
                return GetReturnTypeFromTypeName(domain, operation, typeName, true);
            }

            if (pgType.ClrType == null)
            {
                return GetReturnTypeFromTypeName(domain, operation, resultType.Trim('"').Trim(), false);
            }
            else
            {
                // this is a simple type like an int or string
                return new OperationReturn
                {
                    ReturnType = ReturnType.Primitive,
                    ClrReturnType = pgType.ClrType
                };
            }
        }

        private OperationReturn GetReturnTypeFromTypeName(Domain domain, Operation operation, string typeName, bool multiple)
        {
            var appType = domain.Types.SingleOrDefault(t => t.Name == typeName && t.Namespace == operation.Namespace);
            if (appType != null)
            {
                return new OperationReturn { ReturnType = ReturnType.ApplicationType, SimpleReturnType = appType, Multiple = multiple};
            }
            else
            {
                // see if this is a <x>_result - a restricted set of fields to ensure fields like deleted_date and search_text don't get returned
                // these are generated by ResultType.handlebars
                var attribs = GetDbCustomTypeAttributes(typeName, operation.Namespace);
                if (attribs?.generated == true && attribs?.isResult == true)
                {
                    var appTypeName = attribs?.applicationtype.ToString();
                    appType = domain.Types.SingleOrDefault(t => t.Name == appTypeName);
                    if (appType != null)
                    {
                        return new OperationReturn { ReturnType = ReturnType.ApplicationType, SimpleReturnType = appType, Multiple = multiple};
                    }
                    else
                    {
                        // error
                        throw new InvalidOperationException(
                            $"Unable to find application type {appTypeName} returned by operation {operation.Name} from generated result type.");
                    }
                }
                else
                {
                    // look in result types
                    var returnType = domain.ResultTypes.SingleOrDefault(t => t.Name == typeName);
                    if (returnType != null)
                    {
                        returnType.Operations.Add(operation);
                        return new OperationReturn { ReturnType = ReturnType.CustomType, SimpleReturnType = returnType, Multiple = multiple};
                    }
                    else
                    {
                        // get return type info from postgres meta-data
                        var customReturnType = ReadCustomOperationReturn(typeName, domain, operation);
                        if (customReturnType == null)
                        {
                            // error
                            throw new InvalidOperationException(
                                $"Unable to find user-defined type information for {typeName} returned by operation {operation.Name}");
                        }

                        customReturnType.Multiple = multiple;
                        return customReturnType;
                    }
                }
            }
        }

        private OperationReturn ReadCustomOperationReturn(string typeName, Domain domain, Operation operation)
        {
            var result = ReadCustomOperationType(typeName, domain, operation);
            return new OperationReturn {ReturnType = ReturnType.CustomType, SimpleReturnType = result};
        }

        private ResultType ReadCustomOperationType(string typeName, Domain domain, Operation operation)
        {
            using var cn = new NpgsqlConnection(_connectionString);
            using var cmd = new NpgsqlCommand(TypeQuery, cn);
            // TODO - this code assumes the custom type is in the same namespace as the function that returns it, which may not be a valid assumption
            cmd.Parameters.AddWithValue("schemaName", NpgsqlDbType.Text, EscapeSqlName(operation.Namespace));
            cmd.Parameters.AddWithValue("typeName", NpgsqlDbType.Text, EscapeSqlName(typeName));

            cn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                // possibly inaccurate since it just picks the related type of the operation
                var result = new ResultType(typeName, operation.Namespace, operation.RelatedType, true, domain);
                while (reader.Read())
                {
                    var fld = new Field(result);
                    fld.Name = SanitizeFieldName(GetField<string>(reader, "column_name"));
                    var providerTypeRaw = GetField<string>(reader, "data_type");
                    if (providerTypeRaw.EndsWith(')'))
                    {
                        var typeAndSize = ParseTypeAndSize(providerTypeRaw);
                        fld.ProviderTypeName = typeAndSize.Item1;
                        fld.Size = typeAndSize.Item2;
                    }
                    else
                    {
                        fld.ProviderTypeName = providerTypeRaw;
                    }

                    fld.ClrType = new PostgresType(fld.ProviderTypeName).ClrType;
                        
                    fld.Order = GetField<short>(reader, "ordinal_position");
                    result.Fields.Add(fld);
                }

                if (operation.Attributes?.applicationtype != null)
                {
                    domain.UpdateResultFieldPropertiesFromApplicationType(operation, result);
                }

                result.Operations.Add(operation);
                domain.ResultTypes.Add(result);

                return result;
            }
        }
        
        private static Tuple<string, int> ParseTypeAndSize(string providerTypeRaw)
        {
            var regex = new Regex("(\\D+)\\((\\d+)\\)");
            var match = regex.Match(providerTypeRaw);
            var type = match.Groups[1].Value;
            var size = int.Parse(match.Groups[2].Value);
            return new Tuple<string, int>(type, size);
        }

        private bool ParseTableResultType(string resultType, Domain domain, Operation operation,
            out OperationReturn operationReturn)
        {
            var regex = new Regex("TABLE\\((.*)\\)");
            var match = regex.Match(resultType);
            var columns = match.Groups[1].Value;

            if (columns.IndexOf(',') < 0)
            {
                var nameAndType = GetFieldNameAndType(columns);
                // TODO - create a simple return type that is an array of whatever this type is
            }
            else
            {
                var fields = new List<Field>();

                var split = columns.Split(',');
                var index = 0;
                foreach (var s in split)
                {
                    var n = GetFieldNameAndType(s);
                    fields.Add(new Field(domain)
                    {
                        Name = n.Name, ProviderTypeName = n.Type.Name, ClrType = n.Type.ClrType,
                        Order = index
                    });
                    index++;
                }

                var existingType = FindTypeByFields(domain, fields, operation);
                if (existingType != null)
                {
                    operationReturn = new OperationReturn()
                    {
                        ReturnType = ReturnType.ApplicationType,
                        SimpleReturnType = existingType
                    };
                    return true;
                }
                else
                {
                    // instead of using an attribute here we could do some fancy inferencing too
                    var name = domain.NamingConvention.CreateResultTypeNameForOperation(operation.Name);
                    if (operation.CustomReturnTypeName != null)
                    {
                        name = operation.CustomReturnTypeName;
                        // TODO - we could check that the fields match here too?
                    }

                    var existingReturnType = domain.ResultTypes.SingleOrDefault(t => t.Name == name);
                    if (existingReturnType == null)
                    {
                        // possibly inaccurate since it just picks the related type of the operation it is returned by
                        var result = new ResultType(name, operation.Namespace, operation.RelatedType, false, domain);
                        result.Operations.Add(operation);

                        var newFields = fields.Select(f => new Field(result)
                        {
                            // re-create the fields now that we know who the parent type will be
                            Attributes = f.Attributes,
                            Name = f.Name,
                            ProviderTypeName = f.ProviderTypeName,
                            ClrType = f.ClrType,
                            Order = f.Order
                        });
                        result.Fields.AddRange(newFields);

                        if (operation.Attributes?.applicationtype != null)
                        {
                            domain.UpdateResultFieldPropertiesFromApplicationType(operation, result);
                        }

                        domain.ResultTypes.Add(result);

                        {
                            operationReturn = new OperationReturn()
                            {
                                ReturnType = ReturnType.CustomType,
                                SimpleReturnType = result,
                                Multiple = true
                            };
                            return true;
                        }
                    }
                    else
                    {
                        existingReturnType.Operations.Add(operation);

                        {
                            operationReturn = new OperationReturn()
                            {
                                ReturnType = ReturnType.CustomType,
                                SimpleReturnType = existingReturnType,
                                Multiple = true
                            };
                            return true;
                        }
                    }
                }
            }

            operationReturn = new OperationReturn{ }; // this should be ignored because the result is false
            return false;
        }

        private SimpleType FindTypeByFields(Domain domain, List<Field> fields, Operation operation)
        {
            return domain.FindTypeByFields(fields, operation, false);
        }
        
        private IEnumerable<Parameter> ReadParameters(string parameters, Domain domain, Operation operation)
        {
            var p = new List<Parameter>();

            if (parameters.IndexOf(',') < 0)
            {
                var prm = ReadSingleParameter(parameters, domain, operation);
                prm.Order = 0;
                p.Add(prm);
            }
            else
            {
                var split = parameters.Split(',');
                var index = 0;
                foreach (var s in split)
                {
                    var prm = ReadSingleParameter(s.Trim(), domain, operation);
                    prm.Order = index;
                    p.Add(prm);
                    index++;
                }
            }

            return p;
        }

        private Parameter ReadSingleParameter(string p, Domain domain, Operation operation)
        {
            var n = GetFieldNameAndType(p);
            var type = n.Type.ClrType;
            if (type == null)
            {
                var resultType = domain.ResultTypes.SingleOrDefault(rt => rt.Name == n.Type.Name && rt.Namespace == operation.Namespace);
                if (resultType == null)
                {
                    resultType = ReadCustomOperationType(n.Type.Name, domain, operation);
                }
                
                if (resultType != null)
                {
                    if (n.Type.IsArray)
                    {
                        type = typeof(List<ResultType>);
                    }
                    else
                    {
                        type = typeof(ResultType);
                    }
                }
                else
                {
                    Log.Warning("Unable to determine CLR type for {TypeName}", n.Type);
                }  
            }
            
            var parameter = new Parameter(domain, operation, n.Name, type, n.Type.Name);
            return parameter;
        }

        private NameAndType GetFieldNameAndType(string value)
        {
            // TODO - might need to consider quoted parameters (e.g. "parameter name") in the future
            value = value.Trim();
            var space = value.IndexOf(' ');
            var name = SanitizeFieldName(value.Substring(0, space));
            var pgType = new PostgresType(SanitizeObjectName(value.Substring(space + 1)));
            return new NameAndType {Name = name, Type = pgType };
        }

        private static void GetAdditionalFieldInfoFromInformationSchema(string catalog, string ns, string name,
            NpgsqlConnection cn, ApplicationType type)
        {
            using (var cmd = new NpgsqlCommand(
                $"select * from information_schema.columns where table_catalog = '{catalog}' and table_schema = '{ns}' and table_name = '{name}'",
                cn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var fieldName = reader["column_name"].ToString();
                        var isNullable = reader["is_nullable"].ToString() == "YES";
                        var isGenerated = reader["is_generated"].ToString() == "ALWAYS";
                        var colDefault = reader["column_default"] == DBNull.Value ? null : reader["column_default"].ToString();

                        var field = type.Fields.FirstOrDefault(f => f.Name == fieldName);
                        if (field == null)
                        {
                            throw new DataException("Can't find field " + fieldName);
                        }
                        else
                        {
                            field.IsRequired = !isNullable;
                            
                            // nextval is the syntax for use of sequences
                            if (isGenerated || colDefault?.StartsWith("nextval(") == true)
                            {
                                field.IsGenerated = true;
                            }

                            if (isNullable && !ClrTypeIsNullable(field.ClrType))
                            {
                                // we need to change the CLR type to make it nullable
                                field.ClrType = MakeClrTypeNullable(field.ClrType);
                            }
                        }
                    }
                }
            }
        }

        private static bool ClrTypeIsNullable(Type type)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }

        public static Type MakeClrTypeNullable(Type type)
        {
            return typeof(Nullable<>).MakeGenericType(type);
        }

        private static void GetPrimaryKeyInfoFromInformationSchema(string catalog, string ns, string name,
            NpgsqlConnection cn, ApplicationType type)
        {
            using (var cmd = new NpgsqlCommand(
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

        private void GetUniqueConstraintsFromInformationSchema(string catalog, string ns, string name, NpgsqlConnection cn, ApplicationType applicationType)
        {
            using (var cmd = new NpgsqlCommand(
                $@"
SELECT 
    c.conname, 
    pg_get_constraintdef(c.oid)
FROM   pg_constraint c
WHERE 
contype = 'u'
AND c.conrelid = '{ns}.""{name}""'::regclass;
",
                cn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var constraintName = reader["conname"].ToString();
                        var definition = reader["pg_get_constraintdef"].ToString();
                        var start = definition.IndexOf('(') + 1;
                        var end = definition.IndexOf(')');
                        if (start > 0 && end > start)
                        {
                            var fieldNames = definition.Substring(start, end - start).Split(',');
                            var constraint = new Constraint(constraintName);
                            constraint.Fields.AddRange(applicationType.Fields.Where(a => fieldNames.Contains(a.Name)));
                            applicationType.Constraints.Add(constraint);
                        }
                    }
                }
            }
        }

        private static void GetForeignKeyInfoFromInformationSchema(NpgsqlConnection cn, ApplicationType type, List<ApplicationType> allTypes)
        {
            using (var cmd = new NpgsqlCommand(
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
                        var field = type.Fields.FirstOrDefault(a => a.Name == fieldName);
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
                        var refField = refType.Fields.FirstOrDefault(a => a.Name == refFieldName);

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

        private void UpdateParameterNullabilityFromApplicationType(Operation op)
        {
            if (op.RelatedType == null)
            {
                return;
            }
            
            foreach (var prm in op.Parameters)
            {
                var fld = op.RelatedType.Fields.FirstOrDefault(f => f.Name == prm.Name);
                if (fld != null)
                {
                    prm.UpdateFromField(fld);
                }
                else
                {
                    if (prm.Name == _namingConvention.SecurityUserIdParameterName && !prm.IsNullable)
                    {
                        prm.MakeClrTypeNullable();
                    }
                    else
                    {
                        // since this matching above is done by name, it misses some things e.g. where the parameter is called id_param and the field is called id.
                        // here we 'fall back' to try to fix that
                        var paramFld = op.RelatedType.Fields.FirstOrDefault(f => _namingConvention.CreateParameterNameFromFieldName(f.Name) == prm.Name);
                        if (paramFld != null)
                        {
                            prm.UpdateFromField(paramFld);
                        }
                    }
                }
            }
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
        
        private const string ProceduresQuery =
        @"SELECT 
	n.nspname as schema,
    p.proname as name,
    pg_catalog.pg_get_function_result(p.oid) as result_type,
    pg_catalog.pg_get_function_arguments(p.oid) as argument_types,
    p.prokind,
    dsc.description
    FROM pg_catalog.pg_proc p
    LEFT JOIN pg_catalog.pg_namespace n ON n.oid = p.pronamespace
    left join pg_catalog.pg_description dsc on p.oid = dsc.objoid
    WHERE pg_catalog.pg_function_is_visible(p.oid)
    AND n.nspname<> 'pg_catalog'
    AND n.nspname<> 'information_schema'
    ORDER BY 1, 2, 4;";

        private const string TypeAttributesQuery =
        @"SELECT 
        st.schemaname,
        st.relname as table_name,
        c.column_name,
        pgd.description
        FROM pg_catalog.pg_statio_all_tables as st
        INNER JOIN pg_catalog.pg_description pgd on (pgd.objoid=st.relid)
        LEFT JOIN information_schema.columns c on (pgd.objsubid=c.ordinal_position
        AND  c.table_schema=st.schemaname and c.table_name=st.relname);";

        private const string ListTypesQuery = @"    
        SELECT n.nspname,
            pg_catalog.format_type ( t.oid, NULL ) AS obj_name,
            CASE
                WHEN t.typrelid != 0 THEN CAST ( 'tuple' AS pg_catalog.text )
                WHEN t.typlen < 0 THEN CAST ( 'var' AS pg_catalog.text )
                ELSE CAST ( t.typlen AS pg_catalog.text )
                END AS obj_type,
            coalesce ( pg_catalog.obj_description ( t.oid, 'pg_type' ), '' ) AS description
        FROM pg_catalog.pg_type t
        JOIN pg_catalog.pg_namespace n
            ON n.oid = t.typnamespace
        WHERE ( t.typrelid = 0
                OR ( SELECT c.relkind = 'c'
                        FROM pg_catalog.pg_class c
                        WHERE c.oid = t.typrelid ) )
            AND NOT EXISTS (
                    SELECT 1
                        FROM pg_catalog.pg_type el
                        WHERE el.oid = t.typelem
                        AND el.typarray = t.oid )
            AND n.nspname <> 'pg_catalog'
            AND n.nspname <> 'information_schema'
            AND n.nspname !~ '^pg_toast'
        order by obj_name";
        
        private const string IndividualCustomTypeQuery = @"    
        SELECT n.nspname,
            pg_catalog.format_type ( t.oid, NULL ) AS obj_name,
            CASE
                WHEN t.typrelid != 0 THEN CAST ( 'tuple' AS pg_catalog.text )
                WHEN t.typlen < 0 THEN CAST ( 'var' AS pg_catalog.text )
                ELSE CAST ( t.typlen AS pg_catalog.text )
                END AS obj_type,
            coalesce ( pg_catalog.obj_description ( t.oid, 'pg_type' ), '' ) AS description
        FROM pg_catalog.pg_type t
        JOIN pg_catalog.pg_namespace n
            ON n.oid = t.typnamespace
        WHERE 
           pg_catalog.format_type ( t.oid, NULL ) = @typeName 
           AND ( t.typrelid = 0
                OR ( SELECT c.relkind = 'c'
                        FROM pg_catalog.pg_class c
                        WHERE c.oid = t.typrelid ) )
            AND NOT EXISTS (
                    SELECT 1
                        FROM pg_catalog.pg_type el
                        WHERE el.oid = t.typelem
                        AND el.typarray = t.oid )
            AND n.nspname <> 'pg_catalog'
            AND n.nspname <> 'information_schema'
            AND n.nspname !~ '^pg_toast'";

        // this query comes from here: https://dba.stackexchange.com/a/35510
        private const string TypeQuery =
            @"WITH types AS (
    SELECT n.nspname,
            pg_catalog.format_type ( t.oid, NULL ) AS obj_name,
            CASE
                WHEN t.typrelid != 0 THEN CAST ( 'tuple' AS pg_catalog.text )
                WHEN t.typlen < 0 THEN CAST ( 'var' AS pg_catalog.text )
                ELSE CAST ( t.typlen AS pg_catalog.text )
                END AS obj_type,
            coalesce ( pg_catalog.obj_description ( t.oid, 'pg_type' ), '' ) AS description
        FROM pg_catalog.pg_type t
        JOIN pg_catalog.pg_namespace n
            ON n.oid = t.typnamespace
        WHERE ( t.typrelid = 0
                OR ( SELECT c.relkind = 'c'
                        FROM pg_catalog.pg_class c
                        WHERE c.oid = t.typrelid ) )
            AND NOT EXISTS (
                    SELECT 1
                        FROM pg_catalog.pg_type el
                        WHERE el.oid = t.typelem
                        AND el.typarray = t.oid )
            AND n.nspname <> 'pg_catalog'
            AND n.nspname <> 'information_schema'
            AND n.nspname !~ '^pg_toast'
),
cols AS (
    SELECT n.nspname::text AS schema_name,
            pg_catalog.format_type ( t.oid, NULL ) AS obj_name,
            a.attname::text AS column_name,
            pg_catalog.format_type ( a.atttypid, a.atttypmod ) AS data_type,
            a.attnotnull AS is_required,
            a.attnum AS ordinal_position,
            pg_catalog.col_description ( a.attrelid, a.attnum ) AS description
        FROM pg_catalog.pg_attribute a
        JOIN pg_catalog.pg_type t
            ON a.attrelid = t.typrelid
        JOIN pg_catalog.pg_namespace n
            ON ( n.oid = t.typnamespace )
        JOIN types
            ON ( types.nspname = n.nspname
                AND types.obj_name = pg_catalog.format_type ( t.oid, NULL ) )
        WHERE a.attnum > 0
            AND NOT a.attisdropped
)
    SELECT 
        cols.column_name,
        cols.data_type,
        cols.ordinal_position,
        cols.is_required,
        coalesce ( cols.description, '' ) AS description
    FROM cols
    WHERE cols.schema_name = @schemaName
        AND cols.obj_name = @typeName
    ORDER BY 
        cols.ordinal_position ;";
    }

    public class NameAndType
    {
        public string Name;
        public PostgresType Type;
    }

}
