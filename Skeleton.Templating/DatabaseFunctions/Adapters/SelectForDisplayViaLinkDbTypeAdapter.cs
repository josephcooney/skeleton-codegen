using System;
using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;
using Serilog;

namespace Skeleton.Templating.DatabaseFunctions.Adapters
{
    public class SelectForDisplayViaLinkDbTypeAdapter : SelectForDisplayDbTypeAdapter
    {
        private readonly ApplicationType _linkingType;
        private string _linkTypeAlias;

        public SelectForDisplayViaLinkDbTypeAdapter(ApplicationType applicationType, string operation, ApplicationType linkingType, Domain domain) : base(applicationType, operation, domain)
        {
            _linkingType = linkingType ?? throw new ArgumentNullException(nameof(linkingType));
        }

        public ApplicationType LinkingType => _linkingType;

        public string LinkTypeAlias
        {
            get
            {
                if (_linkTypeAlias == null)
                {
                    _linkTypeAlias = CreateAliasForLinkingField(LinkingTypeFieldRaw);
                }

                return _linkTypeAlias;
            }
        }

        public IPseudoField LinkingTypeField // this would need to be a List<DbFieldAdapter> and the template do a foreach for this to handle composite primary keys
        {
            get
            {
                return _applicationType.Domain.TypeProvider.CreateFieldAdapter(LinkingTypeFieldRaw, this);
            }
        }

        private Field LinkingTypeFieldRaw => _linkingType.Fields.SingleOrDefault(f => f.HasReferenceType && f.ReferencesType == this._applicationType && !f.IsTrackingUser);

        public IPseudoField LinkTypeOtherField
        {
            get
            {
                var fields = _linkingType.Fields
                    .Where(f => f.HasReferenceType && f.ReferencesType != _applicationType &&
                                !f.ReferencesType.IsSecurityPrincipal).Select(f => _applicationType.Domain.TypeProvider.CreateFieldAdapter(f, this));
                if (fields.Count() > 1)
                {
                    var fieldNames = string.Join(',', fields.Select(f => f.Name));
                    
                    Log.Error("Cannot determine linking type from {Name} to {LinkingTypeName}. It has multiple links {FieldNames}.", _applicationType.Name, _linkingType.Name, fieldNames);
                    throw new InvalidOperationException(
                        $"Cannot determine linking type from {_applicationType.Name} to {_linkingType.Name}. It has multiple links via fields " + fieldNames);
                }
                else
                {
                    return fields.SingleOrDefault();
                }
            }
        }

        public override List<IPseudoField> SelectInputFields
        {
            get
            {
                var fields = base.SelectInputFields;
                fields.Add(LinkTypeOtherField);
                return fields;
            }
        }

        public override string FunctionName => _applicationType.Name + "_select_via_" + LinkTypeOtherField.Name;
    }
}
