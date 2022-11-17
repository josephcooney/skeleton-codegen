using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Skeleton.Model.NamingConventions;

namespace Skeleton.Model
{
    [DebuggerDisplay("Operation: {Namespace} {Name} {ProviderType}")]
    public class Operation
    {
        private readonly INamingConvention _namingConvention;
        private string _nameInternal;
        
        public Operation(string name, INamingConvention namingConvention)
        {
            _nameInternal = name;
            _namingConvention = namingConvention;
            Parameters = new List<Parameter>();
        }

        public string Namespace { get; set; }
        
        public Name Name
        {
            get
            {
                string fullName = Attributes?.fullName?.ToString();
                if (!string.IsNullOrEmpty(fullName))
                {
                    return new Name(fullName, _namingConvention, () => RelatedType );
                }

                return new Name(_nameInternal, _namingConvention, () => RelatedType);
            }
        }

        public string ProviderType { get; set; } // e.g. FUNCTION, PROCEDURE etc
        
        public dynamic Attributes { get; set; }

        public List<Parameter> Parameters { get; }

        public List<Parameter> UserProvidedParameters
        {
            get
            {
                if (ChangesOrCreatesData)
                {
                    return Parameters.Where(p =>
                        p.RelatedTypeField?.IsTrackingUser != true &&
                        p.RelatedTypeField?.IsAttachmentContentType != true &&
                        p.RelatedTypeField?.IsAttachmentThumbnail != true &&
                        !p.IsSecurityUser).ToList();
                }
                else
                {
                    return Parameters.Where(p =>
                        p.RelatedTypeField?.IsAttachmentThumbnail != true &&
                        !p.IsSecurityUser).ToList();
                }
            }
        }

        public bool ChangesOrCreatesData => ChangesData || CreatesNew;
        
        public OperationReturn Returns { get; set; }

        public bool Ignore => Attributes?.ignore == true;
        public bool IsGenerated => Attributes?.generated == true;
        public bool ChangesData => Attributes?.changesData == true;
        public bool CreatesNew => Attributes?.createsNew == true;

        public ApplicationType RelatedType { get; set; } // this is set from the Attributes.applicationType - looked up by name
        
        public string FriendlyName
        {
            get
            {
                var friendly = Attributes?.friendlyName;
                if (friendly == null)
                {
                    return Name.BareName.ToString();
                }

                return friendly.ToString();
            }
        }
        
        public string CustomReturnTypeName => Attributes?.returnTypeName?.ToString();

        public bool GenerateUI => !Ignore && !(Attributes?.ui == false);

        public bool GenerateApi => !Ignore && !(Attributes?.api == false);

        public bool IsSelectById
        {
            get
            {
                return ReturnsRelatedType && Parameters.Count == 2 && Parameters.All(p =>
                    p.IsSecurityUser || (p.RelatedTypeField != null && p.RelatedTypeField.IsKey));
            }
        }

        private bool ReturnsRelatedType
        {
            get
            {
                return (Returns.ReturnType == ReturnType.ApplicationType && Returns.SimpleReturnType == RelatedType) ||
                       Returns.ReturnType == ReturnType.CustomType;
            }
        }

        public bool SingleResult => Attributes?.single_result == true || Returns.Multiple == false || Returns.ReturnType == ReturnType.Primitive;
        
        
    }

    public class OperationReturn
    {
        public ReturnType ReturnType { get; set; }

        public SimpleType SimpleReturnType { get; set; }

        public System.Type ClrReturnType { get; set; }
        
        public bool Multiple { get; set; }
    }

    public enum ReturnType
    {
        None,
        Primitive,
        ApplicationType,
        CustomType
    }
}
