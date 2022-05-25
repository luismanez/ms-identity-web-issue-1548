using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace DurableAppV4Net6Test
{
    public class Graphito
    {
        private readonly GraphServiceClient _graphServiceClient;

        // not used in the sample, but just checking while debugging if they were injected properly
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IDownstreamWebApi _downstreamWebApi;

        public Graphito(
            GraphServiceClient graphServiceClient,
            ITokenAcquisition tokenAcquisition,
            IDownstreamWebApi downstreamWebApi)
        {
            _graphServiceClient = graphServiceClient;
            _tokenAcquisition = tokenAcquisition;
            _downstreamWebApi = downstreamWebApi;
        }

        [FunctionName(nameof(GraphitoHttpTrigger))]
        public async Task<IActionResult> GraphitoHttpTrigger(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync(nameof(GraphitoOrchestrator), null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(GraphitoOrchestrator))]
        public async Task<string> GraphitoOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var output = await context.CallActivityAsync<string>(nameof(GraphitoActivity), "Alice");

            return output;
        }

        [FunctionName(nameof(GraphitoActivity))]
        public async Task<string> GraphitoActivity([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"[DurableActivity] Saying hello to {name}.");

            try
            {
                var firstUser = await _graphServiceClient
                    .Users
                    .Request()
                    .Select("displayName")
                    .Top(1)
                    .GetAsync();

                var myName = firstUser.FirstOrDefault().DisplayName;

                var message = $"Hello {name}, I am {myName}!";

                log.LogInformation($"[DurableActivity] {message}.");

                return message;
            }
            catch (System.Exception ex)
            {
                log.LogError(ex.ToString());
                throw;
            }
        }        
    }
}