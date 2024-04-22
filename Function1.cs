using DurableTask.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace OrchestrationTriggerSwitch
{
    public  class Function1
    {
     
        [Function(nameof(RunOrchestrator))]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(RunOrchestrator));

            var config = GetJsonFromBlob();
            string[] activityFunctionNames = config.GetSection("ActivityFunctions").Get<string[]>();
            foreach (string activityFunctionName in activityFunctionNames)
            {
                switch (activityFunctionName)
                {
                    case "Action1Activity":
                        await context.CallActivityAsync("Action1Activity", logger);
                        break;
                    case "Action2Activity":
                        await context.CallActivityAsync("Action2Activity", logger);
                        break;
                    case "AllActivity":
                        await context.CallActivityAsync("Action1Activity", logger);
                        await context.CallActivityAsync("Action2Activity", logger);
                        await context.CallActivityAsync("Action3Activity", logger);
                        await context.CallActivityAsync("Action4Activity", logger);
                        break;
                    default:
                        logger.LogInformation($"Invalid activity function '{activityFunctionName}' specified in configuration.");
                        break;
                }
            }
        }

        [Function(nameof(Action1Activity))]
        public void Action1Activity([ActivityTrigger] FunctionContext executionContext)
        {
            ILogger log = executionContext.GetLogger("Action1Activity");
            log.LogInformation("Activity1 executed.");

        }
        [Function(nameof(Action2Activity))]
        public void Action2Activity([ActivityTrigger] FunctionContext executionContext)
        {
            ILogger log = executionContext.GetLogger("Action2Activity");
            log.LogInformation("Activity2 executed.");
        }
        [Function(nameof(Action3Activity))]
        public void Action3Activity([ActivityTrigger] FunctionContext executionContext)
        {
            ILogger log = executionContext.GetLogger("Action3Activity");
            log.LogInformation("Action3 activity started ......");
            Thread.Sleep(180000);
            log.LogInformation("Activity 3 is still running......." + DateTime.Now.ToString());
            Thread.Sleep(180000);
            log.LogInformation("Activity 3 is still running......." + DateTime.Now.ToString());
            Thread.Sleep(180000);
            log.LogInformation("Activity 3 is still running......." + DateTime.Now.ToString());
            Thread.Sleep(60000);
            log.LogInformation("Activity 3 completed it's execution" + DateTime.Now.ToString());

        }

        [Function(nameof(Action4Activity))]
        public void Action4Activity([ActivityTrigger] FunctionContext executionContext)
        {
            ILogger log = executionContext.GetLogger("Action4Activity");
            log.LogInformation("Activity4 executed.");
        }

        [Function("Function1_HttpStart")]
        public  async Task<HttpResponseData> HttpStart(
         [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
         [DurableClient] DurableTaskClient client,
         FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("Function1_HttpStart");

            // string instanceId = await client.StartNewAsync("RunOrchestrator");
            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(RunOrchestrator));

            logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return client.CreateCheckStatusResponse(req, instanceId);
        }

        private  static IConfiguration GetJsonFromBlob()
        {
            var blobServiceClient = new BlobServiceClient("<Your_blob_connectionstring>");
            var blobContainerClient = blobServiceClient.GetBlobContainerClient("<your_blob_container_name>");
            var blobClient = blobContainerClient.GetBlobClient("<blob_name>");

            using var memoryStream = new MemoryStream();
            blobClient.DownloadTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonStream(memoryStream);


            return configurationBuilder.Build();
        }
    }
}
