using System.Collections.Generic;
using System.Linq;

namespace Skeleton.Model
{
    public class SimpleType
    {
        public SimpleType(string name, string ns, Domain domain)
        {
            Name = name;
            Namespace = ns;
            Fields = new List<Field>();
            Domain = domain;
        }

        public string Name { get; }

        public string Namespace { get; }
        
        public Domain Domain { get; }

        public List<Field> Fields { get; }
        
        public Field DisplayField
        {
            get
            {
                var displayField = Fields.OrderBy(f => f.Order).FirstOrDefault(f => f.IsDisplayField);
                if (displayField != null)
                {
                    return displayField;
                }
                return Fields.OrderBy(f => f.Rank).FirstOrDefault(f => f.ClrType == typeof(string));
            }
        }

        public Field IdentityField
        {
            get
            {
                return Fields.First(f => f.IsKey);
            }
        } 
        
        public dynamic Attributes { get; set; }

        public Field GetFieldByName(string name)
        {
            return Fields.SingleOrDefault(f => f.Name == name);
        }
    }
}