using System.Collections.Generic;
using System.Linq;

namespace Skeleton.Model
{
    // TODO - cut over to using 'name' instead of strings to represent the name of things
    public class Name
    {
        private readonly NameStrategy _strategy;
        private List<string> parts = new List<string>();
        
        public Name(string bareName, NameStrategy strategy)
        {
            _strategy = strategy;
            switch (strategy)
            {
                case NameStrategy.Capitalization:
                    parts = SplitNameByCapitals(bareName);
                    break;
                case NameStrategy.Dashes:
                    parts = SplitNameByDelimiter(bareName, "-");
                    break;
                case NameStrategy.Underscores:
                    parts = SplitNameByDelimiter(bareName, "_");
                    break;
            }
        }

        private List<string> SplitNameByDelimiter(string bareName, string delimeter)
        {
            return bareName.Split(delimeter).ToList();
        }

        private List<string> SplitNameByCapitals(string bareName)
        {
            // TODO
            return null;
        }

        string ToCsharpName()
        {
            return string.Join("", parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
        }
    }

    public enum NameStrategy
    {
        Capitalization,
        Underscores,
        Dashes
    }
}