using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;

namespace Skeleton.Templating.Classes.Repository
{
    public class RepositoryAdapter : ClassAdapter
    {
        
        public RepositoryAdapter(Domain domain, ApplicationType type) : base(type, domain)
        {
            Type = type;
        }

        public ApplicationType Type { get; }

        public new List<DbOperationAdapter> Operations
        {
            get { return _domain.Operations.Where(o => !o.Ignore && o.Attributes?.applicationtype == Type.Name || o.Returns.SimpleReturnType == Type).OrderBy(o => o.Name).Select(o => new DbOperationAdapter(o, _domain, Type)).ToList(); }
        }

        public List<ReturnModel> DistinctReturnTypes
        {
            get
            {
                return Operations.Where(o => !o.NoResult).Select(o => new ReturnModel(o.Returns, o.ReturnTypeName)).Distinct().ToList();
            }
        }
    }

    public class ReturnModel
    {
        public ReturnModel(string returns, string typeName)
        {
            Returns = returns;
            TypeName = typeName;
        }
        
        public string Returns { get; set; }
        
        public string TypeName { get; set; }
    }
}
