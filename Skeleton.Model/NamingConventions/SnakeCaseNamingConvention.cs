namespace Skeleton.Model.NamingConventions;

public class SnakeCaseNamingConvention : INamingConvention
{
    private readonly NamingConventionSettings _settings;

    public SnakeCaseNamingConvention(NamingConventionSettings settings)
    {
        _settings = settings;
    }

    public string[] GetNameParts(string name)
    {
        return name.Split('_');
    }
}