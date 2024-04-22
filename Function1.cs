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
            var config =  GetJsonFromBlob();
            string[] activityFunctionNames = config.GetSection("ActivityFunctions").Get<string[]>();

            //IConfiguration config = new ConfigurationBuilder()
            //    .SetBasePath(Directory.GetCurrentDirectory())
            //    .AddJsonFile("config.json", optional: false, reloadOnChange: true)
            //    .Build();

            //string[] activityFunctionNames = config.GetSection("ActivityFunctions").Get<string[]>();

            foreach (string activityFunctionName in activityFunctionNames)
            {
                switch (activityFunctionName)
                {
                    case "Action1Activity":
                        await context.CallActivityAsync("Action1Activity", null);
                        break;
                    case "Action2Activity":
                        await context.CallActivityAsync("Action2Activity", null);
                        break;
                    case "AllActivity":
                        await context.CallActivityAsync("Action1Activity", null);
                        await context.CallActivityAsync("Action2Activity", null);
                        break;
                    default:
                        Console.WriteLine($"Invalid activity function '{activityFunctionName}' specified in configuration.");
                        break;
                }
            }
        }

        [Function(nameof(Action1Activity))]
        public  void Action1Activity([ActivityTrigger] TaskContext context)
        {
            Console.WriteLine("Action1 activity executed.");
        }

        [Function(nameof(Action2Activity))]
        public  void Action2Activity([ActivityTrigger] TaskContext context)
        {
            Console.WriteLine("Action2 activity executed.");
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
            var blobServiceClient = new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=testdurable86ba;AccountKey=PLRr9f7iU4dPGBG3EZ3S2r7QCI7X1B15rJSTCsBw6V/d4f8rwwVe2YFkjpP2KGZIvtCK9L3zEGxQ+AStmhpfZQ==;EndpointSuffix=core.windows.net");
            var blobContainerClient = blobServiceClient.GetBlobContainerClient("testorchestratorcontainers");
            var blobClient = blobContainerClient.GetBlobClient("config.json");

            using var memoryStream = new MemoryStream();
            blobClient.DownloadTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonStream(memoryStream);


            return configurationBuilder.Build();
        }
    }
}
