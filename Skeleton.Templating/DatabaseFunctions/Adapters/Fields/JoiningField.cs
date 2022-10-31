using System;
using Skeleton.Model;

namespace Skeleton.Templating.DatabaseFunctions.Adapters.Fields
{
    public class JoiningField : IJoiningField
    {
        private readonly Field _field;
        private readonly string _alias;
        private readonly string _relatedAlias;

        public JoiningField(Field field, string alias, string relatedAlias)
        {
            _field = field;
            _alias = alias;
            _relatedAlias = relatedAlias;
        }

        public Field Field => _field;

        public string Name => _field.ReferencesType.DisplayField?.Name;

        public string ParentAlias => _relatedAlias;
        public string ProviderTypeName => _field.ReferencesType.DisplayField?.ProviderTypeName;
        public bool HasDisplayName => false;
        public string DisplayName => null;
        public int Order => _field.Order;
        public bool IsUuid => _field.ClrType == typeof(Guid);
        public bool Add => _field.Add;
        public bool Edit => _field.Edit;

        public bool IsUserEditable => _field.IsCallerProvided;
        public bool IsIdentity => _field.IsKey;
        public bool IsInt => _field.IsInt;
        public bool HasSize => _field.Size != null;
        public int? Size => _field.Size;
        public Type ClrType => _field.ClrType;
        public bool IsGenerated => false;
        public bool IsRequired => false;

        public string PrimaryAlias => _alias;
    }
}
