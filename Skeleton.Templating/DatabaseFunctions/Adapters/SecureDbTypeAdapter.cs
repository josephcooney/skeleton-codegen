using System.Collections.Generic;
using System.Linq;
using System.Text;
using Skeleton.Model;
using Skeleton.Model.Operations;

namespace Skeleton.Templating.DatabaseFunctions.Adapters
{
    public class SecureDbTypeAdapter : DbTypeAdapter
    {
        public SecureDbTypeAdapter(ApplicationType applicationType, Domain domain) : base(applicationType, null, OperationType.None, domain)
        {
        }

        public string OwnershipExpression
        {
            get { return GenerateOwnershipExpression("current_setting('app.user_id', 't')::" + UserIdField?.ProviderTypeName); }
        }

        private string GenerateOwnershipExpression(string currentUserIdentifier)
        {
            var sb = new StringBuilder();
            if (HasCreatedByField || HasModifiedByField)
            {
                GetDirectOwnershipExpression(currentUserIdentifier, sb, null);
            }
            else
            {
                var related = LinkToOwershipType;
                if (related != null && related.Any())
                {
                    var aliases = new FieldEntityAliasDictionary();

                    sb.AppendLine("EXISTS");
                    sb.AppendLine("(");
                    sb.AppendLine("\tSELECT TRUE");
                    sb.AppendLine($"\tFROM");

                    GenerateOwnershipJoinExpression(related, sb, aliases);

                    sb.AppendLine("\tWHERE");

                    var lastLinkToOwnershipType = related.Last();
                    var relatedIdentity = lastLinkToOwnershipType.Type.Fields.Where(f => f.IsTrackingUser);
                    GetRelatedOwnershipExpression(currentUserIdentifier, relatedIdentity, sb, (f) => aliases.GetAliasForLinkingField(lastLinkToOwnershipType));
                    sb.AppendLine("\tAND");

                    var linkOnCurrentType = related.First();
                    sb.AppendLine(
                        $"\t{aliases.GetAliasForLinkingField(related.Skip(1).Take(1).Single())}.{Util.EscapeSqlReservedWord(linkOnCurrentType.ReferencesTypeField.Name)} = {Util.EscapeSqlReservedWord(linkOnCurrentType.Type.Name)}.{Util.EscapeSqlReservedWord(linkOnCurrentType.Name)}");

                    sb.AppendLine(")");
                }
                else
                {
                    sb.AppendLine("TODO"); // unknown relationship type
                }
            }

            return sb.ToString();
        }

        protected void GenerateOwnershipJoinExpression(List<Field> related, StringBuilder sb, FieldEntityAliasDictionary aliases)
        {
            Field previous = null;
            string prevAlias = null;
            // skip the first item when building the join expression for the security policy because the type we are building the policy for
            // will be automatically included as a set of items to filter, and explicitly including it in the policy causes an  indefinitely recursive 
            // security policy to be created (which doesn't work) 
            foreach (var field in related.Skip(1))  
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
