﻿using System.Linq;

namespace Skeleton.Model.NamingConventions;

public class SnakeCaseNamingConvention : NamingConventionBase, INamingConvention
{
    public SnakeCaseNamingConvention(NamingConventionSettings settings)
    {
        if (settings != null)
        {
            _settings = settings;
        }
        else
        {
            _settings = new NamingConventionSettings();
        }

        _settings.CreatedUserFieldNames = SetDefaultIfNotProvided(_settings.CreatedUserFieldNames, "created_by");
        _settings.ModifiedUserFieldNames = SetDefaultIfNotProvided(_settings.ModifiedUserFieldNames, "modified_by");
        _settings.ThumbnailFieldNames = SetDefaultIfNotProvided(_settings.ThumbnailFieldNames, "thumbnail");
        _settings.ContentTypeFieldNames = SetDefaultIfNotProvided(_settings.ContentTypeFieldNames, "content_type");
    }

    public string[] GetNameParts(string name)
    {
        return name.Split('_');
    }

    public string CreateParameterNameFromFieldName(string fieldName)
    {
        return fieldName + "_param";
    }
    
    public string SecurityUserIdParameterName => "security_user_id_param";
}