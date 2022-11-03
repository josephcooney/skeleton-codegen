using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Skeleton.Model;
using Skeleton.Model.Operations;
using Skeleton.Templating.DatabaseFunctions.Adapters.Fields;
using Serilog;

namespace Skeleton.Templating.DatabaseFunctions.Adapters
{
    public class SelectForDisplayDbTypeAdapter : DbTypeAdapter
    {
        private List<IPseudoField> _fields;
        private FieldEntityAliasDictionary _aliases = new FieldEntityAliasDictionary();

        public SelectForDisplayDbTypeAdapter(ApplicationType applicationType, string operation, Domain domain) : base(applicationType, operation, OperationType.Select, domain)
        {
            try
            {
                _aliases.Add(ShortName, applicationType.Fields.First(f => f.IsKey));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error creating SelectForDisplayDbTypeAdapter for {Name}. It may be missing an identity field.", applicationType.Name);
                throw;
            }
        }

        public List<IPseudoField> DisplayAllFields
        {
            get
            {
                if (_fields == null)
                {
                    _fields = new List<IPseudoField>();
                    foreach (var fld in _applicationType.Fields)
                    {
                        if (!fld.IsExcludedFromResults && !fld.IsFile ) //exclude search fields, soft delete flag, and files - files are too big to return in a 'select all'
                        {
                            _fields.Add(_applicationType.Domain.TypeProvider.CreateFieldAdapter(fld, this));
                        }

                        if (fld.ReferencesType != null)
                        {
                            TraverseRelatedFields(_fields, fld, this.ShortName);
                        }
                    }
                }

                return _fields.Where(f => !(f is JoiningField)).ToList();
            }
        }

        public List<IJoiningField> RelatedFields => _fields.Where(f => f is IJoiningField).Cast<IJoiningField>().ToList();

        protected string CreateAliasForLinkingField(Field field)
        {
            return _aliases.CreateAliasForLinkingField(field);
        }

        protected string GetAliasForLinkingField(Field field)
        {
            return _aliases.GetAliasForLinkingField(field);
        }

        public string OwnershipExpression => GenerateOwnershipExpression(Domain.NamingConvention.SecurityUserIdParameterName);

        private void TraverseRelatedFields(List<IPseudoField> fields, Field fld, string currentAlias)
        {
            if (fld.ReferencesType != null && !fld.ReferencesType.IsLink && fld.ReferencesType.DisplayField != null)
            {
                var alias = CreateAliasForLinkingField(fld);
                _fields.Add(new RelatedTypeField(fld, currentAlias, alias));
            }
            else if (fld.ReferencesType != null && fld.ReferencesType.IsLink)
            {
                // traverse relationships between "link" tables 
                var alias = CreateAliasForLinkingField(fld);
                _fields.Add(new JoiningField(fld, currentAlias, alias));
                TraverseRelatedFields(_fields, fld.ReferencesTypeField, alias);
            }
        }

        private string GenerateOwnershipExpression(string currentUserIdentifier)
        {
            var sb = new StringBuilder();
            if (HasCreatedByField || HasModifiedByField)
            {
                GetDirectOwnershipExpression(currentUserIdentifier, sb, GetAliasForLinkingField(_applicationType.Fields.First(f => f.IsKey)));
            }
            else
            {
                var related = LinkToOwershipType;
                if (related != null)
                {
                    GenerateOwnershipJoinExpression(related, sb, _aliases);
                    var lastLinkToOwnershipType = related.Last();
                    var relatedIdentity = lastLinkToOwnershipType.ReferencesType.Fields.Where(f => f.IsTrackingUser);
                    GetRelatedOwnershipExpression(currentUserIdentifier, relatedIdentity, sb, GetAliasForLinkingField);
                }
                else
                {
                    sb.AppendLine("TODO"); // unknown relationship type
                }
            }

            return sb.ToString();
        }

        public virtual bool FilterListOperation => AllowAnonView && !AllowAnonList && AllowUserList;

        protected void GenerateOwnershipJoinExpression(List<Field> related, StringBuilder sb, FieldEntityAliasDictionary aliases)
        {
            Field previous = null;
            string prevAlias = null;
            foreach (var field in related)
            {
                var alias = aliases.CreateAliasForTypeByField(field);

                if (previous == null)
                {
                    sb.AppendLine($"\t{field.Type.Name} as {alias}");
                }
                else
                {
                    sb.AppendLine($"\tINNER JOIN {field.Type.Name} as {alias} on {prevAlias}.{previous.Name} = {alias}.{previous.ReferencesTypeField.Name}");
                }

                prevAlias = alias;
                previous = field;
            }
        }
    }
}