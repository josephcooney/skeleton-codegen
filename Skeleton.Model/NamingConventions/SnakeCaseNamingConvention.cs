using System.Linq;

namespace Skeleton.Model.NamingConventions;

public class SnakeCaseNamingConvention : INamingConvention
{
    private readonly NamingConventionSettings _settings;

    public SnakeCaseNamingConvention(NamingConventionSettings settings)
    {
        if (settings != null)
        {
            _settings = settings;
        }
        else
        {
            _settings = new NamingConventionSettings() { CreatedUserFieldNames = new[]{"created_by"}, ModifiedUserFieldNames = new[]{"modified_by"} };
        }
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
    public bool IsTrackingUserFieldName(string fieldName)
    {
        return _settings.CreatedUserFieldNames.Contains(fieldName) ||
               _settings.ModifiedUserFieldNames.Contains(fieldName);
    }
}