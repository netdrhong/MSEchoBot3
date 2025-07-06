$rawChatURL = "https://teams.microsoft.com/l/message/19:9a1b9b0c-9d11-4139-9807-446e5af06585_b07ee26b-4a6f-4b61-9a50-d92c8314bb13@unq.gbl.spaces/1751827017602?context=%7B%22contextType%22%3A%22chat%22%7D"

# 1. Set your variables
$botUrl = "https://anpteamsechobot2.azurewebsites.net" # Or your deployed bot URL: https://your-bot-app.azurewebsites.net
$apiKey = "YOUR_SECURE_API_KEY_GOES_HERE"
$chatId = "19:9a1b9b0c-9d11-4139-9807-446e5af06585_b07ee26b-4a6f-4b61-9a50-d92c8314bb13@unq.gbl.spaces" # Replace with a real Chat ID
$tenantId = "4851961a-a473-455d-840f-221dc8c83528" # Replace with your Tenant ID

# 2. Construct the request URI
$uri = "$botUrl/api/teamsmessage/send"

# 3. Define the request headers
$headers = @{
    "Content-Type" = "application/json"
    "X-Api-Key"    = $apiKey
}

# 4. Create the request body as a PowerShell object
$body = @{
    text       = "Hasdfafasfello from PowerShell! This is a proactive message."
    chatId     = $chatId
    serviceUrl = "https://smba.trafficmanager.net/amer/"
    tenantId   = $tenantId
}

# 5. Convert the body to a JSON string and send the request
Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body ($body | ConvertTo-Json)