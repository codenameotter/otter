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
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;

namespace Relevate
{
    public class WorbixClient
    {
        public string SiteId { get; set; }
        public string SpaceId { get; set; }
        public string ClientName { get; set; }
    }

    public class RegistrationResponse
    {
        public string ClientId { get; set; }
        public string BlobContainerURL { get; set; }
    }

    public static class RegisterClient
    {
        public static WorbixClient worbixClient;
        public static RegistrationResponse registrationResponse;
        [FunctionName("RegisterClient")]
        public static async Task<IActionResult> RegisterClientForWorbix(
            [HttpTrigger(AuthorizationLevel.Anonymous,"post", Route = "RegisterClient")] HttpRequest req,
             ILogger log)
        {
            string requestBody;
            //string storageConnectionString = Environment.GetEnvironmentVariable("storageconnectionstring");
            string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=inputsa;AccountKey=J3v5ZwzS34fR6pftCnptE7/PN3IFd0/hdZkR895F8N8iGKoV6MBUiZWQKTLmM1KwF8vTzArU7fo8DDW9uPIUnA==;EndpointSuffix=core.windows.net";
            CloudStorageAccount storageAccount;
            CloudBlobClient cloudBlobClient;
            CloudBlobContainer cloudBlobContainer;

            try
            {
                requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic worbixClient = JsonConvert.DeserializeObject<WorbixClient>(requestBody);

                if (worbixClient.SiteId != null && worbixClient.SpaceId != null && worbixClient.ClientName != null)
                {
                    List<SqlParameter> sp = new List<SqlParameter>()
                    {
                        new SqlParameter() {ParameterName = "@ApplicationName", SqlDbType = System.Data.SqlDbType.NVarChar, Value = "Worbix"},
                        new SqlParameter() {ParameterName = "@AggregationLevel1", SqlDbType = System.Data.SqlDbType.NVarChar, Value = worbixClient.ClientName},
                        new SqlParameter() {ParameterName = "@AggregationLevel2", SqlDbType = System.Data.SqlDbType.BigInt, Value=worbixClient.SiteId},
                        new SqlParameter() {ParameterName = "@AggregationLevel3", SqlDbType = System.Data.SqlDbType.BigInt, Value = worbixClient.SpaceId},
                        new SqlParameter(){ParameterName="@LineageId", SqlDbType=System.Data.SqlDbType.BigInt,Direction=System.Data.ParameterDirection.Output}
                    };
                    var clientid = await RunStoredProcedure(sp);
                    if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
                    {
                        // If the connection string is valid, proceed with operations against Blob storage here.
                        cloudBlobClient = storageAccount.CreateCloudBlobClient();
                        cloudBlobContainer = cloudBlobClient.GetContainerReference(worbixClient.ClientName.ToLower());
                        await cloudBlobContainer.CreateIfNotExistsAsync();
                        //set permissions etc..get a token
                        BlobContainerPermissions permissions = new BlobContainerPermissions
                        {
                            PublicAccess = BlobContainerPublicAccessType.Blob
                        };
                        
                        await cloudBlobContainer.SetPermissionsAsync(permissions);
                        return new OkObjectResult(new RegistrationResponse
                        {
                            ClientId = clientid,
                            BlobContainerURL = cloudBlobContainer.Uri.ToString()
                        });
                    }
                    return new BadRequestObjectResult("Looks like storage account does not exist or you may not have required permissions. ");

                }
                return new BadRequestObjectResult("Please pass siteId, spaceId and clientName parameters in the request body");
            }
            catch(Exception e)
            {
                return new BadRequestObjectResult("Request did not have valid body or generated an exception ->"+e.Message);
            }
        }

        public  static async Task<string> RunStoredProcedure(List<SqlParameter> paramList)
        {
           
                //var str = Environment.GetEnvironmentVariable("sqldb_connection");
                var str = "Server=tcp:contata.database.windows.net,1433;Initial Catalog=OTTER;Persist Security Info=False;User ID=contata.admin;Password=C@ntata123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;";
                string lineageId;
                try
                {
                    using (SqlConnection conn = new SqlConnection(str))
                    {
                        conn.Open();
                    //string storedProcedureName = Environment.GetEnvironmentVariable("register_client");
                    string storedProcedureName = "App.usp_ClientSiteMapping";

                    using (SqlCommand cmd = new SqlCommand(storedProcedureName, conn))
                        {
                            cmd.CommandType = System.Data.CommandType.StoredProcedure;
                            cmd.Parameters.AddRange(paramList.ToArray());
                            SqlDataReader reader = await cmd.ExecuteReaderAsync();
                            lineageId = paramList.Find((v) => { return v.Direction == System.Data.ParameterDirection.Output; }).Value.ToString();
                            reader.Close();
                        }

                    }
                }
                catch
                {
                    lineageId = "-1";
                }
            return lineageId;
        }
    }
}
