using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TwilioSupportFunctions
{
    public static class ProcessNumbersOrchestrators
    {
        [FunctionName("O_CallSupport")]
        public static async Task<IActionResult> CallSupport([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            try
            { 
                var waitBetweenTries = TimeSpan.FromSeconds(100); // 3 tries in 5 minutes
                var phoneNumbers = await context.CallActivityAsync<string[]>("A_GetNumbersFromStorage", null);
                var callTime = context.CurrentUtcDateTime;

                foreach (var phoneNumber in phoneNumbers)
                {
                    CallInfo callinfo = new CallInfo
                    {
                        NumberToCall = phoneNumber,
                        InstanceId = context.InstanceId
                    };

                    for (var i = 0; i < 3; i++)
                    {
                        using (var timeoutCts = new CancellationTokenSource())
                        {
                            if (!context.IsReplaying)
                            {
                                log.LogWarning("About to call A_MakeCall activity. ");
                            }

                            await context.CallActivityAsync("A_MakeCall", callinfo);
                            var twilioCallbackTask = context.WaitForExternalEvent<string>("TwilioCallback");
                            var timeoutTask = context.CreateTimer(context.CurrentUtcDateTime.AddMinutes(1), timeoutCts.Token);
                            if (twilioCallbackTask == await Task.WhenAny(twilioCallbackTask, timeoutTask))
                            {
                                timeoutCts.Cancel();
                                var twilioResult = twilioCallbackTask.Result;

                                log.LogWarning($"twilioResult = {twilioResult}");

                                if (twilioResult == "in-progress")
                                {
                                    log.LogWarning($"Call was in-progress by {phoneNumber} at {context.CurrentUtcDateTime}");
                                    return new OkObjectResult("was in-progress");
                                }
                                else if (twilioResult == "completed")
                                {
                                    log.LogWarning($"Call was completed by {phoneNumber} at {context.CurrentUtcDateTime}");
                                    //return new OkObjectResult("was completed");
                                }
                                else if (twilioResult == "answered")
                                {
                                    log.LogWarning($"Call was answered by {phoneNumber} at {context.CurrentUtcDateTime}");
                                    return new OkObjectResult("was answered");
                                }
                                else
                                {
                                    log.LogWarning($"Result was  {twilioResult} at {context.CurrentUtcDateTime}");
                                    log.LogWarning($"Call Wasnt answered by {phoneNumber} at {context.CurrentUtcDateTime}");
                                }
                            }
                        }

                        var currTime = context.CurrentUtcDateTime;
                        if (currTime - callTime < waitBetweenTries)
                        {
                            // wait a bit till next try
                            await context.CreateTimer(callTime.Add(waitBetweenTries), CancellationToken.None);
                        }
                    }
                }

                // save message in queue then start again

                return new OkObjectResult($"Finished calling all of the numbers at at {context.CurrentUtcDateTime} and no one picked up.");
            }
            catch (Exception e)
            {
                log.LogWarning(e.Message);
                return new BadRequestResult();
            }
        }
    }
}