namespace Skeleton.Model
{
    public class SecurityRoles
    {
        private readonly Settings _settings;

        public SecurityRoles(Settings settings)
        {
            _settings = settings;
        }
        
        public string Anonymous => "anon";
        public string User => "user";
        public string Admin => _settings.AdminRoleName ?? "admin";
    }

    public class SecurityRights
    {
        public const string Add = "add";
        public const string Edit = "edit";
        public const string Delete = "delete";
        public const string Read = "read";
        public const string ReadAll = "read-all";
        public const string EditAll = "edit-all";
        public const string List = "list";
        public const string None = "none";
    }
}
