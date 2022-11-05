using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

}