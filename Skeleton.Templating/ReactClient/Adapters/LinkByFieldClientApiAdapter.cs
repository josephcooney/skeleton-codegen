using System;
using System.Linq;
using Serilog;
using Skeleton.Model;

namespace Skeleton.Templating.ReactClient.Adapters
{
    public class LinkByFieldClientApiAdapter : ClientApiAdapter
    {
        private readonly Field _linkingField;

        public LinkByFieldClientApiAdapter(ApplicationType type, Domain domain, Field linkingField) : base(type, domain)
        {
            _linkingField = linkingField;
        }

        public string LinkingFieldName => _linkingField.Name;

        public string LinkingFieldBareName => Util.BareName(_linkingField.Name, _type.Name);


        public string LinkingFieldTsType => Util.GetTypeScriptTypeForClrType(_linkingField.ClrType);

        public override SimpleType SelectAllType
        {
            get
            {
                var operations = _domain.Operations.Where(op => op.RelatedType == _type && op.UserProvidedParameters.Any() && op.UserProvidedParameters.Count() == 1 && !op.CreatesNew && !op.ChangesData && op.UserProvidedParameters.First().RelatedTypeField == _linkingField);
                if (operations.Count() == 1)
                {
                    return operations.First().Returns.SimpleReturnType;
                }

                if (operations.Count() == 0)
                {
                    throw new InvalidOperationException($"Unable to find operation to return {_type.Name} from linking field {_linkingField.Name}");
                }

                if (operations.Count() > 1)
                {
                    var distinctReturnTypes = operations.Where(o => o.Returns.SimpleReturnType != _type /* exclude functions that just do a 'basic' return - we want the 'select_for_display' variation */).Select(o => o.Returns.SimpleReturnType).Distinct();
                    if (distinctReturnTypes.Count() == 0)
                    {
                        // this is a weird case where there isn't a 'display' variant 
                        return null;
                    }
                    
                    if (distinctReturnTypes.Count() == 1)
                    {
                        return distinctReturnTypes.First();
                    }

                    Log.Error("There are multiple operations to return {TypeName} from linking field {LinkingFieldName} with different return types. Operation names are {OperationNames}", _type.Name, _linkingField.Name, operations.Select(o => o.Name).ToList());
                    
                    throw new InvalidOperationException($"There are multiple operations to return {_type.Name} from linking field {_linkingField.Name} with different return types.");
                }

                return null;
            }
        }
    }
}
