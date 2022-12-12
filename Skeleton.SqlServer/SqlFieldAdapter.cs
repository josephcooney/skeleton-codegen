using Skeleton.Model;
using Skeleton.Model.Operations;

namespace Skeleton.SqlServer;

public class SqlFieldAdapter : IParamterPrototype
{
    private readonly Field _field;
    private readonly IOperationPrototype _prototype;
    private readonly SqlServerTypeProvider _typeProvider;

    public SqlFieldAdapter(Field field, IOperationPrototype prototype, SqlServerTypeProvider typeProvider)
        {
            _field = field;
            _prototype = prototype;
            _typeProvider = typeProvider;
        }

        public string Value
        {
            get
            {
                if (_prototype.OperationType == OperationType.Insert)
                {
                    if (_field.IsKey && _field.ClrType == typeof(int) && _field.IsGenerated)
                    {
                        return "DEFAULT";  // this shouldn't happen
                    }

                    if (_field.IsKey && _field.ClrType == typeof(Guid))
                    {
                        return "new_id"; 
                    }
                    if (_field.IsTrackingDate)
                    {
                        return _typeProvider.OperationTimestampFunction();
                    }
                    if (_field.IsSearch)
                    {
                        return GetSearchFieldsAsTsVector();
                    }

                    if (_field.IsTrackingUser)
                    {
                        return _typeProvider.FormatOperationParameterName(_prototype.FunctionName, _field.Name);    
                    }

                    if (!_prototype.UsesCustomInsertType)
                    {
                        return _typeProvider.FormatOperationParameterName(_prototype.FunctionName, _field.Name); 
                    }

                    if (_prototype.AddMany)
                    {
                        return _prototype.AddManyArrayItemVariableName + "." + _field.Name;
                    }
                    
                    return _typeProvider.EscapeReservedWord(_field.Name);
                }

                if (_prototype.OperationType == OperationType.Update)
                {
                    if (_field.IsKey)
                    {
                        return _typeProvider.FormatOperationParameterName(_prototype.FunctionName, Name);
                    }
                    if (_field.IsTrackingDate)
                    {
                        return _typeProvider.OperationTimestampFunction();
                    }
                    if (_field.IsSearch)
                    {
                        return GetSearchFieldsAsTsVector();
                    }
                    return _typeProvider.FormatOperationParameterName(_prototype.FunctionName, Name);
                }

                return "FIXME";
            }
        }

        public IOperationPrototype Parent => _prototype;

        private string GetSearchFieldsAsTsVector()
        {
            var textFields = _prototype.Fields.Where(f => f.ClrType == typeof(string) && !f.IsGenerated).Select(f => f.IsRequired ? f.Value : $"coalesce({f.Value}, '')");
            return "to_tsvector(" + string.Join(" || ' ' || ", textFields) + ")";
        }

        public string Name => _field.Name;
        public string ParentAlias => _prototype.ShortName;

        public string ProviderTypeName
        {
            get
            {
                var prefix = _typeProvider.DatabaseName + ".";
                if (_field.ProviderTypeName.StartsWith(prefix))
                {
                    return _field.ProviderTypeName.Substring(prefix.Length);
                }
                else
                {
                    return _field.ProviderTypeName;
                }
            }
        }
        public bool HasDisplayName => false;
        public string DisplayName => Name;

        public IPseudoField ReferencesTypeField
        {
            get
            {
                return new SqlFieldAdapter(_field.ReferencesTypeField, _prototype, _typeProvider); // not sure if parent is the right thing to pass here...because this references a different type 
            }
        }

        public int Order => _field.Order;

        public bool IsUuid => _field.ClrType == typeof(Guid);
        public bool Add => _field.Add;
        public bool Edit => _field.Edit;

        public bool IsUserEditable => _field.IsCallerProvided;
        public bool IsIdentity => _field.IsKey;
        public bool IsInt => _field.IsInt;

        public bool HasSize => _field.Size != null || ProviderTypeName.ToLowerInvariant() == "varchar" || ProviderTypeName.ToLowerInvariant() == "varbinary";

        public int? Size => _field.Size;

        public string? SizeDisplay
        {
            get
            {
                if (HasSize)
                {
                    if (_field.Size > 8000 || _field.Size == null)
                    {
                        return "max";
                    }

                    return _field.Size.ToString();
                }

                return null;
            }
        }

        public Type ClrType => _field.ClrType;

        public bool IsGenerated => _field.IsGenerated;
        public bool IsRequired => _field.IsRequired;
}