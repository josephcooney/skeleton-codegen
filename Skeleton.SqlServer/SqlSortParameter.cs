using Skeleton.Model;
using Skeleton.Model.NamingConventions;

namespace Skeleton.SqlServer;

public class SqlSortParameter : IPseudoField
{
    private readonly INamingConvention _namingConvention;

    public SqlSortParameter(INamingConvention namingConvention)
    {
        _namingConvention = namingConvention;
    }
        
    public string Name => GetNameForNamingConvention(_namingConvention);
    public string ParentAlias => null;
    public string ProviderTypeName => "varchar";
    public bool HasDisplayName => false;
    public string DisplayName => null;
    public int Order => 0;
    public bool IsUuid => false;
    public bool Add => false;
    public bool Edit => false;
    public bool IsUserEditable => true;
    public bool IsKey => false;
    public bool IsInt => false;
    public bool HasSize => true;
    public int? Size => 100;
    public Type ClrType => typeof(string);
    public bool IsGenerated => false;
    public bool IsRequired => false;
        
    public static string GetNameForNamingConvention(INamingConvention namingConvention)
    {
        return namingConvention.CreateNameFromFragments(new List<string> { "sort", "field" });
    }
}