using System.Linq;
using Skeleton.Model;
using Skeleton.Templating.Classes;

namespace Skeleton.Templating.ReactClient.Adapters
{
    public class LinkingApiAdapter : ClientApiAdapter
    {
        private readonly ApplicationType _linkingType;

        public LinkingApiAdapter(ApplicationType type, Domain domain, ApplicationType linkingType) : base(type, domain)
        {
            _linkingType = linkingType;
            LinkingType = new ClientApiAdapter(linkingType, domain);
        }

        public ClientApiAdapter LinkingType { get; }
        
        public Field LinkingTypeIdField
        {
            get { return _linkingType.Fields.First(f => f.HasReferenceType && f.ReferencesType == base._type); }
        }

        public Field CurrentTypeIdField
        {
            get { return _linkingType.Fields.First(f => f.HasReferenceType && f.ReferencesType != base._type); }
        }
        
        public override SimpleType SelectAllType { get { return base.SelectAllType; } } // TODO - need to get 'select by' operation that links the two types, and return the result of that
    }
}
