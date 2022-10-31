using System.Collections.Generic;

namespace Skeleton.Model
{
    public class Constraint
    {
        public Constraint(string name)
        {
            Name = name;
            Fields = new List<Field>();
        }

        public string Name { get; }

        public List<Field> Fields { get; }
    }
}
