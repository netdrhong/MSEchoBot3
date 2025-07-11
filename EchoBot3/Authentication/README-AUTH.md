Show me the general steps and code example for implementing Azure AD authentication.

Of course. Here are the general steps and code examples for implementing the recommended approach: **Azure AD authentication using a Managed Identity**.

This process involves two main parts:
1.  **Securing the Bot API (`EchoBot3`)**: We'll configure it to require a valid Azure AD token.
2.  **Updating the Azure Function (`EchoBot3.Scheduler`)**: We'll modify it to acquire a token using its Managed Identity and send it with its requests.

---

### Part 1: Secure the Bot API (`EchoBot3`)

#### Step 1.1: Create an App Registration for the API

First, your bot's API needs an identity in Azure AD so it can define permissions.

1.  Go to the **Azure Portal > Azure Active Directory > App registrations**.
2.  Click **+ New registration**.
3.  Name it something like `EchoBot3-API`.
4.  Leave the other options as default and click **Register**.
5.  Once created, go to the **Expose an API** section.
6.  Click **Set** next to "Application ID URI". The default value is fine.
7.  Click **+ Add a scope**.
    *   **Scope name**: `Messages.Send`
    *   **Who can consent?**: Admins and users
    *   **Admin consent display name**: `Send messages via the bot`
    *   **Admin consent description**: `Allows the caller to send proactive messages through the bot's API.`
    *   Fill in the user consent fields similarly.
8.  Click **Add scope**.
9.  Copy the **Application (client) ID** and the **Directory (tenant) ID** from the "Overview" page. You'll need them for the next step.

25a3e11b-149c-47a3-9a5a-61c51125447f
4851961a-a473-455d-840f-221dc8c83528

#### Step 1.2: Update the `EchoBot3` Project

Now, let's configure the ASP.NET Core application to use Azure AD.

1.  **Install the necessary NuGet package**:
```powershell
```powershell
    Install-Package Microsoft.Identity.Web
```
```

2.  **Update `appsettings.json`**:
    Remove the `ApiKey` and add a new `AzureAd` section with the details from your App Registration.

```json
```json EchoBot3\appsettings.json
    {
      "MicrosoftAppType": "UserAssignedMSI",
      "MicrosoftAppId": "402e6bf2-5cd7-405e-97c9-c9d98c9af236",
      "MicrosoftAppPassword": "",
      "MicrosoftAppTenantId": "TENANTID",
      "AzureAd": {
        "Instance": "https://login.microsoftonline.com/",
        "TenantId": "YOUR_TENANT_ID", // The Directory (tenant) ID you copied
        "ClientId": "YOUR_CLIENT_ID"  // The Application (client) ID you copied
      }
    }
```
```

3.  **Update `Startup.cs` to add authentication services**:

```csharp
```csharp EchoBot3\Startup.cs
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.22.0

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;

namespace EchoBot3
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add Azure AD authentication for the API
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"));

            services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
            });

            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the Bot Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, Bots.EchoBot>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseWebSockets()
                .UseRouting()
                    // Add Authentication and Authorization middleware
                    .UseAuthentication()
                    .UseAuthorization()
                    .UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                    });

                // app.UseHttpsRedirection();
            }
        }
    }
```
```

4.  **Update `TeamsMessageController.cs` to require an authenticated token**:
    Replace the `[ApiKeyAuth]` attribute with `[Authorize]` and `[RequiredScope]`.

```csharp
```csharp EchoBot3\Controllers\TeamsMessageController.cs
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Identity.Web.Resource;
    // ... other using statements

    namespace EchoBot3.Controllers
    {
        [Route("api/[controller]")]
        [ApiController]
        [Authorize] // Require an authenticated user (in this case, a service principal)
        public class TeamsMessageController : ControllerBase
        {
            // Required scope from your App Registration
            private const string SendMessagesScope = "Messages.Send";

            // ... constructor and other methods ...

            [HttpPost("send")]
            [RequiredScope(SendMessagesScope)] // Enforce that the caller has the correct permission
            public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
            {
                // ... existing method body ...
            }

            [HttpPost("send-card")]
            [RequiredScope(SendMessagesScope)] // Protect this endpoint as well
            public async Task<IActionResult> SendCardMessage([FromBody] SendMessageRequest request)
            {
                // ... existing method body ...
            }

            // ... other methods ...
        }
    }
