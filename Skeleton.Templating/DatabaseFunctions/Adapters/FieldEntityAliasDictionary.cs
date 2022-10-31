using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;

namespace Skeleton.Templating.DatabaseFunctions.Adapters
{
    public class FieldEntityAliasDictionary
    {
        private Dictionary<string, Field> _aliases = new Dictionary<string, Field>();

        public string CreateAliasForLinkingField(Field field)
        {
            var chr = field.ReferencesType.Name[0].ToString().ToLowerInvariant();
            if (!_aliases.ContainsKey(chr))
            {
                _aliases.Add(chr, field);
                return chr;
            }

            var index = 1;
            while (true)
            {
                var alias = chr + index;
                if (!_aliases.ContainsKey(alias))
                {
                    _aliases.Add(alias, field);
                    return alias;
                }

                index++;
            }
        }

        public string CreateAliasForTypeByField(Field f)
        {
            var chr = f.Type.Name[0].ToString().ToLowerInvariant();
            if (!_aliases.ContainsKey(chr))
            {
                _aliases.Add(chr, f);
                return chr;
            }

            var index = 1;
            while (true)
            {
                var alias = chr + index;
                if (!_aliases.ContainsKey(alias))
                {
                    _aliases.Add(alias, f);
                    return alias;
                }

                index++;
            }
        }

        public string GetAliasForLinkingField(Field field)
        {
            return _aliases.FirstOrDefault(v => v.Value == field).Key;
        }

        public void Add(string alias, Field field)
        {
            _aliases.Add(alias, field);
        }
    }
}
