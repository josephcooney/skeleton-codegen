using System.Collections.Generic;

namespace Skeleton.Model.Operations;

// used for providing information about an operation you want to create to the type provider
public interface IOperationPrototype
{
    OperationType OperationType { get; }
    Name Name { get; }
    
    string FunctionName { get; }
    
    string ShortName { get; }
    
    bool UsesCustomInsertType { get; }
    
    bool AddMany { get; }
    
    string AddManyArrayItemVariableName { get; }
    
    string NewRecordParameterName { get; }
    
    List<IParamterPrototype> Fields { get; }
}