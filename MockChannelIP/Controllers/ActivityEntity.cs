using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace MockChannelIP.Controllers
{
    public class ActivityEntity : TableEntity
    {
        public ActivityEntity()
        {

        }
        public ActivityEntity(string convId)
        {
            this.PartitionKey = convId;
        }
        public ActivityEntity(string convId, string activityId)
        {
            this.PartitionKey = convId;
            this.RowKey = activityId;
        }
        public ActivityEntity(string convId, string activityId, string resptime, Boolean isResp) : this(convId, activityId)
        {
            if (isResp)
                this.responseTimeStamp = resptime;
        }
        public string activityMsg;
        public string scenarioName { get; set; }
        public string incomingAcvitityMsg { get; set; }
        public string botResponseMsg { get; set; }

        public string requestTimeStamp { get; set; }
        public string responseTimeStamp { get; set; }
    }
   
}