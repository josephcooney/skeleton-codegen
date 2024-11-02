using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Skeleton.Model
{
    [DebuggerDisplay("Application Type: {Namespace} {Name}")]
    public class ApplicationType : SimpleType
    {
        public ApplicationType(string name, string ns, Domain domain) : base(name, ns, domain)
        {
            Constraints = new List<Constraint>();
        }

        public bool IsReferenceData => Attributes?.type == "reference";

        public bool IsSearchable
        {
            get { return Fields.Any(f => f.IsSearch); }
        }

        public DeleteType DeleteType
        {
            get
            {
                if (Fields.Any(f => f.IsDelete))
                {
                    return DeleteType.Soft;
                }

                if (Attributes?.hardDelete == true)
                {
                    return DeleteType.Hard;
                }

                return DeleteType.None;
            }
        }

        public bool Ignore => Attributes?.ignore == true;

        public List<Constraint> Constraints { get; }

        public bool GenerateUI => GenerateApi && !(Attributes?.ui == false);

        public bool GenerateApi => !Ignore && !(Attributes?.api == false);

        public bool IsLink
        {
            get
            {
                return Fields.Count(f => f.HasReferenceType && f.IsCallerProvided) == 2 && !Fields.Any(f => f.IsCallerProvided && !f.HasReferenceType);
            }
        }

        public bool IsAttachment
        {
            get
            {
                if (Attributes?.isAttachment != null)
                {
                    return (bool) Attributes?.isAttachment;
                }

                return Name == "attachment" && Fields.Any(f => f.IsFile);
            }
        }

        public bool IsSecurityPrincipal => Attributes?.isSecurityPrincipal == true;

        public bool IsHelp => Attributes?.isHelp == true;

        public bool IsFieldLabels => Attributes.isFieldLabels = true; // field labels allow cms-like functionality for fields
        
        public int Rank // not as useful as I was hoping it would be
        {
            get { return Fields.Count(f => f.HasReferenceType && !f.ReferencesType.IsReferenceData && !f.ReferencesType.IsSecurityPrincipal && !f.ReferencesType.IsLink && !f.ReferencesType.Ignore && !f.ReferencesType.IsAttachment ); }
        }

        public bool Important => Attributes?.important == true;
        public bool Paged => Attributes?.paged == true;
    }

    public enum DeleteType
    {
        None,
        Hard,
        Soft
    }
}
