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
            DartFileName = Util.SnakeCase(operation.Name + "_" + NamingConventions.ModelClassNameSuffix);
            Fields = operation.UserProvidedParameters.Where(p => p.RelatedTypeField != null).Select(p => p.RelatedTypeField).Cast<TypedValue>().ToList(); 
            Fields.AddRange(operation.UserProvidedParameters.Where(p => p.RelatedTypeField == null)); // this is to handle parameters that don't match anything on the underlying type
            _domain = domain;
            _namespace = operation.Namespace;
        }

        public ClientCustomTypeModel(ResultType resultType)
        {
            Name = resultType.Name;
            DartFileName = Util.SnakeCase(resultType.Name);
            Fields = resultType.Fields.Where(f => f.IsUserEditable).Cast<TypedValue>().ToList();
            _domain = resultType.Domain;
            _namespace = resultType.Namespace;
        }
        
        public string Name { get;  }

        public string DartFileName { get; }
        
        public List<TypedValue> Fields { get;  }
        
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