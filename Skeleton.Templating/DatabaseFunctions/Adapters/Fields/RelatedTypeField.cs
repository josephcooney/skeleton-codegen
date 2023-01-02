using System;
using Skeleton.Model;
using Skeleton.Templating.DatabaseFunctions.Adapters.Fields;

namespace Skeleton.Templating.DatabaseFunctions.Adapters
{
    public class RelatedTypeField : IJoiningField
    {
        public const string DisplayFieldNameSuffix = "_display";

        private readonly Field _field;
        private readonly string _alias;
        private readonly string _relatedAlias;

        public RelatedTypeField(Field field, string alias, string relatedAlias)
        {
            _field = field;
            _alias = alias;
            _relatedAlias = relatedAlias;
        }

        public Field Field => _field;

        public string Name => _field.ReferencesType.DisplayField?.Name;

        public string ParentAlias => _relatedAlias;
        public string ProviderTypeName => _field.ReferencesType.DisplayField?.ProviderTypeName;

        public int? Size => _field.ReferencesType.DisplayField?.Size;
        public Type ClrType => _field.ClrType;
        public bool IsGenerated => false;
        public bool IsRequired => _field.IsRequired;

        public bool HasSize => Size != null;
        
        public bool HasDisplayName => true;
        public string DisplayName => _field.Name + DisplayFieldNameSuffix;
        public int Order => _field.Order;
        public bool IsUuid => _field.ClrType == typeof(Guid);
        public bool Add => _field.Add;
        public bool Edit => _field.Edit;
        public bool IsUserEditable => _field.IsCallerProvided;
        public bool IsKey => _field.IsKey;
        public bool IsInt => _field.IsInt;

        public string PrimaryAlias => _alias;
    }
}