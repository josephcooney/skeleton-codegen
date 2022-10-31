using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Skeleton.Model;
using Skeleton.Templating.Classes.Adapters;

namespace Skeleton.Templating.Classes.WebApi
{
    public class AttachmentControllerAdapter : ControllerAdapter
    {
        public AttachmentControllerAdapter(ApplicationType type, Domain domain) : base(type, domain)
        {
        }

        public FieldAdapter AttachmentFileField
        {
            get { return _type.Fields.Where(f => f.IsFile && !f.IsAttachmentThumbnail).Select(f => new FieldAdapter(f)).SingleOrDefault(); }
        }
        
        public FieldAdapter AttachmentFieldContentType
        {
            get { return _type.Fields.Where(f => f.IsAttachmentContentType).Select(f => new FieldAdapter(f)).SingleOrDefault(); }
        }

        public bool AllowAnonGet
        {
            get
            {
                var getOp = this.Operations.FirstOrDefault(o => o.IsSelectById);

                if (getOp != null)
                {
                    return getOp.AllowAnon;
                }

                return false;
            }
        }

        public bool HasThumbnail => _type.Fields.Any(f => f.IsAttachmentThumbnail);
    }
}
