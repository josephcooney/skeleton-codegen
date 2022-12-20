namespace Skeleton.Model.Operations;

public interface ISortField : IPseudoField
{
    public string  SortExpression { get; }
}