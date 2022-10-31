namespace Skeleton.Model.Operations;

public interface IParamterPrototype : IPseudoField
{
    public string Value { get; }
    
    public IOperationPrototype Parent { get; }
}