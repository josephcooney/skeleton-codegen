using System.Linq;
using Skeleton.Model;

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
    }
}
