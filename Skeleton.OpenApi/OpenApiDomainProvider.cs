using System;
using System.Linq;
using Skeleton.Model;
using Skeleton.Templating;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NSwag;
using Serilog;

namespace Skeleton.OpenApi
{
    public class OpenApiDomainProvider
    {
        private readonly IOpenApiDocumentProvider _openApiDocumentProvider;

        public OpenApiDomainProvider(IOpenApiDocumentProvider openApiDocumentProvider)
        {
            _openApiDocumentProvider = openApiDocumentProvider;
        }
        
        public void  AugmentDomainFromOpenApi(Domain domain)
        {
            var doc = GetOpenApiDocument();
            if (doc == null)
            {
                Log.Error("Unable to augment domain model from OpenApi document - unable to retrieve OpenApi document");
                return;
            }

            foreach (var op in doc.Operations)
            {
                var domainOp = FindMatchingDomainOperation(domain, op, doc);
                if (domainOp.matchType != MatchType.Error)
                {
                    if (domainOp.operation == null)
                    {
                        AddOperation(domain, op, doc);
                    }
                    else
                    {
                        if (domainOp.matchType == MatchType.Replace)
                        {
                            UpdateOperation(domainOp.operation, op, domain, doc);
                        }
                    }
                }
            }    
        }
        
        private enum MatchType
        {
            Exact,
            Replace,
            None,
            Error
        }

        private (string noun, string verb) DecomposeRoutePath(string path)
        {
            var parts = path.Split('/');
            if (parts.Length < 4)
            {
                throw new FormatException("Route was not of the expected format");
            }
            var noun = parts[2];
            var verb = parts[3];
            return (noun, verb);
        }
        
        private (Operation operation, MatchType matchType) FindMatchingDomainOperation(Domain domain, OpenApiOperationDescription op, OpenApiDocument doc)
        {
            string noun = null;
            string verb = null;
            
            try
            {
                var routeParts = DecomposeRoutePath(op.Path);
                noun = Util.CanonicalizeName(routeParts.noun);
                verb = Util.CanonicalizeName(routeParts.verb);
            }
            catch (FormatException)
            {
                Log.Warning("Api Operation path {Path} was not of the expected format. Expected /api/[noun]/[verb]", op.Path);
                return (null, MatchType.Error);
            }
            
            var nameMatch = domain.Operations.Where(dmnOp => dmnOp.RelatedType != null && Util.CanonicalizeName(dmnOp.BareName) == verb && Util.CanonicalizeName(dmnOp.RelatedType.Name) == noun);
            if (nameMatch.Any())
            {
                if (nameMatch.Count() == 1)
                {
                    var domainOp = nameMatch.First();
                    Log.Debug("API operation {Path} matched {OperationName} by name", op.Path, domainOp.Name);
                    if (ParametersMatch(domainOp, domain, op, doc) && ReturnTypesMatch(domainOp, domain, op, doc))
                    {
                        return (domainOp, MatchType.Exact);
                    }
                    else
                    {
                        return (domainOp, MatchType.Replace);
                    }
                }
                else
                {
                    // TODO - find nearest match?
                    Log.Error("There were multiple operations that matched API operation {OperationName} by name.", op.Path);
                    return (null, MatchType.Error);
                }
            }
            return (null, MatchType.None); 
        }

        private bool ReturnTypesMatch(Operation domainOp, Domain domain, OpenApiOperationDescription op, OpenApiDocument doc)
        {
            return true;
        }

        private bool ParametersMatch(Operation domainOp, Domain domain, OpenApiOperationDescription op, OpenApiDocument doc)
        {
            // name and schema seem to be the most useful things about an OpenApiParameter (which ActualParameters is)
            return true;
        }

        private void AddOperation(Domain domain, OpenApiOperationDescription op, OpenApiDocument doc)
        {
            if (!OperationProducesBinary(op))
            {
                Log.Information("Adding operation {Path} to domain", op.Path);
                var parts = DecomposeRoutePath(op.Path);
                var noun = Util.CanonicalizeName(parts.noun);
                var relatedType = domain.Types.FirstOrDefault(t => Util.CanonicalizeName(t.Name) == noun);
                var domainOp = new Operation {Name = $"{ConvertApiStyleNameToStandardName(parts.noun)}_{ConvertApiStyleNameToStandardName(parts.verb)}", Namespace = domain.DefaultNamespace, RelatedType = relatedType };
                domainOp.Attributes = new JObject();
                domainOp.Attributes.HttpMethod = op.Method;

                if (op.Method == "post" || op.Method == "put")
                {
                    domainOp.Attributes.changesData = true;
                    domainOp.Attributes.createsNew = true; // don't do a 'select by Id' first
                }

                if (op.Method == "delete")
                {
                    domainOp.Attributes.isDelete = true;
                }
                
                PopulateNewOperationParameters(domain, op, doc, domainOp);

                if (op.Operation.ActualResponses.ContainsKey("200"))
                {
                    PopulateNewOperationResponse(domain, op, doc, domainOp);
                }
                else
                {
                    Log.Information("Operation {Path} does not define a successful response, so no domain operation will be added for it.", op.Path);
                }
                
                domain.Operations.Add(domainOp);
            }
            else
            {
                Log.Debug("OpenApi operation {Path} produces a file. We won't add operation information for it.", op.Path);
            }
        }

