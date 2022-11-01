namespace Skeleton.Model.NamingConventions;

public interface INamingConvention
{
    string[] GetNameParts(string name);

    string CreateParameterNameFromFieldName(string fieldName);
}