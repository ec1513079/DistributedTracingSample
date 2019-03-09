
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

namespace FunctionApp1
{
    public static class Function1
    {
        static HttpClient httpClient = new HttpClient();

        [FunctionName("HttpFunction1")]
        public static async Task<IActionResult> HttpFunction1([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info($"HttpFunction1 C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;
            
            var response = await httpClient.PostAsync($"{req.Scheme}://{req.Host}/api/{nameof(HttpFunction2)}"
                , new StringContent(JsonConvert.SerializeObject(new { name = name }), Encoding.UTF8, @"application/json"));

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {await response.Content.ReadAsStringAsync()}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
        
        [FunctionName("HttpFunction2")]
        public static async Task<IActionResult> HttpFunction2([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info($"HttpFunction1 C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult($"‚±‚ñ‚É‚¿‚Í, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
