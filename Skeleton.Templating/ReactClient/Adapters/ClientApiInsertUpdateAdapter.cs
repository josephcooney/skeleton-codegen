using System.Collections.Generic;
using System.Linq;
using Serilog;
using Skeleton.Model;
using Skeleton.Templating.Classes;

namespace Skeleton.Templating.ReactClient.Adapters
{
    public class ClientApiInsertUpdateAdapter : ClientDetailAdapter
    {
        private readonly ClientApiOperationAdapter _operation;

        public ClientApiInsertUpdateAdapter(ApplicationType type, Domain domain, ClientApiOperationAdapter operation) : base(type, domain)
        {
            _operation = operation;
        }

        public ClientApiOperationAdapter CurrentOperation => _operation;

        public string OperationName => Util.CSharpNameFromName(_operation.BareName);

        public string OperationNameFriendly => _operation.FriendlyName;

        public bool IsUpdate => !_operation.CreatesNew;

        public bool AssociateViaLink => _operation.CreatesNew && !((ApplicationType) base._type).IsLink;

        public string StateTypeName => $"{Util.CSharpNameFromName(Name)}{OperationNameFriendly}State";

        public string ModelTypeName => _operation.ModelTypeName;

        public string FormDataTypeName => _operation.UsesModel ? ModelTypeName : StateTypeName;

        public bool HasAnyHtmlFields => _operation.Parameters.Any(p => p.IsHtml) || (_operation.HasCustomType && _operation.CustomType.Fields.Any(f => f != null && f.IsHtml));

        public List<ClassAdapter> ParameterReferenceTypes
        {
            get
            {
                var baseList = CurrentOperation.ParameterReferenceTypes;

                var linkingTypes = _applicationType.LinkedTypes.Where(t => t.IsLink);
                foreach (var link in linkingTypes)
                {
                    var otherSideOfLink = link.Fields.Where(f => f.HasReferenceType && f.ReferencesType != _type && !f.ReferencesType.IsSecurityPrincipal).Select(f => f.ReferencesType).ToList();
                    if (otherSideOfLink.Count() > 1)
                    {
                        Log.Warning("Looking for links to {TypeName} - Link type {LinkTypeName} links to multiple 'other' things. Templates do not support this.", _type.Name, link.Name); // templates have not been designed for this
                    }
                    else
                    {
                        if (otherSideOfLink.Any())
                        {
                            baseList.Add(new ClassAdapter(otherSideOfLink.First(), _domain));
                        }
                    }
                }
                
                return baseList;
            }
        }

        public bool ReferencesSelf =>
            _type.Fields.Any(f => f.RelatedTypeField != null && f.RelatedTypeField.ReferencesType == _type);
    }
}
