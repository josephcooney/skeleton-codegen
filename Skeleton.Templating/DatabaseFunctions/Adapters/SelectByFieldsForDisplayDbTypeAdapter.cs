using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;

namespace Skeleton.Templating.DatabaseFunctions.Adapters
{
    public class SelectByFieldsForDisplayDbTypeAdapter : SelectForDisplayDbTypeAdapter
    {
        private readonly List<Field> _selectFields;

        public SelectByFieldsForDisplayDbTypeAdapter(ApplicationType applicationType, string operation, List<Field> selectFields, Domain domain) : base(applicationType, operation, domain)
        {
            _selectFields = selectFields;
        }

        public override List<IPseudoField> SelectInputFields
        {
            get
            {
                var fields = new List<IPseudoField>();
                fields.AddRange(this.SelectFields);
                if (UserIdField != null)
                {
                    fields.Add(UserIdField);
                }
                return fields.OrderBy(f => f.Order).ToList();
            }
        }

        public List<IPseudoField> SelectFields
        {
            get
            {
                return _selectFields.Select(a => _applicationType.Domain.TypeProvider.CreateFieldAdapter(a, this) as IPseudoField).ToList();
            }
        }

        public override bool FilterListOperation
        {
            get
            {
                if (IsPrimaryKey)
                {
                    // it's not really a "list all" operation if it is a lookup by primary key
                    return false;
                }

                return base.FilterListOperation;
            }
        }

        public bool IsPrimaryKey => _selectFields.All(f => f.IsKey);
        
        public bool ReturnsSingle => IsPrimaryKey;
    }
}
