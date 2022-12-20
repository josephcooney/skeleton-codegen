using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;
using Skeleton.Model.Operations;
using Skeleton.Templating.DatabaseFunctions.Adapters.Fields;

namespace Skeleton.Templating.DatabaseFunctions.Adapters
{
    public class PagedDbTypeAdapter : DbTypeAdapter
    {
        public PagedDbTypeAdapter(ApplicationType applicationType, OperationType operationType, Domain domain) : base(applicationType, new []{"select", "paged"}, operationType, domain)
        {
        }

        public override List<IPseudoField> SelectInputFields
        {
            get
            {
                var fields = base.SelectInputFields;
                fields.Add(PageSizeField);
                fields.Add(PageNumberField);
                fields.Add(SortField);
                fields.Add(SortDescendingField);
                return fields;
            }
        }

        public IPseudoField PageSizeField => new PageSizeField(Domain.NamingConvention);

        public IPseudoField PageNumberField => new PageNumberField(Domain.NamingConvention);

        public IPseudoField SortField => new SortField(Domain.NamingConvention);

        public IPseudoField SortDescendingField => new SortDescendingField(Domain.TypeProvider, Domain.NamingConvention);
        
        public List<ISortField> SortFields => _applicationType.Fields.Where(f => (!f.IsExcludedFromResults)).Select(a => _applicationType.Domain.TypeProvider.CreateSortField(a, this)).ToList();
    }
}