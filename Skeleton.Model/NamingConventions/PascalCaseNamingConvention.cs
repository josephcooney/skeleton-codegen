using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Skeleton.Model.NamingConventions;

public class PascalCaseNamingConvention : INamingConvention
{
    private readonly NamingConventionSettings _settings;
    Regex _namePartRegex = new Regex(@"([A-Z][a-z]+|[A-Z]+[A-Z]|[A-Z]|[^A-Za-z]+[^A-Za-z])", RegexOptions.RightToLeft);
    
    public PascalCaseNamingConvention(NamingConventionSettings settings)
    {
        _settings = settings;
    }

    public string[] GetNameParts(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return Array.Empty<string>();
        }
        
        var items = new List<string>();
        var matches = _namePartRegex.Matches(name);
        foreach (Match match in matches)
        {
            items.Add(match.Value);
        }

        items.Reverse();
        return items.ToArray();
    }

    public string CreateParameterNameFromFieldName(string fieldName)
    {
        return fieldName + "Param";
    }
}