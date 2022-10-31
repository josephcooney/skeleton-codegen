namespace Skeleton.Model.NamingConventions;

public class NamingConventionSettings
{
    public DbNamingConvention DbNamingConvention { get; set; }
    
    public string[] CreatedUserFieldNames { get; set; }
    public string[] CreatedTimestampFieldNames { get; set; }
    public string[] ModifiedUserFieldNames { get; set; }
    public string[] ModifiedTimestampFieldNames { get; set; }
}

public enum DbNamingConvention
{
    ProviderDefault = 0,
    SnakeCase = 1,
    PascalCase = 2
}

