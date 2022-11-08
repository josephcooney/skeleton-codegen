using System;
using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;
using Skeleton.Templating.Classes.WebApi;

namespace Skeleton.Templating.Classes.Adapters
{
    public class ControllerAdapter : ClassAdapter
    {
        public ControllerAdapter(ApplicationType type, Domain domain) : base(type, domain)
        {
        }

        public FieldAdapter CreatedByField
        {
            get
            {
                var field = _type.Fields.FirstOrDefault(f => f.IsTrackingUser && _type.Domain.NamingConvention.IsCreatedByFieldName(f.Name));
                if (field != null)
                {
                    return new FieldAdapter(field);
                }
                return null;
            }
        }

        public FieldAdapter ModifiedByField
        {
            get
            {
                var field = _type.Fields.FirstOrDefault(f => f.IsTrackingUser && _type.Domain.NamingConvention.IsModifiedByFieldName(f.Name));
                if (field != null)
                {
                    return new FieldAdapter(field);
                }
                return null;
            }
        }

        public List<FieldAdapter> InsertFields
        {
            get
            {
                var fields = _type.Fields.Where(f => f.IsCallerProvided).Select(f => new FieldAdapter(f)).ToList();
                if (CreatedByField != null)
                {
                    fields.Add(CreatedByField);
                }
                return fields.OrderBy(f => f.Order).ToList();
            }
        }

        public List<FieldAdapter> UpdateFields
        {
            get
            {
                var fields = _type.Fields.Where(f => f.IsCallerProvided).Union(_type.Fields.Where(f2 => f2.IsKey)).Select(f => new FieldAdapter(f)).ToList();
                if (ModifiedByField != null)
                {
                    fields.Add(ModifiedByField);
                }
                return fields.OrderBy(f => f.Order).ToList();
            }
        }

        public bool HasSecurityPrincipalType => SecurityPrincipalType != null;

        public ApplicationType SecurityPrincipalType
        {
            get
            {
                var userTracking = _type.Fields.FirstOrDefault(f => f.IsTrackingUser);
                if (userTracking != null)
                {
                    return userTracking.ReferencesType;
                }

                return null;
            }
        }

        public override List<OperationAdapter> Operations => base.Operations.Where(o => o.GenerateApi).ToList();
        
        public bool GenerateConstructor
        {
            get { return _type.Attributes?.apiConstructor != "none"; }
        }
        
        public string ControllerBaseClassName
        {
            get
            {
                var customControllerBase = _type?.Attributes?.apiControllerBaseClass?.ToString();
                if (!string.IsNullOrEmpty(customControllerBase))
                {
                    return customControllerBase;
                }

                return "ControllerBase";
            }
        }
    }

    public class FieldAdapter
    {
        private readonly Field _field;

        public FieldAdapter(Field field)
        {
            _field = field;
        }

        public virtual string Source
        {
            get
            {
                if (_field.IsTrackingUser)
                {
                    return "userId";
                }

                return "model." + Util.CSharpNameFromName(_field.Name);
            }
        }

        public int Order => _field.Order;

        public string Name => _field.Name;
    }
}