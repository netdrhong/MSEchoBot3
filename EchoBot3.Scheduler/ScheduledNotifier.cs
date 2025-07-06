using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace EchoBot3.Scheduler
{
    public class ScheduledNotifier
    {
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        public ScheduledNotifier(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<ScheduledNotifier>();
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }       

        // CRON Expression: Runs at the top of every 5th minute
        [Function("ScheduledNotifier")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, FunctionContext context)
        {
            var logger = context.GetLogger("ScheduledNotifier");
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var botApiUrl = _configuration["BotApiUrl"];
            //var apiKey = _configuration["BotApiKey"];
            var chatId = _configuration["ChatId"];
            var serviceUrl = _configuration["ServiceUrl"];
            var tenantId = _configuration["TenantId"];

            if (string.IsNullOrEmpty(botApiUrl))// || string.IsNullOrEmpty(apiKey))
            {
                logger.LogError("BotApiUrl or BotApiKey is not configured. Aborting.");
                return;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, $"{botApiUrl}/api/teamsmessage/send");

                // Add the security API key to the header
                //request.Headers.Add("X-Api-Key", apiKey);

                // Create the JSON body
                var jsonBody = $@"{{
                    ""text"": ""This is a scheduled message from the Azure Function."",
                    ""chatId"": ""{chatId}"",
                    ""serviceUrl"": ""{serviceUrl}"",
                    ""tenantId"": ""{tenantId}""
                }}";
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Send the request
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("Successfully sent message to the bot API.");
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    logger.LogError($"Failed to send message. Status: {response.StatusCode}. Response: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An exception occurred while trying to send a message.");
            }
        }
    }

}
