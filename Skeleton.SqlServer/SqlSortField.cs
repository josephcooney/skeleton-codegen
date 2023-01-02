using Skeleton.Model;
using Skeleton.Model.Operations;

namespace Skeleton.SqlServer;

public class SqlSortField : ISortField
{
    private readonly IPseudoField _field;
    private readonly IOperationPrototype _prototype;
    private readonly ITypeProvider _typeProvider;

    public SqlSortField(IPseudoField field, IOperationPrototype prototype, ITypeProvider typeProvider)
    {
        _field = field;
        _prototype = prototype;
        _typeProvider = typeProvider;
    }

    public string Name => _field.Name;
    public string ParentAlias => _prototype.ShortName;
    public string ProviderTypeName => _field.ProviderTypeName;
    public bool HasDisplayName => _field.HasDisplayName;
    public string DisplayName => _field.DisplayName;
    public int Order => _field.Order;
    public bool IsUuid => _field.ClrType == typeof(Guid);
    public bool Add => _field.Add;
    public bool Edit => _field.Edit;
    public bool IsUserEditable => _field.IsUserEditable;
    public bool IsKey => _field.IsKey;
    public bool IsInt => _field.IsInt;
    public bool HasSize => _field.Size != null;
    public int? Size => _field.Size;
    public Type ClrType => _field.ClrType;
    public bool IsGenerated => _field.IsGenerated;
    public bool IsRequired => _field.IsRequired;

    public string SortExpression
    {
        get
        {
            if (_field.ProviderTypeName.ToLowerInvariant() == "datetimeoffset")
            {
                return $"CAST ({_field.Name} as datetime)";
            }

            return  _typeProvider.EscapeReservedWord(_field.Name);
        }
    }

    public string SortExpressionWithParentAlias {         
        get
        {
            if (_field.ProviderTypeName.ToLowerInvariant() == "datetimeoffset")
            {
                return $"CAST ({_field.ParentAlias}.{_field.Name} as datetime)";
            }

            return  $"{_field.ParentAlias}.{_field.Name}";
        } 
    }
}