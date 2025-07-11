using EchoBot3.Authentication;
using EchoBot3.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web.Resource;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EchoBot3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    //[ApiKeyAuth]
    public class TeamsMessageController : ControllerBase
    {


        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TeamsMessageController> _logger;
        private readonly IBot _bot;

        // Required scope from your App Registration
        private const string SendMessagesScope = "Messages.Send";


        public TeamsMessageController(
            IBotFrameworkHttpAdapter adapter,
            IConfiguration configuration,
            ILogger<TeamsMessageController> logger,
            IBot bot)
        {
            _adapter = adapter;
            _configuration = configuration;
            _logger = logger;
            _bot = bot;
        }

        [HttpGet("get")]
        public IActionResult Get()
        {
            return Ok(new { Message = "TeamsMessageController is ready to send messages." });
        }

        [HttpPost("send")]
        //[RequiredScope(SendMessagesScope)]
        [Authorize(Policy = "RequireAppRole")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                // Validate request
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Get bot configuration
                var appId = _configuration["MicrosoftAppId"];
                var tenantId = request.TenantId ?? _configuration["MicrosoftAppTenantId"];

                if (string.IsNullOrEmpty(appId))
                {
                    return BadRequest("Bot App ID is not configured.");
                }

                // Create conversation reference
                var conversationReference = CreateConversationReference(request, appId, tenantId);

                string activityId = null;

                // Send proactive message
                await ((CloudAdapter)_adapter).ContinueConversationAsync(
                    appId,
                    conversationReference,
                    async (turnContext, cancellationToken) =>
                    {
                        // Create and send the message
                        var message = MessageFactory.Text(request.Text);
                        var response = await turnContext.SendActivityAsync(message, cancellationToken);
                        activityId = response.Id;
                    },
                    CancellationToken.None);

                _logger.LogInformation($"Message sent successfully to chat {request.ChatId}");

                return Ok(new SendMessageResponse
                {
                    Success = true,
                    Message = "Message sent successfully",
                    ActivityId = activityId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message to chat {request.ChatId}: {ex.Message}");

                return StatusCode(500, new SendMessageResponse
                {
                    Success = false,
                    Message = $"Failed to send message: {ex.Message}"
                });
            }
        }

        [HttpPost("send-card")]
        //[RequiredScope(SendMessagesScope)]
        [Authorize(Policy = "RequireAppRole")]
        public async Task<IActionResult> SendCardMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                // Validate request
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Get bot configuration
                var appId = _configuration["MicrosoftAppId"];
                var tenantId = request.TenantId ?? _configuration["MicrosoftAppTenantId"];

                if (string.IsNullOrEmpty(appId))
                {
                    return BadRequest("Bot App ID is not configured.");
                }

                // Create conversation reference
                var conversationReference = CreateConversationReference(request, appId, tenantId);

                string activityId = null;

                // Send proactive message with card
                await ((CloudAdapter)_adapter).ContinueConversationAsync(
                    appId,
                    conversationReference,
                    async (turnContext, cancellationToken) =>
                    {
                        // Create an adaptive card
                        var card = CreateAdaptiveCard(request.Text);
                        var message = MessageFactory.Attachment(card);
                        var response = await turnContext.SendActivityAsync(message, cancellationToken);
                        activityId = response.Id;
                    },
                    CancellationToken.None);

                _logger.LogInformation($"Card message sent successfully to chat {request.ChatId}");

                return Ok(new SendMessageResponse
                {
                    Success = true,
                    Message = "Card message sent successfully",
                    ActivityId = activityId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending card message to chat {request.ChatId}: {ex.Message}");

                return StatusCode(500, new SendMessageResponse
                {
                    Success = false,
                    Message = $"Failed to send card message: {ex.Message}"
                });
            }
        }

        private ConversationReference CreateConversationReference(SendMessageRequest request, string appId, string tenantId)
        {
            // Create conversation reference for proactive messaging
            var conversationReference = new ConversationReference
            {
                ServiceUrl = request.ServiceUrl,
                Conversation = new ConversationAccount
                {
                    Id = request.ChatId,
                    TenantId = tenantId
                },
                Bot = new ChannelAccount
                {
                    Id = appId
                },
                ChannelId = "msteams" // Teams channel identifier
            };

            return conversationReference;
        }

        private Attachment CreateAdaptiveCard(string text)
        {
            // Create a simple adaptive card
            var cardJson = $@"{{
                ""type"": ""AdaptiveCard"",
                ""version"": ""1.4"",
                ""body"": [
                    {{
                        ""type"": ""TextBlock"",
                        ""text"": ""{text.Replace("\"", "\\\"")}"",
                        ""wrap"": true,
                        ""size"": ""Medium""
                    }}
                ],
                ""actions"": [
                    {{
                        ""type"": ""Action.OpenUrl"",
                        ""title"": ""Learn More"",
                        ""url"": ""https://adaptivecards.io/""
                    }}
                ]
            }}";

            return new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = Newtonsoft.Json.JsonConvert.DeserializeObject(cardJson)
            };
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
        }

        [HttpPost]
        [HttpGet]
        [RequiredScope(SendMessagesScope)]
        public async Task PostAsync()
        {
            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.
            await _adapter.ProcessAsync(Request, Response, _bot);
        }

    }
}