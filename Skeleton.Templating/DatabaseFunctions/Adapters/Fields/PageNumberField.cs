﻿using System;
using System.Collections.Generic;
using Skeleton.Model;
using Skeleton.Model.NamingConventions;

namespace Skeleton.Templating.DatabaseFunctions.Adapters.Fields
{
    public class PageNumberField : IPseudoField
    {
        private readonly INamingConvention _namingConvention;

        public PageNumberField(INamingConvention namingConvention)
        {
            _namingConvention = namingConvention;
        }
        
        public string Name => GetNameForNamingConvention(_namingConvention);
        public string ParentAlias => null;
        public string ProviderTypeName => "integer";
        public bool HasDisplayName => false;
        public string DisplayName => null;
        public int Order => 0;
        public bool IsUuid => false;
        public bool Add => false;
        public bool Edit => false;
        public bool IsUserEditable => true;
        public bool IsKey => false;
        public bool IsInt => true;
        public bool HasSize => false;
        public int? Size => null;
        public Type ClrType => typeof(int);
        public bool IsGenerated => false;
        public bool IsRequired => false;
        
        public static string GetNameForNamingConvention(INamingConvention namingConvention)
        {
            return namingConvention.CreateNameFromFragments(new List<string> { "page", "num" });
        }
    }
}