using Skeleton.Model;
using Newtonsoft.Json.Linq;

namespace Skeleton.Templating
{
    public class SecurityUtil
    {
        public static bool HasViewRights(dynamic securityRole)
        {
            return HasListRights(securityRole) || HasReadRights(securityRole);
        }

        public static bool HasListRights(dynamic securityRole)
        {
            var result = securityRole != null &&
                         !Contains(securityRole, SecurityRights.None) &&
                          Contains(securityRole, SecurityRights.List);

            return result;
        }

        public static bool HasReadRights(dynamic securityRole)
        {
            var result = securityRole != null &&
                         !Contains(securityRole, SecurityRights.None) &&
                         (Contains(securityRole, SecurityRights.Read) ||
                          Contains(securityRole, SecurityRights.ReadAll));

            return result;
        }

        public static bool HasAddRights(dynamic securityRole)
        {
            var result = securityRole != null &&
                         !Contains(securityRole, SecurityRights.None) &&
                         Contains(securityRole, SecurityRights.Add);

            return result;
        }

        public static bool HasEditRights(dynamic securityRole)
        {
            var result = securityRole != null &&
                         !Contains(securityRole, SecurityRights.None) &&
                         Contains(securityRole, SecurityRights.Edit);

            return result;
        }

        public static bool HasDeleteRights(dynamic securityRole)
        {
            var result = securityRole != null &&
                         !Contains(securityRole, SecurityRights.None) &&
                         Contains(securityRole, SecurityRights.Delete);

            return result;
        }

        public static bool Contains(dynamic securityRole, string item)
        {
            // this feels like a really painful way of determining this. I would love to know a better way.
            var array = ((Newtonsoft.Json.Linq.JArray)securityRole);
            foreach (var val in array)
            {
                if (val.Value<string>() == item)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
