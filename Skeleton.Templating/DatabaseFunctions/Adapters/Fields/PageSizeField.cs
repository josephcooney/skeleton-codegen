﻿using System;
using System.Collections.Generic;
using Skeleton.Model;
using Skeleton.Model.NamingConventions;

namespace Skeleton.Templating.DatabaseFunctions.Adapters.Fields
{
    public class PageSizeField : IPseudoField
    {
        private readonly INamingConvention _namingConvention;

        public PageSizeField(INamingConvention namingConvention)
        {
            _namingConvention = namingConvention;
        }

        public string Name => GetNameForNamingConvention(_namingConvention);
        public string ParentAlias => null; // TODO - maybe should be operation name?
        public string ProviderTypeName => "integer"; // TODO - could be more db-agnostic?
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
        public Type ClrType => typeof(string);
        public bool IsGenerated => false;
        public bool IsRequired => false;

        public static string GetNameForNamingConvention(INamingConvention namingConvention)
        {
            return namingConvention.CreateNameFromFragments(new List<string> { "page", "size" });
        }
    }
}