using System;
using Skeleton.Model;

namespace Skeleton.Templating.DatabaseFunctions.Adapters.Fields;

// not sure if I should be using JoiningField here or not 
public class LinkingField : IJoiningField
{
    private readonly Field _field;
    private readonly Field _currentTypeField;
    private readonly string _alias;
    private readonly string _relatedAlias;
    private readonly Domain _domain;

    public LinkingField(Field field, Field currentTypeField, string alias, string relatedAlias)
    {
        _field = field;
        _currentTypeField = currentTypeField;
        _alias = alias;
        _relatedAlias = relatedAlias;
        _domain = field.ReferencesType.Domain;
    }

    public Field Field => _field;

    public string Name => Util.PluraliseParameterName(_field.Name);

    public string ParentAlias => _relatedAlias;
    public string ProviderTypeName => $"{_field.ProviderTypeName}[]"; // this is really postgres-specific
    public bool HasDisplayName => false;
    public string DisplayName => null;
    public int Order => _field.Order;
    public bool IsUuid => _field.ClrType == typeof(Guid);
    public bool Add => _field.Add;
    public bool Edit => _field.Edit;

    public bool IsUserEditable => _field.IsCallerProvided;
    public bool IsKey => _field.IsKey;
    public bool IsInt => _field.IsInt;
    public bool HasSize => _field.Size != null;
    public int? Size => _field.Size;
    public Type ClrType => _field.ClrType;
    public bool IsGenerated => false;
    public bool IsRequired => false;

    public string PrimaryAlias => _alias;

    public bool IsLinkingField => true;

    public string SelectExpression => $"array(select {_domain.TypeProvider.EscapeSqlName(_field.Name)} from {_domain.TypeProvider.EscapeSqlName(_field.Type.Name)} where {_domain.TypeProvider.EscapeSqlName(_currentTypeField.Name)} = {_alias}.{_domain.TypeProvider.EscapeSqlName(_currentTypeField.ReferencesTypeField.Name)}) as {_domain.TypeProvider.EscapeSqlName(Name)}";
}