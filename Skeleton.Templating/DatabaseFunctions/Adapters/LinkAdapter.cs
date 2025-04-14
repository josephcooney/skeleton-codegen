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
            var searchField = LinkType.Fields.FirstOrDefault(f => f.IsSearch);
            if (searchField != null)
            {
                fields.Add(Domain.TypeProvider.CreateFieldAdapter(searchField, _operationPrototype));
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
            list.AddRange(LinkType.Fields.Where(a => a.IsCallerProvided).Select(a => Domain.TypeProvider.CreateFieldAdapter(a, _operationPrototype)).OrderBy(f => f.Order));
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