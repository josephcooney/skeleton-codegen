using System.Collections.Generic;

namespace Skeleton.Model;

public class EnumType : SimpleType
{
    private readonly List<string> _values;

    public EnumType(string name, string ns, Domain domain, List<string> values) : base(name, ns, domain)
    {
        _values = values;
        domain.EnumTypes.Add(this);
    }

    public List<string> Values => _values;
}