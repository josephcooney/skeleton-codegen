using System;
using Skeleton.Model;

namespace Skeleton.Templating.DatabaseFunctions.Adapters.Fields
{
    public class PageSizeField : IPseudoField
    {
        public PageSizeField()
        {
            
        }
        
        public string Name => "page_size";
        public string ParentAlias => null; // TODO - maybe should be operation name?
        public string ProviderTypeName => "integer"; // TODO - could be more db-agnostic?
        public bool HasDisplayName => false;
        public string DisplayName => null;
        public int Order => 0;
        public bool IsUuid => false;
        public bool Add => false;
        public bool Edit => false;
        public bool IsUserEditable => true;
        public bool IsIdentity => false;
        public bool IsInt => true;
        public bool HasSize => false;
        public int? Size => null;
        public Type ClrType => typeof(string);
        public bool IsGenerated => false;
        public bool IsRequired => false;
    }
}