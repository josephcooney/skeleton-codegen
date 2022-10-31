using System.Collections.Generic;
using System.Linq;

namespace Skeleton.Model
{
    public class ResultType : SimpleType
    {
        public ResultType(string name, string ns, ApplicationType relatedType, bool isCustomType, Domain domain) : base(name, ns, domain)
        {
            Operations = new List<Operation>();
            RelatedType = relatedType;
            IsCustomType = isCustomType;
        }

        public ApplicationType RelatedType { get; }
        
        public List<Operation> Operations { get; }

        public bool Ignore => Operations.All(op => op.Ignore);
        
        public bool IsCustomType { get; }
    }
}