using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace TwilioSupportFunctions
{
    public static class ProcessNumbersActivities
    {
        [FunctionName("A_GetNumbersFromStorage")]
        public static async Task<string[]> GetNumbersFromStorage([ActivityTrigger] string inputNumbers, ILogger log)
        {
            log.LogWarning($"GetNumbersFromStorage {inputNumbers}");

            string connectionString = Environment.GetEnvironmentVariable("funcdemostorConnString");

            string containerName = Environment.GetEnvironmentVariable("NumbersContainerName");
            string fileName = Environment.GetEnvironmentVariable("NumbersFileName");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            // Connect to the blob storage
            CloudBlobClient serviceClient = storageAccount.CreateCloudBlobClient();

            // Connect to the blob container
            CloudBlobContainer container = serviceClient.GetContainerReference($"{containerName}");

            // Connect to the blob file
            CloudBlockBlob blob = container.GetBlockBlobReference($"{fileName}");

            // Get the blob file as text
            string contents = blob.DownloadTextAsync().Result;

            string[] numbers = contents.Split(',');

            await Task.Delay(100);

            return numbers;
        }

        [FunctionName("A_MakeCall")]
        public static string MakeCall([ActivityTrigger] CallInfo callInfo, 
        [Table("MadeCalls", "AzureWebJobStorage")] out CallDetails calldetails, ILogger log)
        {
            
            log.LogWarning($"MakeCall to {callInfo.NumberToCall}");

            var madeCallId = Guid.NewGuid().ToString("N");

            calldetails = new CallDetails
            {
                PartitionKey = "MadeCalls",
                RowKey = madeCallId,
                OrchestrationId = callInfo.InstanceId,
                NumberCalled = callInfo.NumberToCall
            };

            string accountSid = Environment.GetEnvironmentVariable("accountSid");
            string authToken = Environment.GetEnvironmentVariable("authToken");
            TwilioClient.Init(accountSid, authToken);

            var to = new PhoneNumber(callInfo.NumberToCall);

            var from = new PhoneNumber(Environment.GetEnvironmentVariable("twilioDemoNumber"));

            log.LogWarning($"InstanceId {callInfo.InstanceId}");

            var statusCallbackUri = new Uri(string.Format(Environment.GetEnvironmentVariable("statusCallBackUrl"), callInfo.InstanceId));

            log.LogWarning($"statusCallbackUri {statusCallbackUri}");

            var statusCallbackEvent = new List<string> { "answered" };

            log.LogWarning($"About to make a call to  {to} from {from}.");

            var call = CallResource.Create(
                to,
                from,
                url: new Uri("http://demo.twilio.com/docs/voice.xml"),
                statusCallback: statusCallbackUri,
                statusCallbackMethod: Twilio.Http.HttpMethod.Post,
                statusCallbackEvent: statusCallbackEvent);

            return madeCallId;
        }
    }
}
