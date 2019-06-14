using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace Relevate
{
    public static class FieldsToMap
    {
        [FunctionName("FieldsToMap")]
        public static async Task<IActionResult> FieldsToMapForWorbix(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {

            try
            {
                List<SqlParameter> sp = new List<SqlParameter>()
                    {
                       new SqlParameter(){ParameterName="@Menu", SqlDbType=System.Data.SqlDbType.NVarChar,Value=""}
                    };
                var menu = await RunStoredProcedure(sp);
                return new OkObjectResult(menu);
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult("Request generated an exception ->" + e.Message);

            }

        }
        public static async Task<string> RunStoredProcedure(List<SqlParameter> paramList)
        {

            //var str = Environment.GetEnvironmentVariable("sqldb_connection");
            var str = "Server=tcp:contata.database.windows.net,1433;Initial Catalog=OTTER;Persist Security Info=False;User ID=contata.admin;Password=C@ntata123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;";
            try
            {
                using (SqlConnection conn = new SqlConnection(str))
                {
                    conn.Open();
                    //string storedProcedureName = Environment.GetEnvironmentVariable("register_client");
                    string storedProcedureName = "Process.usp_EntityHeaderMaster";

                    using (SqlCommand cmd = new SqlCommand(storedProcedureName, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddRange(paramList.ToArray());
                        SqlDataReader reader = await cmd.ExecuteReaderAsync();
                        //menu = paramList.Find((v) => { return v.Direction == System.Data.ParameterDirection.Output; }).Value.ToString();
                        while (reader.Read())
                        {

                            return reader[0].ToString();
                        }
                        reader.Close();
                    }

                }
            }
            catch
            {
                return "-1";
            }
            return "-1";
        }

    }
}
