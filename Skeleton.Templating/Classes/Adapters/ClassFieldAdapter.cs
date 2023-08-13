using System.Linq;
using Skeleton.Model;

namespace Skeleton.Templating.Classes
{
    public class ClassFieldAdapter : Field
    {
        public ClassFieldAdapter(Field field) : base(field.Type)
        {
            Attributes = field.Attributes;
            ClrType = field.ClrType;
            Name = field.Name;
            Order = field.Order;
            Size = field.Size;
            IsRequired = field.IsRequired;
            ReferencesType = field.ReferencesType;
            ReferencesTypeField = field.ReferencesTypeField;
            IsKey = field.IsKey;
            ProviderTypeName = field.ProviderTypeName;
        }
        
        public bool IsReferenceField => ReferencesType != null;

    }

    public class DisplayFieldAdapter : ClassFieldAdapter
    {
        private readonly string _displayName;
        private readonly Field _linkingIdField;

        public DisplayFieldAdapter(Field field, string displayName) : base(field)
        {
            _displayName = displayName;
        }

        public DisplayFieldAdapter(Field field, string displayName, Field linkingIdField, Domain domain) : base(field)
        {
            _displayName = displayName;
            _linkingIdField = linkingIdField;
        }

        public new string DisplayName => _displayName;

        public bool IsLinkingField => _linkingIdField != null;

        public bool IsLinkingFieldWithDetails => IsLinkingField && RelatedType.HasDetails;

        public bool IsLinkingFieldToAttachmentWithThumbnail =>
            IsLinkingField && RelatedType.IsAttachment && RelatedType.Fields.Any(f => f.IsAttachmentThumbnail);

        public ClassFieldAdapter LinkingField => _linkingIdField != null ? new ClassFieldAdapter(_linkingIdField) : null;

        public ClassAdapter RelatedType => _linkingIdField != null ? new ClassAdapter(_linkingIdField.ReferencesType, _domain) : null; 
    }
}