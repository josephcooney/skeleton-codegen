using System;

namespace Skeleton.Model;

public abstract class TypedValue
{
    protected readonly Domain _domain;

    public TypedValue(Domain domain)
    {
        _domain = domain;
    }
    
    public virtual string Name { get; set; }
    
    public virtual int Order { get; set; }
    
    public virtual Type ClrType { get; set; }
    
    public virtual string ProviderTypeName { get; set; }
    
    public dynamic Attributes { get; set; }
    
    public bool IsBoolean => ClrType == typeof(bool) || ClrType == typeof(bool?);
    
    public virtual bool IsInt => ClrType == typeof(int) || ClrType == typeof(int?);
    
    public bool IsDateTime => (ClrType == typeof(DateTime) || ClrType == typeof(DateTime?));
    
    public bool IsDate => _domain.TypeProvider.IsDateOnly(ProviderTypeName);
    
    public abstract bool IsLargeTextContent { get; }
    
    public abstract bool IsHtml { get; }
    
    public abstract bool IsFile { get; }
    
    public abstract bool IsRating { get; }
    
    public abstract bool IsColor { get; }
}