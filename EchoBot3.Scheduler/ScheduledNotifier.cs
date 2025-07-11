using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http.Headers;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

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
            var apiKey = _configuration["BotApiKey"];
            var chatId = _configuration["ChatId"];
            var serviceUrl = _configuration["ServiceUrl"];
            var tenantId = _configuration["TenantId"];
            var apiScope = _configuration["BotApiScope"]; // e.g., "api://<client-id>/Messages.Send"

            logger.LogInformation($"BotApiUrl: {botApiUrl}, ApiScope: {apiScope}");
            //if (string.IsNullOrEmpty(botApiUrl))// || string.IsNullOrEmpty(apiKey))
            //{
            //    logger.LogError("BotApiUrl or BotApiKey is not configured. Aborting.");
            //    return;
            //}
            if (string.IsNullOrEmpty(botApiUrl) || string.IsNullOrEmpty(apiScope))
            {
                logger.LogError("BotApiUrl or BotApiScope is not configured. Aborting.");
                return;
            }

            try
            {

                // 1. Acquire token using Managed Identity
                //var credential = new DefaultAzureCredential();
                var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ExcludeEnvironmentCredential = true,
                    ExcludeWorkloadIdentityCredential = true,
                    ExcludeSharedTokenCacheCredential = true,
                    ExcludeVisualStudioCredential = true,
                    ExcludeVisualStudioCodeCredential = true,
                    ExcludeAzureCliCredential = true,
                    ExcludeAzurePowerShellCredential = true,
                    ExcludeInteractiveBrowserCredential = true
                });
                logger.LogInformation("Acquiring token using DefaultAzureCredential...");
                var tokenRequestContext = new TokenRequestContext(new[] { apiScope });
                logger.LogInformation($"Requesting token for scope: {apiScope}");
                var accessToken = await credential.GetTokenAsync(tokenRequestContext);
               
                // DEBUG: Log token details (don't do this in production!)
                logger.LogInformation($"Token acquired successfully. Expires at: {accessToken.ExpiresOn}");
                //var client = _httpClientFactory.CreateClient();
                //var request = new HttpRequestMessage(HttpMethod.Post, $"{botApiUrl}/api/teamsmessage/send");

                // Decode the token to see what roles it contains (for debugging)
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var token = tokenHandler.ReadJwtToken(accessToken.Token);
                var roles = token.Claims.Where(c => c.Type == "roles").Select(c => c.Value);
                logger.LogInformation($"Token contains roles: {string.Join(", ", roles)}");


                // 2. Make the authenticated call
                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, $"{botApiUrl}/api/teamsmessage/send");          

                // Add the token to the Authorization header
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);

                // Add the security API key to the header
                //request.Headers.Add("X-Api-Key", apiKey);
                // Fallback: also add API key (remove this once Azure AD works)
                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Add("X-Api-Key", apiKey);
                }
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
            catch (Azure.Identity.AuthenticationFailedException authEx)
            {
                logger.LogError(authEx, "❌ Azure Identity authentication failed");
                logger.LogError($"Error details: {authEx.Message}");

                // Check if it's a scope/permission issue
                if (authEx.Message.Contains("AADSTS70011") || authEx.Message.Contains("invalid_scope"))
                {
                    logger.LogError("This looks like a scope/permission issue. Check:");
                    logger.LogError("1. BotApiScope format should be: api://YOUR_API_CLIENT_ID/.default");
                    logger.LogError("2. Function App's Managed Identity has been granted the application role");
                    logger.LogError("3. Admin consent has been granted");
                }
            }
            catch (Exception ex)
            {
                // Log the full exception details to see the root cause
                logger.LogError(ex, "An exception occurred: {ExceptionMessage}", ex.Message);
                logger.LogError("Full exception details: {ExceptionDetails}", ex.ToString());
            }
        }
    }

}
