using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;

namespace Skeleton.Templating.Classes.Repository
{
    public class RepositoryAdapter
    {
        private readonly Domain _domain;

        public RepositoryAdapter(Domain domain, ApplicationType type)
        {
            _domain = domain;
            Type = type;
        }

        public ApplicationType Type { get; }

        public List<DbOperationAdapter> Operations
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
        
        public bool HasCustomResultType
        {
            get { return _domain.Operations.Any(o => !o.Ignore && o.Returns.SimpleReturnType != null && (o.Returns.SimpleReturnType is ResultType) && !((ResultType)o.Returns.SimpleReturnType).Ignore); }
        }

        public string Namespace
        {
            get
            {
                if (!string.IsNullOrEmpty(_domain.DefaultNamespace) && (string.IsNullOrEmpty(Type.Namespace) || Type.Namespace == _domain.TypeProvider.DefaultNamespace))
                {
                    return _domain.DefaultNamespace;
                }

                return Type.Namespace;
            }
        }

        public string DomainNamespace
        {
            get
            {
                if (!string.IsNullOrEmpty(_domain.Settings.DomainNamespace))
                {
                    return _domain.Settings.DomainNamespace;
                }
                else
                {
                    return $"{Util.CSharpNameFromName(Namespace)}.Data.Domain";
                }
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
