using System;
using System.Linq;
using Skeleton.Model;
using Skeleton.Model.Operations;

namespace Skeleton.Postgres;

public class PostgresFieldAdapter : IParamterPrototype
{
    private readonly Field _field;
    private readonly IOperationPrototype _prototype;
    private readonly ITypeProvider _typeProvider;

    public PostgresFieldAdapter(Field field, IOperationPrototype prototype, ITypeProvider typeProvider)
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
                    if (_field.IsKey && _field.ClrType == typeof(int))
                    {
                        return "DEFAULT"; 
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
                    
                    return _prototype.NewRecordParameterName + "." + _field.Name;
                }

                if (_prototype.OperationType == OperationType.Update)
                {
                    if (_field.IsKey)
                    {
                        return _typeProvider.FormatOperationParameterName(_prototype.FunctionName, _field.Name);
                    }
                    if (_field.IsTrackingDate)
                    {
                        return _typeProvider.OperationTimestampFunction();
                    }
                    if (_field.IsSearch)
                    {
                        return GetSearchFieldsAsTsVector();
                    }
                    return _typeProvider.FormatOperationParameterName(_prototype.FunctionName, _field.Name);
                }

                return "FIXME";
            }
        }

        private string GetSearchFieldsAsTsVector()
        {
            var textFields = _prototype.Fields.Where(f => f.ClrType == typeof(string) && !f.IsGenerated).Select(f => f.IsRequired ? f.Value : $"coalesce({f.Value}, '')");
            return "to_tsvector(" + string.Join(" || ' ' || ", textFields) + ")";
        }

        public string Name => _field.Name;
        public string ParentAlias => _prototype.ShortName;

        public string ProviderTypeName => _field.ProviderTypeName;
        public bool HasDisplayName => false;
        public string DisplayName => null;
        
        public IPseudoField ReferencesTypeField
        {
            get
            {
                return new PostgresFieldAdapter(_field.ReferencesTypeField, _prototype, _typeProvider); // not sure if parent is the right thing to pass here...because this references a different type 
            }
        }

        public int Order => _field.Order;

        public bool IsUuid => _field.ClrType == typeof(Guid);
        public bool Add => _field.Add;
        public bool Edit => _field.Edit;

        public bool IsUserEditable => _field.IsCallerProvided;
        public bool IsIdentity => _field.IsKey;
        public bool IsInt => _field.IsInt;

        public bool HasSize => _field.Size != null;

        public int? Size => _field.Size;

        public Type ClrType => _field.ClrType;

        public bool IsGenerated => _field.IsGenerated;
        public bool IsRequired => _field.IsRequired;
        
        public IOperationPrototype Parent => _prototype;
        
}