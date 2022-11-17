using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;

namespace Skeleton.Templating.Classes.Adapters
{
    public class ClientCustomTypeModel
    {
        private Domain _domain;
        private string _namespace;
        
        public ClientCustomTypeModel(OperationAdapter operation, Domain domain)
        {
            Name = operation.Name + NamingConventions.ModelClassNameSuffix;
            Fields = operation.UserProvidedParameters.Select(p => p.RelatedTypeField).ToList();
            _domain = domain;
            _namespace = operation.Namespace;
        }

        public ClientCustomTypeModel(ResultType resultType)
        {
            Name = resultType.Name.ToString();
            Fields = resultType.Fields.Where(f => f.IsUserEditable).ToList();
            _domain = resultType.Domain;
            _namespace = resultType.Namespace;
        }
        
        public string Name { get;  }
        
        public List<Field> Fields { get;  }
        
        public string Namespace
        {
            get
            {
                if (!string.IsNullOrEmpty(_domain.DefaultNamespace) && (string.IsNullOrEmpty(_namespace) || _namespace == _domain.TypeProvider.DefaultNamespace))
                {
                    return _domain.DefaultNamespace;
                }

                return _namespace;
            }
        }
    }
}