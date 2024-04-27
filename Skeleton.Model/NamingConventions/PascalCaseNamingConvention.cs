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
        // replace any underscores with spaces, and then split on spaces to handle 'hybrid' names better
        var subParts = name.Replace("_", " ").Split(" ");
        foreach (var subPart in subParts.Reverse())
        {
            if (_namePartRegex.IsMatch(subPart))
            {
                var matches = _namePartRegex.Matches(subPart);
                foreach (Match match in matches)
                {
                    items.Add(match.Value);
                }   
            }
            else
            {
                items.Add(subPart);
            }
        }
        
        items.Reverse();
        return items.Where(s => !string.IsNullOrEmpty(s)).ToArray();
    }

    public string CreateParameterNameFromFieldName(string fieldName)
    {
        return fieldName + "Param";
    }

    public string IdFieldName => "Id";

    public bool IsSecurityUserIdParameterName(string fieldName)
    {
        return fieldName.Equals(SecurityUserIdParameterName, StringComparison.InvariantCultureIgnoreCase);
    }

    public string SecurityUserIdParameterName => "SecurityUserIdParam";
    public override string CreateNameFromFragments(List<string> fragments)
    {
        return PascalCaseName(fragments);
    }
    
    public static string PascalCaseName(IEnumerable<string> parts)
    {
        parts = parts.Where(s => !string.IsNullOrEmpty(s));
        return string.Join("", parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
    }
}