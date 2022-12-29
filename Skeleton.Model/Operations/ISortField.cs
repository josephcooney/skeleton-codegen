namespace Skeleton.Model.Operations;

public interface ISortField : IPseudoField
{
    public string  SortExpression { get; }
    
    public string SortExpressionWithParentAlias { get; }
}