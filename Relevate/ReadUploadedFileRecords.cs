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
    public static class ReadUploadedFileRecords
    {
        [FunctionName("ReadUploadedFileRecords")]
        public static async Task<IActionResult> ReadUploadedFileRecordsForWorbix(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {

            string filename = req.Query["filename"];
            string numberOfRecords = req.Query["numberOfRecords"];
            string siteId = req.Query["siteID"];
            string spaceId= req.Query["spaceID"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            filename = filename ?? data?.filename;
            numberOfRecords = numberOfRecords ?? data?.numberOfRecords;
            siteId = siteId ?? data?.siteId;
            spaceId = spaceId ?? data?.spaceId;


            if (filename != null && numberOfRecords != null)
            {
                try
                {
                    List<SqlParameter> sp = new List<SqlParameter>()
                    {
                       new SqlParameter() {ParameterName = "@fileName", SqlDbType = System.Data.SqlDbType.NVarChar, Value=filename},
                       new SqlParameter() {ParameterName = "@siteID", SqlDbType = System.Data.SqlDbType.BigInt, Value=siteId},
                       new SqlParameter() {ParameterName = "@spaceID", SqlDbType = System.Data.SqlDbType.BigInt, Value=spaceId},
                       new SqlParameter() {ParameterName = "@topN", SqlDbType = System.Data.SqlDbType.BigInt, Value=numberOfRecords},
                       //new SqlParameter(){ParameterName="@result", SqlDbType=System.Data.SqlDbType.NVarChar,Direction=System.Data.ParameterDirection.Output}
                       new SqlParameter(){ParameterName="@result", SqlDbType=System.Data.SqlDbType.NVarChar,Value=""}
                    };
                    var records = await RunStoredProcedure(sp);
                    return new OkObjectResult(records);
                }
                catch (Exception e)
                {
                    return new BadRequestObjectResult("Request generated an exception ->" + e.Message);

                }
            }
            else
            {
                return new BadRequestObjectResult("Please pass a filename and number of records to be selected as parameters on the query string or in the request body");
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
                    string storedProcedureName = "Process.GetTopNRows";

                    using (SqlCommand cmd = new SqlCommand(storedProcedureName, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddRange(paramList.ToArray());
                        SqlDataReader reader = await cmd.ExecuteReaderAsync();
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
