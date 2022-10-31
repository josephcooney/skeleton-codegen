using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;
using Skeleton.Templating.Classes;
using Skeleton.Templating.DatabaseFunctions.Adapters;

namespace Skeleton.Templating.MvcViews
{
    public class ViewAdapter : ClassAdapter
    {
        public ViewAdapter(ApplicationType type, Domain domain) : base(type, domain)
        {
        }

        public string HumanizedNamePlural
        {
            get
            {
                return  Util.Pluralize(Util.HumanizeName(base._type.Name));
            }
        }

        public ApplicationType Type => (ApplicationType)_type;
    }

    public class IndexViewAdapter : ViewAdapter
    {
        private readonly ResultType _displayType;
        private List<DisplayFieldAdapter> _displayFields;

        public IndexViewAdapter(ApplicationType type, ResultType displayType, Domain domain) : base(type, domain)
        {
            _displayType = displayType;
        }

        public List<DisplayFieldAdapter> DisplayFields
        {
            get
            {
                if (_displayFields == null)
                {
                    _displayFields = new List<DisplayFieldAdapter>();
                    foreach (var f in _type.Fields.Where(f => !f.IsExcludedFromResults))
                    {

                        var displayField = _displayType?.Fields.FirstOrDefault(fld => fld.Name == f.Name + RelatedTypeField.DisplayFieldNameSuffix);

                        if (f.ReferencesType != null && displayField != null)
                        {
                            _displayFields.Add(new DisplayFieldAdapter(displayField, Util.HumanizeName(f)));
                        }
                        else
                        {
                            _displayFields.Add(new DisplayFieldAdapter(f, Util.HumanizeName(f)));
                        }
                    }
                }

                return _displayFields;
            }
        }

        public string DisplayTypeName => _displayType?.Name ?? _type.Name;
    }
}