```
```

---

### Part 2: Update the Azure Function (`EchoBot3.Scheduler`)

#### Step 2.1: Enable Managed Identity and Grant Permissions

1.  In the Azure Portal, go to your deployed **Azure Function App**.
2.  Under **Settings**, select **Identity**.
3.  Switch the **Status** to **On** for the "System assigned" identity and click **Save**.
4.  Now, go back to your `EchoBot3-API` **App Registration** in Azure AD.
5.  Go to **API permissions**.
6.  Click **+ Add a permission**.
7.  Select the **APIs my organization uses** tab and search for your `EchoBot3-API` by name.
8.  Select **Delegated permissions**, check the box for `Messages.Send`, and click **Add permissions**.
9.  Finally, click the **Grant admin consent for [Your Tenant]** button to approve the permission.

#### Step 2.2: Update the Function App Project

1.  **Install the necessary NuGet package**:
```powershell
```powershell
    Install-Package Azure.Identity
```
```

2.  **Update `ScheduledNotifier.cs` to use the Managed Identity**:
    We'll use `DefaultAzureCredential` to automatically handle token acquisition.

```csharp
```csharp EchoBot3.Scheduler\ScheduledNotifier.cs
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Identity;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    namespace EchoBot3.Scheduler
    {
        public class ScheduledNotifier
        {
            // ... constructor ...

            [Function("ScheduledNotifier")]
            public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, FunctionContext context)
            {
                var logger = context.GetLogger("ScheduledNotifier");
                logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

                var botApiUrl = _configuration["BotApiUrl"];
                var apiScope = _configuration["BotApiScope"]; // e.g., "api://<client-id>/Messages.Send"

                if (string.IsNullOrEmpty(botApiUrl) || string.IsNullOrEmpty(apiScope))
                {
                    logger.LogError("BotApiUrl or BotApiScope is not configured. Aborting.");
                    return;
                }

                try
                {
                    // 1. Acquire token using Managed Identity
                    var credential = new DefaultAzureCredential();
                    var tokenRequestContext = new TokenRequestContext(new[] { apiScope });
                    var accessToken = await credential.GetTokenAsync(tokenRequestContext);

                    // 2. Make the authenticated call
                    var client = _httpClientFactory.CreateClient();
                    var request = new HttpRequestMessage(HttpMethod.Post, $"{botApiUrl}/api/teamsmessage/send");

                    // Add the token to the Authorization header
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);

                    // Create the JSON body
                    var jsonBody = $@"{{ ... }}"; // Your existing JSON body
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(request);

                    // ... existing response handling ...
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An exception occurred.");
                }
            }
        }
    }
```
```

3.  **Update `local.settings.json` and Azure configuration**:
    You need to add the `BotApiScope` setting.

```json
```json EchoBot3.Scheduler\local.settings.json
    {
      "IsEncrypted": false,
      "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "BotApiUrl": "https://anpteamsechobot2.azurewebsites.net",
        "BotApiScope": "api://YOUR_API_CLIENT_ID/.default", // The scope you created
        "ChatId": "...",
        "ServiceUrl": "...",
        "TenantId": "..."
      }
    }
```
```
    **Important**: For local development, `DefaultAzureCredential` will try to get a token using your Visual Studio or Azure CLI login. Make sure the account you're logged in with has permissions to the API. Also, remember to add `BotApiScope` to your deployed Function App's configuration in the Azure Portal.

This completes the transition from a static API key to a much more secure, secret-free, token-based authentication system using Azure AD.