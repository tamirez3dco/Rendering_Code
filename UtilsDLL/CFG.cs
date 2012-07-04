using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace UtilsDLL
{
    public class CFG
    {
        public static Dictionary<String, Object> Cfg_dict;

        public static bool Turn_String_Into_CFG(String[] user_args)
        {
            Cfg_dict = new Dictionary<string, object>();
            if (user_args.Length > 0)
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                Cfg_dict = serializer.DeserializeObject(user_args[0]) as Dictionary<string, object>;
            }

            String dns;
            if (!Network_Utils.Get_DNS(out dns))
            {
                Console.WriteLine("Failed to Get_DNS()");
                return false;
            }

            Cfg_dict["is_AWS"] = dns.StartsWith("ip-");
            return true;
        }
    }
}
