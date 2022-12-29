using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;
using Skeleton.Model.Operations;
using Skeleton.Templating.DatabaseFunctions.Adapters.Fields;

namespace Skeleton.Templating.DatabaseFunctions.Adapters
{
    public class SelectPagedForDisplayDbTypeAdapter : SelectForDisplayDbTypeAdapter
    {
        public SelectPagedForDisplayDbTypeAdapter(ApplicationType applicationType, Domain domain) : base(applicationType, new []{"select", "paged", "for", "display"}, domain)
        {
        }
        
        public override List<IPseudoField> SelectInputFields
        {
            get
            {
                var fields = base.SelectInputFields;
                fields.Add(PageSizeField);
                fields.Add(PageNumberField);
                fields.Add(SortParameter);
                fields.Add(SortDescendingField);
                return fields;
            }
        }

        public IPseudoField PageSizeField => new PageSizeField(_applicationType.Domain.NamingConvention);

        public IPseudoField PageNumberField => new PageNumberField(_applicationType.Domain.NamingConvention);

        public IPseudoField SortParameter => new SortParameter(_applicationType.Domain.NamingConvention);

        public IPseudoField SortDescendingField => new SortDescendingField(Domain.TypeProvider, Domain.NamingConvention);

        public List<ISortField> DisplayAllSortFields => DisplayAllFields
            .Select(f => _applicationType.Domain.TypeProvider.CreateSortField(f, this)).ToList();
    }
}