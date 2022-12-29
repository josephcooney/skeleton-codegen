using System;
using Skeleton.Model;

namespace Skeleton.Templating.DatabaseFunctions.Adapters
{
    public class UserIdField : IPseudoField
    {
        private readonly Domain _domain;

        public UserIdField(Domain domain)
        {
            _domain = domain;
            Name = domain.NamingConvention.SecurityUserIdParameterName;
            ProviderTypeName = domain.UserIdentity.ProviderTypeName;
            HasDisplayName = false;
            Order = 0;
        }

        public string Name { get; }
        public string ParentAlias { get; }
        public string ProviderTypeName { get; }
        public bool HasDisplayName { get; }
        public string DisplayName { get; }
        public int Order { get; }
        public bool IsUuid => false;
        public bool Add => true;
        public bool Edit => true;
        public bool IsUserEditable => false;

        public bool IsKey => false;
        public bool IsInt => _domain.UserIdentity.IsInt;
        public bool HasSize => _domain.UserIdentity.Size != null;
        public int? Size => _domain.UserIdentity.Size;
        public Type ClrType => _domain.UserIdentity.ClrType;
        public bool IsGenerated => false;
        public bool IsRequired => false; // TODO - not sure if this is right
    }
}