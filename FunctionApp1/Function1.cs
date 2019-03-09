
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Linq;
using Microsoft.Azure.ServiceBus;

namespace FunctionApp1
{
    public static class Function1
    {
        private static HttpClient httpClient = new HttpClient();

        [FunctionName("HttpFunction1")]
        public static async Task<IActionResult> HttpFunction1(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            //------------- start activity for distributed trace -----------------
            var current = Activity.Current ?? new Activity(context.FunctionName);
            // Inside of the header, it Request-Id is included. 
            if (req.Headers.TryGetValue("Request-Id", out StringValues requestId))
                current.SetParentId(requestId.FirstOrDefault());
            current.Start();
            log.LogInformation($"Id:{current.Id} ParentId:{current.ParentId} RootId:{current.RootId} ");


            string name = GetNameFromRequest(req);
            current.AddBaggage("HttpFunction1Name", name); // add Correlation-Context

            log.LogInformation($"{context.FunctionName} C# HTTP trigger function processed a request.");

            // test call to httpbin
            log.LogInformation(
                await (await httpClient.GetAsync($"https://httpbin.org/get?show_env={context.FunctionName}")).Content
                    .ReadAsStringAsync());

            // test call to HttoFunctiojn2
            var response = await httpClient.PostAsync($"{req.Scheme}://{req.Host}/api/{nameof(HttpFunction2)}"
                , new StringContent(JsonConvert.SerializeObject(new {name = name}), Encoding.UTF8, @"application/json"));

            return name != null
                ? (ActionResult) new OkObjectResult($"Hello, {await response.Content.ReadAsStringAsync()}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        [FunctionName("HttpFunction2")]
        public static async Task<IActionResult> HttpFunction2(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            //------------- start activity for distributed trace -----------------
            var current = Activity.Current ?? new Activity(context.FunctionName);
            // Inside of the header, it Request-Id is included. 
            if (req.Headers.TryGetValue("Request-Id", out StringValues requestId))
                current.SetParentId(requestId.FirstOrDefault());
            current.Start();
            log.LogInformation($"Id:{current.Id} ParentId:{current.ParentId} RootId:{current.RootId} ");


            string name = GetNameFromRequest(req);
            current.AddBaggage(context.FunctionName, name); // add Correlation-Context

            log.LogInformation($"{context.FunctionName} C# HTTP trigger function processed a request.");

            // test call to httpbin
            log.LogInformation(
                await (await httpClient.GetAsync($"https://httpbin.org/get?show_env={context.FunctionName}")).Content
                    .ReadAsStringAsync());

            // test call to HttoFunctiojn2
            var response = await httpClient.PostAsync($"{req.Scheme}://{req.Host}/api/{nameof(HttpFunction3)}"
                , new StringContent(JsonConvert.SerializeObject(new {name = name}), Encoding.UTF8, @"application/json"));

            return name != null
                ? (ActionResult) new OkObjectResult($"Ç±ÇÒÇ…ÇøÇÕ, {await response.Content.ReadAsStringAsync()}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        [FunctionName("HttpFunction3")]
        public static async Task<IActionResult> HttpFunction3(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req,
            [ServiceBus("service-bus-func1", Connection = "AzureWebJobsServiceBus")]IAsyncCollector<Message> serviceBusQueue1,
            ILogger log, ExecutionContext context)
        {
            //------------- start activity for distributed trace -----------------
            var current = Activity.Current ?? new Activity(context.FunctionName);
            // Inside of the header, it Request-Id is included. 
            if (req.Headers.TryGetValue("Request-Id", out StringValues requestId))
                current.SetParentId(requestId.FirstOrDefault());
            current.Start();
            log.LogInformation($"Id:{current.Id} ParentId:{current.ParentId} RootId:{current.RootId} ");


            string name = GetNameFromRequest(req);
            current.AddBaggage(context.FunctionName, name); // add Correlation-Context

            log.LogInformation($"{context.FunctionName} C# HTTP trigger function processed a request.");

            // test call to httpbin
            log.LogInformation(
                await (await httpClient.GetAsync($"https://httpbin.org/get?show_env={context.FunctionName}")).Content
                    .ReadAsStringAsync());

            // Send Message to Service Bus
            string messageBody = JsonConvert.SerializeObject(new { content = "Sample Massage" });
            await serviceBusQueue1.AddAsync(new Message(Encoding.UTF8.GetBytes(messageBody)));

            return name != null
                ? (ActionResult) new OkObjectResult($"Ç⁄ÇÒÇ∂Ç„Å[ÇÈ, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        private static string GetNameFromRequest(HttpRequest req)
        {
            string name = req.Query["name"];
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            return name ?? data?.name;
        }
        
        [FunctionName("SerbiceBusFunction1")]
        public static async Task SerbiceBusFunction1(
            [ServiceBusTrigger("service-bus-func1", Connection = "AzureWebJobsServiceBus")]Message message,
            [ServiceBus("service-bus-func2", Connection = "AzureWebJobsServiceBus")]IAsyncCollector<Message> serviceBusQueue2,
            [ServiceBus("service-bus-func3", Connection = "AzureWebJobsServiceBus")]IAsyncCollector<Message> serviceBusQueue3,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation($"{context.FunctionName} C# ServiceBus trigger function processed a request.");
            
            string messageBody = JsonConvert.SerializeObject(new { content="Sample Massage 1"});
            await serviceBusQueue2.AddAsync(new Message(Encoding.UTF8.GetBytes(messageBody)));
            await serviceBusQueue3.AddAsync(new Message(Encoding.UTF8.GetBytes(messageBody)));
        }

        [FunctionName("SerbiceBusFunction2")]
        public static async Task SerbiceBusFunction2(
            [ServiceBusTrigger("service-bus-func2", Connection = "AzureWebJobsServiceBus")]Message message,
            [ServiceBus("service-bus-func3", Connection = "AzureWebJobsServiceBus")]IAsyncCollector<Message> serviceBusQueue3,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation($"{context.FunctionName} C# ServiceBus trigger function processed a request.");

            string messageBody = JsonConvert.SerializeObject(new { content = "Sample Massage 2" });
            await serviceBusQueue3.AddAsync(new Message(Encoding.UTF8.GetBytes(messageBody)));
        }

        [FunctionName("SerbiceBusFunction3")]
        public static async Task SerbiceBusFunction3(
            [ServiceBusTrigger("service-bus-func3", Connection = "AzureWebJobsServiceBus")]Message message,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation($"{context.FunctionName} C# ServiceBus trigger function processed a request.");
            
            // test call to httpbin
            log.LogInformation(
                await (await httpClient.GetAsync($"https://httpbin.org/get?show_env={context.FunctionName}")).Content
                    .ReadAsStringAsync());

            // test call to httpbin
            //log.LogInformation(
            //    await (await httpClient.GetAsync($"http://localhost:7071/api/{nameof(HttpFunction4)}")).Content
            //        .ReadAsStringAsync());
            log.LogInformation(
                await (await httpClient.GetAsync($"https://ec-deistibuted-tracing-sample-func.azurewebsites.net/api/{nameof(HttpFunction4)}")).Content
                    .ReadAsStringAsync());
        }

        [FunctionName("HttpFunction4")]
        public static async Task<IActionResult> HttpFunction4(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            //------------- start activity for distributed trace -----------------
            var current = Activity.Current ?? new Activity(context.FunctionName);
            // Inside of the header, it Request-Id is included. 
            if (req.Headers.TryGetValue("Request-Id", out StringValues requestId))
                current.SetParentId(requestId.FirstOrDefault());
            current.Start();
            log.LogInformation($"Id:{current.Id} ParentId:{current.ParentId} RootId:{current.RootId} ");
            

            log.LogInformation($"{context.FunctionName} C# HTTP trigger function processed a request.");

            // test call to httpbin
            log.LogInformation(
                await (await httpClient.GetAsync($"https://httpbin.org/get?show_env={context.FunctionName}")).Content
                    .ReadAsStringAsync());

            return (ActionResult) new OkObjectResult($"OK");
        }
    }
}
