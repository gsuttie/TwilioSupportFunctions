using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace TwilioSupportFunctions
{
    public static class ProcessNumbersOrchestrators
    {
        [FunctionName("O_CallSupport")]
        public static async Task<string> CallSupport([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var waitBetweenTries = TimeSpan.FromSeconds(100); // 3 tries in 5 minutes
            var phoneNumbers = await context.CallActivityAsync<string[]>("A_GetNumbersFromStorage", null);
            var callTime = context.CurrentUtcDateTime;

            try
            {
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
                                if (twilioResult == "answered")
                                {
                                    log.LogWarning($"Call Answered by {phoneNumber} at {context.CurrentUtcDateTime}");
                                    return phoneNumber;
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

                return null;
            }
            catch (Exception e)
            {
                log.LogWarning(e.Message);
                return null;
            }
        }
    }
}