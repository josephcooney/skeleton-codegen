using System;
using System.Collections.Generic;
using Skeleton.Model;
using Skeleton.Model.NamingConventions;

namespace Skeleton.Templating.DatabaseFunctions.Adapters.Fields
{
    public class SortDescendingField : IPseudoField
    {
        private readonly ITypeProvider _typeProvider;
        private readonly INamingConvention _namingConvention;

        public SortDescendingField(ITypeProvider typeProvider, INamingConvention namingConvention)
        {
            _typeProvider = typeProvider;
            _namingConvention = namingConvention;
        }
        
        public string Name => GetNameForNamingConvention(_namingConvention);
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
        
        public static string GetNameForNamingConvention(INamingConvention namingConvention)
        {
            return namingConvention.CreateNameFromFragments(new List<string> { "sort", "descending" });
        }
    }
}