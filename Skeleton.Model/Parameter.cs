#nullable enable
using System;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Skeleton.Model
{
    [DebuggerDisplay("Name: {Name} {ClrType}")]
    public class Parameter : TypedValue
    {
        public Parameter(Domain domain, Operation operation, string name, Type clrType, string providerTypeName) : base(domain)
        {
            Operation = operation;
            Name = name;
            ClrType = clrType;
            ProviderTypeName = providerTypeName;
            Attributes = new JObject();
        }
        
        public Operation Operation { get; }
        
        public virtual Field? RelatedTypeField { get; set; }

        public void UpdateFromField(Field field)
        {
            RelatedTypeField = field;
            if (ClrType != field.ClrType)
            {
                ClrType = field.ClrType;
            }
        }

        public bool IsNullable
        {
            get
            {
                if (ClrType == null)
                {
                    Log.Error("Parameter {ParameterName} does not have a CLR type", Name);
                }
                return ClrTypeIsNullable(ClrType!);      
            }
        } 

        public virtual int? Size => RelatedTypeField?.Size;

        public override bool IsLargeTextContent => RelatedTypeField?.IsLargeTextContent == true;

        public override bool IsHtml => RelatedTypeField?.IsHtml == true;
        
        public override bool IsFile => RelatedTypeField?.IsFile == true;

        public override bool IsColor => RelatedTypeField?.IsColor == true;

        public override bool IsRating => this.RelatedTypeField?.IsRating == true;

        public bool IsSecurityUser
        {
            get
            {
                return _domain.UserIdentity?.ClrType != null && _domain.NamingConvention.IsSecurityUserIdParameterName(Name)  && (ClrType == _domain.UserIdentity?.ClrType || (!ClrTypeIsNullable(_domain.UserIdentity!.ClrType) && ClrType == MakeClrTypeNullable(_domain.UserIdentity!.ClrType)));
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

        public bool IsCustomType => ClrType == typeof(ResultType);

        public bool IsJson => ProviderTypeName == "jsonb";

        public void MakeClrTypeNullable()
        {
            ClrType = typeof(Nullable<>).MakeGenericType(ClrType);
        }
    }
}