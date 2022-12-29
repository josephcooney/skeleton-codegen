using System.Collections.Generic;
using Skeleton.Model;
using Skeleton.Templating.DatabaseFunctions.Adapters.Fields;

namespace Skeleton.Templating.DatabaseFunctions.Adapters
{
    public class SelectPagedForDisplayViaLinkDbTypeAdapter : SelectForDisplayViaLinkDbTypeAdapter
    {
        public SelectPagedForDisplayViaLinkDbTypeAdapter(ApplicationType applicationType, string[] operation, ApplicationType linkingType, Domain domain) : base(applicationType, operation, linkingType, domain)
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

        public int LinkFieldParameterIndex = 1;

        public int PageSizeParameterIndex => LinkFieldParameterIndex + 1;
        
        public int OffsetIndex => PageSizeParameterIndex + 1;
    }
}