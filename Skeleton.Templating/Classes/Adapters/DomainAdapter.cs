using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;

namespace Skeleton.Templating.Classes
{
    public class DomainAdapter
    {
        private readonly Domain _domain;

        public DomainAdapter(Domain domain)
        {
            _domain = domain;
        }

        public List<ClassAdapter> IncludedTypes => _domain.Types.Where(a => !a.Ignore).OrderBy(t => t.Name).Select(t => new ClassAdapter(t, _domain)).ToList();

        public List<ClassAdapter> CustomTypes => _domain.ResultTypes.Where(rt => rt.IsCustomType).Select(rt => new ClassAdapter(rt, _domain)).ToList();

        public string DefaultNamespace => _domain.DefaultNamespace;
    }
}
