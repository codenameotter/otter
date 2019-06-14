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
    public static class PostSelectedMenuItems
    {
        [FunctionName("SelectedMenuItems")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            string selectedMenu = req.Query["selectedMenu"];
            string fileUniqueID = req.Query["fileUniqueID"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            selectedMenu = selectedMenu ?? data?.selectedMenu;
            fileUniqueID = fileUniqueID ?? data?.filename;

            if (selectedMenu != null && fileUniqueID !=null )
            {
                try
                {
                    List<SqlParameter> sp = new List<SqlParameter>()
                    {
                       new SqlParameter() {ParameterName="@SelectedMenu", SqlDbType=System.Data.SqlDbType.NVarChar, Value=selectedMenu },
                       new SqlParameter() {ParameterName="@FileUniqueID", SqlDbType=System.Data.SqlDbType.BigInt, Value=fileUniqueID}
                    };
                    
                    var records = int.Parse(await RunStoredProcedure(sp, "Process.usp_SelectedMenuItems"));
                    return records > -1 ? (ActionResult)new OkObjectResult(records):(ActionResult) new BadRequestObjectResult(records);
                }
                catch (Exception e)
                {
                    return new BadRequestObjectResult("Request generated an exception ->" + e.Message);

                }
            }
            else
            {
                return new BadRequestObjectResult("Please pass a valid uniqueID and menu mapping to be selected as parameters on the query string or in the request body");
            }
           
        }
        public static async Task<string> RunStoredProcedure(List<SqlParameter> paramList, string storedProcedureName)
        {

            //var str = Environment.GetEnvironmentVariable("sqldb_connection");
            var str = "Server=tcp:contata.database.windows.net,1433;Initial Catalog=OTTER;Persist Security Info=False;User ID=contata.admin;Password=C@ntata123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;";
            string result = string.Empty;
            try
            {

                using (SqlConnection conn = new SqlConnection(str))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(storedProcedureName, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddRange(paramList.ToArray());
                        SqlDataReader reader = await cmd.ExecuteReaderAsync();
                        while (reader.Read())
                        {

                            result = reader[0].ToString();
                        }
                        reader.Close();
                    }

                }
            }
            catch
            {
                result = "-1";
            }
            return result;
        }

    }
}
