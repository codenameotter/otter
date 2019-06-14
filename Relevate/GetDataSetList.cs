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
    public static class GetDataSetList
    {
        [FunctionName("DataSetList")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string siteId = req.Query["siteId"];
            string spaceId = req.Query["spaceId"];
            string actorID = req.Query["actorID"];

            

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            siteId = siteId ?? data?.siteId;
            spaceId = spaceId ?? data?.spaceId;
            actorID = actorID ?? data?.actorID;

            if (siteId != null && spaceId != null && actorID != null)
            {
                try
                {
                    List<SqlParameter> sp = new List<SqlParameter>()
                    {
                       new SqlParameter() {ParameterName="@SiteId", SqlDbType=System.Data.SqlDbType.BigInt, Value=siteId },
                       new SqlParameter() {ParameterName="@SpaceId", SqlDbType=System.Data.SqlDbType.BigInt, Value=spaceId},
                       new SqlParameter() {ParameterName="@ActorID", SqlDbType=System.Data.SqlDbType.BigInt, Value=actorID}
                    };

                    string records = await RunStoredProcedure(sp, "Process.usp_DataSetList");
                    return (ActionResult)new OkObjectResult(records) ;
                }
                catch (Exception e)
                {
                    return new BadRequestObjectResult("Request generated an exception ->" + e.Message);

                }
            }
            else
            {
                return new BadRequestObjectResult("Please pass a valid siteID, spaceID and actorID as parameters on the query string or in the request body");
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
                    storedProcedureName = storedProcedureName ?? "Process.GetTopNRows";

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
