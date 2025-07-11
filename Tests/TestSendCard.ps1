﻿
# 1. Set your variables
$botUrl = "https://anpteamsechobot2.azurewebsites.net" # Or your deployed bot URL: https://your-bot-app.azurewebsites.net
$apiKey = "YOUR_SECURE_API_KEY_GOES_HERE"
$chatId = "19:9a1b9b0c-9d11-4139-9807-446e5af06585_b07ee26b-4a6f-4b61-9a50-d92c8314bb13@unq.gbl.spaces" # Replace with a real Chat ID
$channelId="oFCHAwq1Zwq_4COB14fAKOHAlC7xKCNqobm0alAfWRs1"
$tenantId = "TENANTID" # Replace with your Tenant ID

# 2. Construct the request URI for the send-card endpoint
$uri = "$botUrl/api/teamsmessage/send-card"

# 3. Define the request headers
$headers = @{
    "Content-Type" = "application/json"
    "X-Api-Key"    = $apiKey
}

# 4. Create the request body. The 'text' will be displayed inside the Adaptive Card.
$body = @{
    text       = "This text will appear inside the Adaptive Card."
    chatId     = $chatId
    serviceUrl = "https://smba.trafficmanager.net/amer/"
    tenantId   = $tenantId
}

# 5. Convert the body to a JSON string and send the request
Write-Host "Sending Adaptive Card to chat ID: $chatId..."
Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body ($body | ConvertTo-Json)
