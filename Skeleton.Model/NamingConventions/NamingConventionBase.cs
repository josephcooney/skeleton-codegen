using System.Collections.Generic;
using System.Linq;

namespace Skeleton.Model.NamingConventions;

public abstract class NamingConventionBase 
{
    protected NamingConventionSettings _settings;
    
    protected string[] SetDefaultIfNotProvided(string[] currentValue, string defaultValue)
    {
        if (currentValue == null || !currentValue.Any())
        {
            return new[] { defaultValue };
        }

        return currentValue;
    }
    
    public bool IsTrackingUserFieldName(string fieldName)
    {
        return IsCreatedByFieldName(fieldName) || IsModifiedByFieldName(fieldName);
    }

    public bool IsCreatedByFieldName(string fieldName)
    {
        return _settings.CreatedUserFieldNames.Contains(fieldName);
    }

    public bool IsModifiedByFieldName(string fieldName)
    {
        return _settings.ModifiedUserFieldNames.Contains(fieldName);
    }
    
    public bool IsThumbnailFieldName(string fieldName)
    {
        return _settings.ThumbnailFieldNames.Contains(fieldName);
    }

    public bool IsContentTypeFieldName(string fieldName)
    {
        return _settings.ContentTypeFieldNames.Contains(fieldName);
    }
    
    public bool IsCreatedTimestampFieldName(string fieldName)
    {
        return _settings.CreatedTimestampFieldNames.Contains(fieldName);
    }

    public bool IsModifiedTimestampFieldName(string fieldName)
    {
        return _settings.ModifiedTimestampFieldNames.Contains(fieldName);
    }

    public string CreateResultTypeNameForOperation(string operationName)
    {
        return CreateNameFromFragments(new List<string>() { operationName, "result" }); // the literal string "result" here could be come a config setting
    }
    
    public abstract string CreateNameFromFragments(List<string> fragments);

    public bool SingularizeTypeNames => _settings.SingularizeTypeNames;
}