using System;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System.Net;

namespace MockChannelIP.Controllers
{
    [RoutePrefix("v3/conversations")]
    public class MockChannelController : ApiController
    {
        static CloudStorageAccount account = default(CloudStorageAccount);
        static CloudTableClient tableClient;
        static CloudTable botOperationLogTable;
        private static HttpClient botHttpClient = new HttpClient();
        static MockChannelController()
        {
            string azureStoragAccount = ConfigurationManager.AppSettings["AzureStorageAccount"];
            string azureStorageSecret = ConfigurationManager.AppSettings["AzureStorageSecret"];
            if (!string.IsNullOrEmpty(azureStoragAccount) && !string.IsNullOrEmpty(azureStorageSecret))
            {
                account = new CloudStorageAccount(new StorageCredentials(azureStoragAccount, azureStorageSecret), true);
                tableClient = account.CreateCloudTableClient();
                botOperationLogTable = tableClient.GetTableReference(ConfigurationManager.AppSettings["TableName"]);
                botOperationLogTable.CreateIfNotExistsAsync();
            }
           
        }
        // Capture the BOT Response and save into Storage Table
        [HttpPost]
        [Route("{conversationId}/activities/{activityId}")]
        public async Task<HttpResponseMessage> ReplyToActivity(string conversationId, string activityId, [FromBody]Activity activity)
        {
          
            string content = JsonConvert.SerializeObject(activity);
            // saves limited content
            content = content.Length >= 25000 ? content.Substring(0, 25000) : content;
            if (activityId == null)
                activityId = Guid.NewGuid().ToString();
            if (activity.Type == "endOfConversation")
            {
                // ignore end of conversation messages
               return Request.CreateResponse(HttpStatusCode.OK);
            }
            else 
            {
                try { await StoreResponse(conversationId, activityId + "Resp", content, activity.Timestamp.Value.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"), true).ConfigureAwait(false); }
                catch (Exception e)
                {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError);
                }
            }
         
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        // Retrieve the BOT Response from Storage Table
        [HttpGet]
        [Route("getConversation")]
        public async Task<ActivityEntity> GetConversation(string conversationId, string activityId)
        {

            ActivityEntity responseEntity = new ActivityEntity(conversationId, activityId);

            try
            {
                TableOperation retrieveOperation = TableOperation.Retrieve<ActivityEntity>(conversationId, activityId);
                TableResult query = await botOperationLogTable.ExecuteAsync(retrieveOperation);

                if (query.Result != null)
                {
                   
                    responseEntity.incomingAcvitityMsg = ((ActivityEntity)query.Result).incomingAcvitityMsg;
                    responseEntity.botResponseMsg = ((ActivityEntity)query.Result).botResponseMsg;
                
                }

                return responseEntity;
            }

            catch (Exception e)
            {
                responseEntity.botResponseMsg = "Exception calling from azure storage table";
                System.Diagnostics.Trace.TraceError("Exception calling from azure storage table" + e.Message);
                return responseEntity;
            }
        }

        /// <summary>
        /// Method to save response from BOT into Table Storage
        /// </summary>
        /// <param name="conversationId"></param>
        /// <param name="activityId"></param>
        /// <param name="botResponse"></param>
        /// <param name="resptime"></param>
        /// <param name="isResp"></param>
        /// <returns></returns>
        private async Task<bool> StoreResponse(string conversationId, string activityId, string botResponse, string resptime, bool isResp)
        {
           
            ActivityEntity respActivityEntity = new ActivityEntity(conversationId, activityId, resptime, isResp);
            respActivityEntity.botResponseMsg = botResponse;
            respActivityEntity.responseTimeStamp = resptime;
            try
            {
                TableOperation operation = TableOperation.Insert(respActivityEntity);
                await botOperationLogTable.ExecuteAsync(operation).ConfigureAwait(false);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}