using Skeleton.Model;
using Skeleton.Templating.Classes.Adapters;

namespace Skeleton.Templating.Classes.Repository
{
    public class DbOperationAdapter : OperationAdapter
    {
        public DbOperationAdapter(Operation op, Domain domain, ApplicationType type) : base(op, domain, type)
        {
        }

        public string SqlName
        {
            get
            {
                return _domain.TypeProvider.GetSqlName(_op.Name);
            }
        }
    }
}