$rawChannelUrl = "https://teams.microsoft.com/l/channel/19%3AoFCHAwq1Zwq_4COB14fAKOHAlC7xKCNqobm0alAfWRs1%40thread.tacv2/Channel01?groupId=f184fa52-bbb6-4f4d-90bb-a556da6f2a3b&tenantId=4851961a-a473-455d-840f-221dc8c83528"

# 1. Set your variables
$botUrl = "https://anpteamsechobot2.azurewebsites.net" # Or your deployed bot URL: https://your-bot-app.azurewebsites.net
$apiKey = "YOUR_SECURE_API_KEY_GOES_HERE"
$chatId = "19:9a1b9b0c-9d11-4139-9807-446e5af06585_b07ee26b-4a6f-4b61-9a50-d92c8314bb13@unq.gbl.spaces" # Replace with a real Chat ID
$channelId ="19%3AoFCHAwq1Zwq_4COB14fAKOHAlC7xKCNqobm0alAfWRs1%40thread.tacv2"
$tenantId = "4851961a-a473-455d-840f-221dc8c83528" # Replace with your Tenant ID

# 2. Construct the request URI for the send-card endpoint
$uri = "$botUrl/api/teamsmessage/send"

# 3. Define the request headers
$headers = @{
    "Content-Type" = "application/json"
    "X-Api-Key"    = $apiKey
}

# 4. Create the request body. The 'text' will be displayed inside the Adaptive Card.
$body = @{
    text       = "This text will appear inside the chat."
    chatId     = $channelId
    serviceUrl = "https://smba.trafficmanager.net/amer/"
    tenantId   = $tenantId
}

# 5. Convert the body to a JSON string and send the request
Write-Host "Sending Adaptive Card to chat ID: $channelId..."
Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body ($body | ConvertTo-Json)