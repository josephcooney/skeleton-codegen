using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.CompilerServices;

namespace Skeleton.Model.NamingConventions;

public class PascalCaseNamingConvention : NamingConventionBase, INamingConvention
{
    Regex _namePartRegex = new Regex(@"([A-Z][a-z]+|[A-Z]+[A-Z]|[A-Z]|[^A-Za-z]+[^A-Za-z])", RegexOptions.RightToLeft);
    
    public PascalCaseNamingConvention(NamingConventionSettings settings)
    {
        if (settings != null)
        {
            _settings = settings;
        }
        else
        {
            _settings = new NamingConventionSettings();
        }

        _settings.CreatedUserFieldNames = SetDefaultIfNotProvided(_settings.CreatedUserFieldNames, "CreatedBy");
        _settings.ModifiedUserFieldNames = SetDefaultIfNotProvided(_settings.ModifiedUserFieldNames, "ModifiedBy");
        _settings.ThumbnailFieldNames = SetDefaultIfNotProvided(_settings.ThumbnailFieldNames, "Thumbnail");
        _settings.ContentTypeFieldNames = SetDefaultIfNotProvided(_settings.ContentTypeFieldNames, "ContentType");
        _settings.CreatedTimestampFieldNames = SetDefaultIfNotProvided(_settings.CreatedTimestampFieldNames, "Created");
        _settings.ModifiedTimestampFieldNames = SetDefaultIfNotProvided(_settings.ModifiedTimestampFieldNames, "Modified");
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

    public string SecurityUserIdParameterName => "SecurityUserIdParam";
    public string CreateNameFromFragments(List<string> fragments)
    {
        return PascalCaseName(fragments);
    }

    public bool EndsWithParts(string name, string[] parts)
    {
        var nameParts = GetNameParts(name).Reverse().ToList();
        var partsReversed = parts.Reverse();
        var index = 0;
        foreach (var part in partsReversed)
        {
            if (nameParts.IndexOf(part) != index)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    public static string PascalCaseName(IEnumerable<string> parts)
    {
        return string.Join("", parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
    }
}