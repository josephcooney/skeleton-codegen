using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;
using Skeleton.Templating.Classes;
using Skeleton.Templating.Classes.Adapters;
using Skeleton.Templating.DatabaseFunctions;
using Skeleton.Templating.DatabaseFunctions.Adapters;
using Serilog;

namespace Skeleton.Templating.ReactClient.Adapters
{
    public class ListViewAdapter : ClassAdapter
    {
        private readonly ApplicationType _underlyingType;

        public ListViewAdapter(SimpleType type, Domain domain, ApplicationType underlyingType) : base(type, domain)
        {
            Log.Debug("Creating list view adapter for type {TypeName} with underlying type {UnderlyingTypeName}", type.Name, underlyingType.Name);
            _underlyingType = underlyingType;
            UnderlyingType = new ClientApiAdapter(underlyingType, domain);
        }

        public List<DisplayFieldAdapter> ListFields
        {
            get
            {
                var displayFields = new List<DisplayFieldAdapter>();
                foreach (var field in _type.Fields)
                {
                    var underlyingField = _underlyingType.Fields.FirstOrDefault(f => f.Name == field.Name);

                    if (underlyingField == null)
                    {
                        if (field.Name.EndsWith(RelatedTypeField.DisplayFieldNameSuffix))
                        {
                            var nameWithoutDisplaySuffix = Util.RemoveSuffix(field.Name, RelatedTypeField.DisplayFieldNameSuffix);
                            var idField = _underlyingType.Fields.FirstOrDefault(f => f.Name == nameWithoutDisplaySuffix);
                            if (idField != null)
                            {
                                if (idField.IsCallerProvided) // not sure why we check to see if the Id field is user editable
                                {
                                    displayFields.Add(new DisplayFieldAdapter(field, Util.HumanizeName(idField), idField, _domain));
                                }
                            }
                            else
                            {
                                // this is a weird one - there is a 'display' field e.g. customer_id_display but no customer_id field
                                displayFields.Add(new DisplayFieldAdapter(field, Util.HumanizeName(nameWithoutDisplaySuffix)));
                            }
                        }
                        else
                        {
                            displayFields.Add(new DisplayFieldAdapter(field, Util.HumanizeName(field)));
                        }
                    }
                    else
                    {
                        if (_type.Fields.Any(f => f.Name == field.Name + RelatedTypeField.DisplayFieldNameSuffix))
                        {
                            // this field is a 'data' field for which there is an equivalent display field, skip it
                            continue;
                        }

                        if (underlyingField.IsCallerProvided && !underlyingField.IsFile)
                        {
                            displayFields.Add(new DisplayFieldAdapter(field, Util.HumanizeName(field)));
                        }
                    }
                }

                return displayFields.OrderBy(f => f.Rank).ToList();
            }
        }

        public ClientApiAdapter UnderlyingType { get; }

        // the operations exposed to the template by the ListViewAdapter are only ones that don't take any parameters
        public override List<OperationAdapter> Operations
        {
            get { return _domain.Operations.Where(op => op.Returns.SimpleReturnType == _type && !op.SingleResult && op.Parameters.Count(p => !p.IsSecurityUser) == 0).Select(o => new OperationAdapter(o, base._domain, _underlyingType)).ToList(); }
        }

        public bool HasOperations => Operations.Count > 0;

        public OperationAdapter PrimaryOperation
        {
            get
            {
                if (Operations.Count == 0)
                {
                    return null;
                }

                if (Operations.Count == 1)
                {
                    return Operations.First();
                }

                var nameSuffix =
                    _domain.NamingConvention.CreateNameFromFragments(DbFunctionGenerator.SelectAllForDisplayFunctionName
                        .ToList());
                var selectAll = Operations.FirstOrDefault(op => op.Name.ToString().EndsWith(nameSuffix));

                if (selectAll != null)
                {
                    return selectAll;
                }

                return Operations.First();
            }
        }

        public string ListStateTypeName => $"ResultData<{Name.CSharpName}[]>";
    }
}
