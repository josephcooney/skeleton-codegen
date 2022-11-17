using System.Collections.Generic;

namespace Skeleton.Model.NamingConventions;

public interface INamingConvention
{
    string[] GetNameParts(string name);

    string CreateParameterNameFromFieldName(string fieldName);
    string SecurityUserIdParameterName { get; }
    string IdFieldName { get; }

    bool IsSecurityUserIdParameterName(string fieldName);
    
    bool IsTrackingUserFieldName(string fieldName);

    bool IsCreatedByFieldName(string fieldName);
    
    bool IsModifiedByFieldName(string fieldName);

    bool IsThumbnailFieldName(string fieldName);

    bool IsContentTypeFieldName(string fieldName);

    bool IsCreatedTimestampFieldName(string fieldName);

    bool IsModifiedTimestampFieldName(string fieldName);

    string CreateNameFromFragments(List<string> fragments);

    string CreateResultTypeNameForOperation(Name operationName);

    string EscapeSqlReservedWord(string name);
}