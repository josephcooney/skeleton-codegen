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
    public class ClientApiAdapter : ClassAdapter
    {
        private List<DisplayFieldAdapter> _displayFields;

        private ApplicationType _applicationType;
        
        public ClientApiAdapter(ApplicationType type, Domain domain) : base(type, domain)
        {
            _applicationType = type;
        }

        public string ClientApiTypeName => _type.Name.CSharpName + "ApiClient";

        public string ClientApiInterfaceName => "I" + ClientApiTypeName;
        
        public List<SimpleType> DistinctOperationReturnTypes
        {
            get
            {
                return this.Operations.Where(o => !o.IsSingular && !o.NoResult).Select(o => o.SimpleReturnType).Distinct().ToList();
            }
        }

        public List<SimpleType> DistinctOperationReturnTypesRelatedToParent
        {
            get
            {
                return Operations.Where(o => (!o.IsSingular && !o.NoResult) && (o.SimpleReturnType is ApplicationType && o.SimpleReturnType == _type || (o.SimpleReturnType is ResultType && 
                    ((ResultType)o.SimpleReturnType).RelatedType == _type))).Select(o => o.SimpleReturnType).Distinct().ToList();
            }
        }    
        
        public List<(SimpleType result, ApplicationType related)> DistinctOperationReturnTypesRelatedToOther
        {
            get
            {
                var items = Operations.Where(o => (!o.IsSingular && !o.NoResult) && (o.SimpleReturnType is ApplicationType && o.SimpleReturnType != _type || (o.SimpleReturnType is ResultType && 
                    ((ResultType)o.SimpleReturnType).RelatedType != _type))).Select(o => (o.SimpleReturnType, o.SimpleReturnType is ApplicationType ? (ApplicationType)o.SimpleReturnType : ((ResultType)o.SimpleReturnType).RelatedType)).Distinct().ToList();

                return items;
            }
        }    
        
        public virtual SimpleType SelectAllType 
        {
            get
            {
                var suffix =
                    _domain.NamingConvention.CreateNameFromFragments(DbFunctionGenerator.SelectAllForDisplayFunctionName
                        .ToList());
                var selectAllOp = Operations.FirstOrDefault(op => op.Name.ToString().EndsWith(suffix));
                // TODO - fall back to "select_all" operation?
                if (selectAllOp == null)
                {
                    Log.Warning("Unable to find 'select all' operation for type {TypeName}", _applicationType.Name);
                }
                return selectAllOp?.SimpleReturnType;
            }
        }

        public virtual SimpleType DetailType
        {
            get
            {
                var fragments = _applicationType.Name.Parts;
                fragments.AddRange(DbFunctionGenerator.SelectForDisplayFunctionName);
                var operationNameStart = _domain.NamingConvention.CreateNameFromFragments(fragments);
                var selectForDisplayOp = Operations.FirstOrDefault(op => op.Name.ToString().StartsWith(operationNameStart));
                if (selectForDisplayOp != null)
                {
                    return selectForDisplayOp.SimpleReturnType;
                }

                return _applicationType;
            }
        }

        public string HumanizedName => _type.Name.Humanized;

        public string HumanizedNamePlural => _type.Name.HumanizedPlural;
        
        public List<DisplayFieldAdapter> DisplayFields
        {
            get
            {
                if (_displayFields == null)
                {
                    _displayFields = new List<DisplayFieldAdapter>();
                    foreach (var f in _type.Fields.Where(f => !f.IsExcludedFromResults))
                    {

                        var displayField = SelectAllType?.Fields.FirstOrDefault(fld => fld.Name == f.Name + RelatedTypeField.DisplayFieldNameSuffix);

                        if (f.ReferencesType != null && displayField != null)
                        {
                            _displayFields.Add(new DisplayFieldAdapter(displayField, Util.HumanizeName(f), f, _domain));
                        }
                        else
                        {
                            if (!f.IsFile)
                            {
                                _displayFields.Add(new DisplayFieldAdapter(f, Util.HumanizeName(f)));
                            }
                        }
                    }

                    _displayFields = _displayFields.OrderBy(f => f.Rank).ToList();
                }

                return _displayFields;
            }
        }

        public List<DisplayFieldAdapter> UserEditableDisplayFields
        {
            get { return DisplayFields.Where(f => f.IsCallerProvided).ToList(); }
        }

        public bool HasDisplayField => _type.DisplayField != null;

        public List<Field> LinkingFields
        {
            get
            {
                var linkingFields = new List<Field>();
                foreach (var f in _type.Fields.Where(f => !f.IsExcludedFromResults && !f.IsTrackingUser))
                {
                    var displayField = SelectAllType?.Fields.FirstOrDefault(fld => fld.Name == f.Name + RelatedTypeField.DisplayFieldNameSuffix);
                    if (f.ReferencesType != null && displayField != null)
                    {
                        linkingFields.Add(displayField);
                    }
                }

                return linkingFields;
            }
        }

        public bool IsReferenceData => ((ApplicationType) _type).IsReferenceData;

        public bool IsAttachmentWithThumbnail => ((ApplicationType) _type).IsAttachment && _type.Fields.Any(f => f.IsAttachmentThumbnail);

        public override List<OperationAdapter> Operations => base.Operations.Where(o => o.GenerateApi).ToList();

        public List<ClientApiOperationAdapter> ApiOperations => Operations.Select(o => new ClientApiOperationAdapter(o.UnderlyingOperation, _domain, _applicationType)).ToList();
        
        public List<ClientApiOperationAdapter> OperationsWithUI => ApiOperations.Where(o => o.GenerateUI).ToList();

        public List<ClientApiOperationAdapter> OperationsWithUIThatChangeData => ApiOperations.Where(o => o.GenerateUI && o.ChangesData).ToList();

        public List<OperationAdapter> AddOperations => OperationsThatChangeData.Where(o => o.CreatesNew).ToList();

        public List<OperationAdapter> UpdateOperations => OperationsThatChangeData.Where(o => !o.CreatesNew && !o.IsDelete).ToList();

        public List<OperationAdapter> OperationsThatChangeData => this.Operations.Where(o => o.ChangesData).ToList();

        public bool NoAddUI => _type is ApplicationType && ((ApplicationType)_type).Attributes?.noAddUI == true;

        public bool NoEditUI => _type is ApplicationType && ((ApplicationType)_type).Attributes?.noEditUI == true;

        public bool HasSelectAllType => this.SelectAllType != this._type;
    }
}
