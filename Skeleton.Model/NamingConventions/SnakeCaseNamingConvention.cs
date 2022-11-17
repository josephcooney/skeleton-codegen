using System.Collections.Generic;
using System.Linq;

namespace Skeleton.Model.NamingConventions;

public class SnakeCaseNamingConvention : NamingConventionBase, INamingConvention
{
    private readonly ITypeProvider _typeProvider;

    public SnakeCaseNamingConvention(NamingConventionSettings settings, ITypeProvider typeProvider)
    {
        _typeProvider = typeProvider;
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
        _settings.CreatedTimestampFieldNames = SetDefaultIfNotProvided(_settings.CreatedTimestampFieldNames, "created");
        _settings.ModifiedTimestampFieldNames = SetDefaultIfNotProvided(_settings.ModifiedTimestampFieldNames, "modified");
    }

    public string[] GetNameParts(string name)
    {
        return name.Split('_');
    }

    public string CreateParameterNameFromFieldName(string fieldName)
    {
        return fieldName + "_param";
    }

    public string IdFieldName => "id";

    public bool IsSecurityUserIdParameterName(string fieldName)
    {
        return fieldName == SecurityUserIdParameterName;
    }

    public string SecurityUserIdParameterName => "security_user_id_param";
    public override string CreateNameFromFragments(List<string> fragments)
    {
        return string.Join('_', fragments);
    }

    public string EscapeSqlReservedWord(string name)
    {
        return _typeProvider.EscapeReservedWord(name);
    }
}