using System;
using Skeleton.Model;

namespace Skeleton.Templating.DatabaseFunctions.Adapters.Fields
{
    public class SortDescendingField : IPseudoField
    {
        private readonly ITypeProvider _typeProvider;

        public SortDescendingField(ITypeProvider typeProvider)
        {
            _typeProvider = typeProvider;
        }
        
        public string Name => "sort_descending";
        public string ParentAlias => null;
        public string ProviderTypeName => _typeProvider.GetProviderTypeForClrType(typeof(bool));
        public bool HasDisplayName => false;
        public string DisplayName  => null;
        public int Order => 0;
        public bool IsUuid => false;
        public bool Add => false;
        public bool Edit => false;
        public bool IsUserEditable => true;
        public bool IsIdentity => false;
        public bool IsInt => false;
        public bool HasSize => false;
        public int? Size  => null;
        public Type ClrType => typeof(bool);
        public bool IsGenerated => false;
        public bool IsRequired => false;
    }
}