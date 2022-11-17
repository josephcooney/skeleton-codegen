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

        public string OperationName => _operation.Name.BareName.CSharpName;

        public string OperationNameFriendly => _operation.FriendlyName;

        public bool IsUpdate => !_operation.CreatesNew;

        public bool AssociateViaLink => _operation.CreatesNew && !((ApplicationType) base._type).IsLink;

        public string StateTypeName => $"I{Name.CSharpName}{OperationNameFriendly}State";

        public string ModelTypeName => _operation.UsesModel ? $"{Name.CSharpName}{OperationName}{NamingConventions.ModelClassNameSuffix}" : null;

        public string FormDataTypeName => _operation.UsesModel ? ModelTypeName : StateTypeName;
    }
}
