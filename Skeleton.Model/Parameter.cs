using System;
using Serilog;

namespace Skeleton.Model
{
    public class Parameter
    {
        protected readonly Domain _domain;

        public Parameter(Domain domain, Operation operation)
        {
            _domain = domain;
            Operation = operation;
        }
        
        public Operation Operation { get; }
        
        public dynamic Attributes { get; set; }

        public const string SecurityUserIdParamName = "security_user_id_param";

        public virtual string Name { get; set; }

        public virtual int Order { get; set; }

        public virtual Type ClrType { get; set; }

        public virtual string ProviderTypeName { get; set; }

        public virtual Field RelatedTypeField { get; set; }

        public bool IsNullable
        {
            get
            {
                if (ClrType == null)
                {
                    Log.Error("Parameter {ParameterName} does not have a CLR type", Name);
                }
                return ClrTypeIsNullable(ClrType);      
            }
        } 

        public virtual int? Size => RelatedTypeField?.Size;

        public bool IsDateTime => ClrType == typeof(DateTime) || ClrType == typeof(DateTime?);

        public bool IsBoolean => ClrType == typeof(bool) || ClrType == typeof(bool?);

        public bool IsLargeTextContent
        {
            get
            {
                return RelatedTypeField?.IsLargeTextContent == true;
            }
        }

        public bool IsFile => this.RelatedTypeField?.IsFile == true;

        public bool IsRating => this.RelatedTypeField?.IsRating == true;

        public bool IsSecurityUser
        {
            get
            {
                return Name == SecurityUserIdParamName && (ClrType == _domain.UserIdentity.ClrType || (!ClrTypeIsNullable(_domain.UserIdentity.ClrType) && ClrType == MakeClrTypeNullable(_domain.UserIdentity.ClrType)));
            }
        }

        public bool IsCurrentUser => IsSecurityUser || (RelatedTypeField != null && RelatedTypeField.IsTrackingUser);
        
        private static bool ClrTypeIsNullable(Type type)
        {
            return !type.IsValueType || type.IsArray || Nullable.GetUnderlyingType(type) != null;
        }

        public static Type MakeClrTypeNullable(Type type)
        {
            return typeof(Nullable<>).MakeGenericType(type);
        }

        public virtual bool IsRequired => RelatedTypeField?.IsRequired ?? false;

        public bool IsJson => ProviderTypeName == "jsonb";
        public bool IsDate => IsDateTime && _domain.TypeProvider.IsDateOnly(ProviderTypeName);
    }
}