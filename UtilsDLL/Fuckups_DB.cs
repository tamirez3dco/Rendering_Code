using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace UtilsDLL
{
    public class Fuckups_DB
    {
        private static System.Data.SqlClient.SqlConnection con = new System.Data.SqlClient.SqlConnection(@"Data Source=.\SQLEXPRESS;AttachDbFilename=C:\inetpub\ftproot\Rendering_Code\Fuckups_Dataset\fuckups_ids.mdf;Integrated Security=True;Connect Timeout=30;User Instance=True");

        public static void Open_Connection()
        {
            con.Open();
        }

        public static void Clear_DB()
        {

            using (var command = new SqlCommand("DeleteAllRows", con)
            {
                CommandType = CommandType.StoredProcedure
            })
            {
                command.ExecuteNonQuery();
            }
        }

        public static int Get_Fuckups(String item_id)
        {
            using (var command = new SqlCommand("Get_Fuckups_By_ID", con)
            {
                CommandType = CommandType.StoredProcedure
            })
            {
                SqlParameter param = new SqlParameter("@id", SqlDbType.NVarChar);
                param.Direction = ParameterDirection.Input;
                param.Value = item_id;
                command.Parameters.Add(param);
                SqlParameter ret_param = new SqlParameter("@stam", SqlDbType.NVarChar);
                ret_param.Direction = ParameterDirection.ReturnValue;
                command.Parameters.Add(ret_param);
                command.ExecuteNonQuery();
                return (int)ret_param.Value;
            }
        }

        public static void Add_Fuckup(String item_id)
        {
            using (var command = new SqlCommand("Add_Fuckup", con)
            {
                CommandType = CommandType.StoredProcedure
            })
            {
                SqlParameter param = new SqlParameter("@id", SqlDbType.NVarChar);
                param.Direction = ParameterDirection.Input;
                param.Value = item_id;
                command.Parameters.Add(param);
                command.ExecuteNonQuery();
                return;
            }
        }
    }
}
