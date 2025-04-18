using System;
using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;
using Skeleton.Model.Operations;

namespace Skeleton.Templating.DatabaseFunctions.Adapters;

public class LinkAdapter
{
    private readonly IOperationPrototype _operationPrototype;

    public LinkAdapter(ApplicationType linkType, ApplicationType currentType, ApplicationType otherSide, IOperationPrototype operationPrototype)
    {
        _operationPrototype = operationPrototype;
        LinkType = linkType;
        OtherSideOfLink = otherSide;
        CurrentType = currentType;
    }
    public ApplicationType CurrentType { get; private set; }
    public ApplicationType LinkType { get; private set; }
    public ApplicationType OtherSideOfLink { get; private set; }

    public Domain Domain => CurrentType.Domain;

    public Field LinkingFieldToOtherSide => LinkType.Fields.First(f =>
        f.HasReferenceType && f.ReferencesType != CurrentType && !f.ReferencesType.IsSecurityPrincipal);
    
    public List<IPseudoField> InsertFields
    {
        get
        {
            var fields  = UserEditableFields.Where(f => f.Add).ToList();
            // we assume for linking tables the PK field is auto-generated, either a sequence or default value of uuid_generate_v4, 
            // so we don't need to add them here
                
            var createdDateTrackingField = LinkType.Fields.FirstOrDefault(a => a.IsCreatedDate);
            if (createdDateTrackingField != null)
            {
                fields.Add(Domain.TypeProvider.CreateFieldAdapter(createdDateTrackingField, _operationPrototype));
            }
            if (CreatedByField != null)
            {
                fields.Add(CreatedByField);
            }
            return fields.OrderBy(f => f.Order).ToList();
        }
    }
    
    public List<IPseudoField> UserEditableFields
    {
        get
        {
            var list = new List<IPseudoField>();
            list.AddRange(LinkType.Fields.Where(a => a.IsCallerProvided).Select(a => new LinkingFieldAdapter(a, _operationPrototype, Domain.TypeProvider, a.HasReferenceType && a.ReferencesType == CurrentType && !a.ReferencesType.IsSecurityPrincipal)).OrderBy(f => f.Order));
            return list;
        }
    }
    
    public IParamterPrototype CreatedByField
    {
        get
        {
            var field = LinkType.Fields.FirstOrDefault(f => f.IsTrackingUser && Domain.NamingConvention.IsCreatedByFieldName(f.Name));
            if (field != null)
            {
                return Domain.TypeProvider.CreateFieldAdapter(field, _operationPrototype);
            }
            return null;
        }
    }
}

// this is pretty postgres-specific - remains to be seen how we'd do this for SQL server
public class LinkingFieldAdapter : IParamterPrototype
{
    private readonly Field _field;
    private readonly IOperationPrototype _prototype;
    private readonly ITypeProvider _typeProvider;
    private readonly bool _isLinkToCurrentType;

    public LinkingFieldAdapter(Field field, IOperationPrototype prototype, ITypeProvider typeProvider, bool isLinkToCurrentType)
    {
        _field = field;
        _prototype = prototype;
        _typeProvider = typeProvider;
        _isLinkToCurrentType = isLinkToCurrentType;
    }

    public string Name => _field.Name;
    public string ParentAlias => _prototype.ShortName;
    public string ProviderTypeName => _field.ProviderTypeName;
    public bool HasDisplayName => false;
    public string DisplayName => null;
    public int Order => _field.Order;
    public bool IsUuid => _field.ClrType == typeof(Guid);
    public bool Add => _field.Add;
    public bool Edit => _field.Edit;
    public bool IsUserEditable => _field.IsCallerProvided;
    public bool IsKey => _field.IsKey;
    public bool IsInt => _field.IsInt;
    public bool HasSize => _field.HasSize;
    public int? Size => _field.Size;
    public Type ClrType => _field.ClrType;
    public bool IsGenerated => _field.IsGenerated;
    public bool IsRequired => _field.IsRequired;

    public string Value
    {
        get
        {
            if (_isLinkToCurrentType)
            {
                return "new_id";
            }
            else
            {
                return $"unnest({_typeProvider.FormatOperationParameterName(_prototype.NewRecordParameterName, _field.Name)})";
            }
        }
    }

    public IOperationPrototype Parent => _prototype;
}