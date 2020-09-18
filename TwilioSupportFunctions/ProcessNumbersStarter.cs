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
    }
}