        private void PopulateNewOperationParameters(Domain domain, OpenApiOperationDescription op, OpenApiDocument doc, Operation domainOp)
        {
            var index = 0;
            foreach (var apiParameter in op.Operation.ActualParameters)
            {
                var clrType = GetClrTypeForJsonSchemaType(apiParameter.Schema.Type);
                if (clrType == null)
                {
                    // TODO - find or create a domain type to match this
                }

                var parameter = new Parameter(domain, domainOp) {Name = apiParameter.Name, ClrType = clrType, Order = index, Attributes = new JObject()};
                parameter.Attributes.userEditable = true;
                domainOp.Parameters.Add(parameter);
                index++;
                Log.Debug("Parameter: {param}", apiParameter);
            }
        }

        private Type GetClrTypeForJsonSchemaType(JsonObjectType schemaType)
        {
            switch (schemaType)
            {
                case JsonObjectType.Boolean:
                    return typeof(bool);

                case JsonObjectType.Integer:
                    return typeof(int);

                case JsonObjectType.Number:
                    return typeof(double);

                case JsonObjectType.String:
                    return typeof(string);

                default:
                    return null;
            }
        }

        private void PopulateNewOperationResponse(Domain domain, OpenApiOperationDescription op, OpenApiDocument doc,
            Operation domainOp)
        {
            var successResponse = op.Operation.ActualResponses["200"];

            if (successResponse.Schema == null)
            {
                domainOp.Returns = new OperationReturn {ReturnType = ReturnType.None};
            }
            else if (successResponse.Schema.Reference != null)
            {
                var responseType = FindExistingType(successResponse.Schema.ActualSchema, domain, doc, op);
                if (responseType != null)
                {
                    if (responseType is ApplicationType)
                    {
                        domainOp.Returns = new OperationReturn
                        {
                            SimpleReturnType = responseType,
                            ReturnType =
                                ReturnType
                                    .ApplicationType // TODO - need to revisit this. Also need to determine if it is List<T> or just T
                        };
                    }
                    else
                    {
                        if (responseType is ResultType)
                        {
                            var resType = responseType as ResultType;
                            resType.Operations.Add(domainOp);
                        }
                        
                        domainOp.Returns = new OperationReturn
                        {
                            SimpleReturnType = responseType,
                            ReturnType =
                                ReturnType
                                    .CustomType // TODO - need to revisit this. Also need to determine if it is List<T> or just T
                        };
                    }
                }
                else
                {
                    // TODO need to create a new response type
                }
            }
            else
            {
                switch (successResponse.Schema.Type)
                {
                    case JsonObjectType.Number:
                    case JsonObjectType.Integer:
                    case JsonObjectType.Boolean:
                    case JsonObjectType.String:
                        domainOp.Returns = new OperationReturn
                            {ReturnType = ReturnType.Primitive, ClrReturnType = GetClrTypeForJsonSchemaType(successResponse.Schema.Type)};
                        break;
                    
                    default:
                        Log.Information("Operation response type {ResponseType} for operation {Path} is not handled yet.",
                            successResponse.Schema.Type, op.Path);
                        break;
                }
            }
        }

        private SimpleType FindExistingType(JsonSchema schemaReference, Domain domain, OpenApiDocument doc, OpenApiOperationDescription op)
        {
            var key = doc.Components.Schemas.Keys.SingleOrDefault(k => doc.Components.Schemas[k] == schemaReference);
            if (key != null)
            {
                key = Util.CanonicalizeName(key);
                SimpleType matchingTypeByName = domain.Types.SingleOrDefault(t => Util.CanonicalizeName(t.Name) == key);
                if (matchingTypeByName != null)
                {
                    if (FieldsMatch(matchingTypeByName, schemaReference))
                    {
                        Log.Debug("Operation {Path} response type resolved to {TypeName}", op.Path, matchingTypeByName.Name);
                        return matchingTypeByName;
                    }
                }
                
                matchingTypeByName = domain.ResultTypes.SingleOrDefault(t => Util.CanonicalizeName(t.Name) == key);
                if (matchingTypeByName != null)
                {
                    if (FieldsMatch(matchingTypeByName, schemaReference))
                    {
                        Log.Debug("Operation {Path} response type resolved to {TypeName}", op.Path, matchingTypeByName.Name);
                        return matchingTypeByName;
                    }
                }
            }
            else
            {
                Log.Error("Unable to find response schema for {Path} in OpenApi list of components. Operation cannot be added", op.Path);
            }
            
            return null;
        }

        private bool FieldsMatch(SimpleType type, JsonSchema schema)
        {
            return true; // TODO
        }

        private bool OperationProducesBinary(OpenApiOperationDescription op)
        {
            return op.Operation.Responses.Any(resp => resp.Value.IsBinary(op.Operation));
        }

        private string ConvertApiStyleNameToStandardName(string name)
        {
            // temporary work-around until we stop treating names as just plain strings
            return name.Replace("-", "_");
        }
        
        private void UpdateOperation(Operation domainOpOperation, OpenApiOperationDescription op, Domain domain, OpenApiDocument doc)
        {
            // TODO
        }
        
        private OpenApiDocument GetOpenApiDocument()
        {
            return _openApiDocumentProvider.GetOpenApiDocument();
        }
    }
}