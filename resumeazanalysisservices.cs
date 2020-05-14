using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using System;

namespace deploymentFunction
{

    public static class Extensions
    {
        public static StringContent AsJson(this object o)
         => new StringContent(JsonConvert.SerializeObject(o), Encoding.UTF8, "application/json");
    }
    public static class DeploymentFunction
    {
        private static HttpClient httpClient = new HttpClient();

        [FunctionName("deploymentFunction")]

        //Set AuthorizationLevel to Anonymous for Azure AD Authentication
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("Trigger Processed a request");

            // Access to App Settings
            var config = new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            // Get input

            string asname = req.Query["asname"];
            string asaction = req.Query["asaction"];
            string rgname = req.Query["rgname"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            asname = asname ?? data?.asname;
            asaction = asaction ?? data?.asaction;
            rgname = rgname ?? data?.scalingtype;
            // Check if customername is empty
            if (asname == null)
            {
                return new BadRequestObjectResult("Please pass the analysis name");
            }

            string performancetype = asname;

            // If no customertype is parsed, use standard performance tier as default
            string BaseUri = config["automationURIStandard"];

            switch (performancetype)
            {
                case "standard":
                    BaseUri = config["automationURIStandard"];
                    break;
                case "performance":
                    BaseUri = config["automationURIPerformance"];
                    break;
            }

            // Content webhook body
            var automationContent = new
            {
                asname = asname,
                asaction = asaction,
                rgname = rgname
            };

            //Post to webhook
            var response = await httpClient.PostAsync(BaseUri, automationContent.AsJson());
            var contents = await response.Content.ReadAsStringAsync();

            return (ActionResult)new OkObjectResult(contents);
        }
    }
}
