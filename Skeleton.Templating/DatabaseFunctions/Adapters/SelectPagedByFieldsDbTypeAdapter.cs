﻿using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;
using Skeleton.Model.Operations;
using Skeleton.Templating.DatabaseFunctions.Adapters.Fields;

namespace Skeleton.Templating.DatabaseFunctions.Adapters
{
    public class SelectPagedByFieldsDbTypeAdapter : SelectByFieldsDbTypeAdapter
    {
        public SelectPagedByFieldsDbTypeAdapter(ApplicationType applicationType, string[] operation, List<Field> selectFields, OperationType operationType, Domain domain) : base(applicationType, operation, selectFields, operationType, domain, false)
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
        
        public IPseudoField PageSizeField => new PageSizeField(Domain.NamingConvention);

        public IPseudoField PageNumberField => new PageNumberField(Domain.NamingConvention);

        public IPseudoField SortParameter => _applicationType.Domain.TypeProvider.CreateSortParameter(_applicationType.Domain.NamingConvention);

        public IPseudoField SortDescendingField => new SortDescendingParameter(Domain.TypeProvider, Domain.NamingConvention);

        public List<PseudoFieldWithIndex> SelectFieldsWithIndices
        {
            get
            {
                int index = 0;
                return SelectFields.Select(f => new PseudoFieldWithIndex() {Field = f, Index = ++index}).ToList();
            }
        }

        public int PageSizeParameterIndex => SelectFieldsWithIndices.Count + 1;
        
        public int OffsetIndex => PageSizeParameterIndex + 1;
        
        public List<ISortField> SortFields => _applicationType.Fields.Where(f => (!f.IsExcludedFromResults)).Select(a => _applicationType.Domain.TypeProvider.CreateSortField(a, this)).ToList();
    }

    public class PseudoFieldWithIndex
    {
        public IPseudoField Field { get; set; }
        public int Index { get; set; }
    }
}