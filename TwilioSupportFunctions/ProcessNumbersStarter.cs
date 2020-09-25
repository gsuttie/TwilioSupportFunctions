using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Net.Http;
using System.Net;
using System.Collections.Specialized;

namespace TwilioSupportFunctions
{
    public static class ProcessNumbersStarter
    {
        [FunctionName("CallSupportNumbers")]
        public static async Task<IActionResult> StartPeriodicTask(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger log)
        {
            var instanceId = await client.StartNewAsync("O_CallSupport", null);

            return client.CreateCheckStatusResponse(req, instanceId);
        }

        //https://twiliosupportfunctionsdemo.azurewebsites.net/api/GetCallInfo?

        [FunctionName("GetCallInfo")]
        public static async Task<HttpResponseMessage> GetCallInfo(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "GetCallInfo")]
            HttpRequestMessage req,
           [DurableClient] IDurableOrchestrationClient client,
           ILogger log)
        {
            log.LogWarning("GetCallInfo function has been called");

            string instanceId = req.RequestUri.ParseQueryString()["instanceID"];

            var myCallbackContent = req.Content.ReadAsFormDataAsync().Result;

            log.LogWarning($"myCallbackContent = {myCallbackContent}");

            log.LogWarning($"CallStatus = {myCallbackContent.Get("CallStatus")}");

            log.LogWarning("About to try to call The EventSync");

            string callbackstatus = myCallbackContent.Get("CallStatus");

            log.LogWarning("Call EventSync");

            // send the ApprovalResult external event to this orchestration
            //await client.RaiseEventAsync(instanceId, "TwilioCallback", eventData: "answered");
            //await client.RaiseEventAsync(instanceId, "TwilioCallback", eventData: "in-progress");
            //await client.RaiseEventAsync(instanceId, "TwilioCallback", eventData: myCallbackContent);
            await client.RaiseEventAsync(instanceId, "TwilioCallback", eventData: callbackstatus);

            log.LogWarning("Raised the EventSync");
            
            
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
