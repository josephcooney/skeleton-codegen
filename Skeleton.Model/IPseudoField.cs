using System;

namespace Skeleton.Model
{
    public interface IPseudoField
    {
        string Name { get; }
        string ParentAlias { get; }
        string ProviderTypeName { get; }
        bool HasDisplayName { get; }
        string DisplayName { get; }
        int Order { get; }
        bool IsUuid { get; }
        bool Add { get; } // true if it should be provided when creating a new entity, otherwise false
        bool Edit { get; } // true if it can be provided when editing an existing entity, otherwise false
        
        bool IsUserEditable { get; }
        
        bool IsIdentity { get; }
        
        bool IsInt { get; }
        
        bool HasSize { get; }
        
        int? Size { get; }
        
        Type ClrType { get; }
        
        bool IsGenerated { get; }
        
        bool IsRequired { get; }
    }
}