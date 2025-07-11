#Client Id: 55d0f5e9-be6e-4d7c-9456-9a3d3b6a81be
#Key: 
$rawChatURL = "https://teams.microsoft.com/l/message/19:9a1b9b0c-9d11-4139-9807-446e5af06585_b07ee26b-4a6f-4b61-9a50-d92c8314bb13@unq.gbl.spaces/1751827017602?context=%7B%22contextType%22%3A%22chat%22%7D"

# 1. Set your variables
$botUrl = "https://anpteamsechobot2.azurewebsites.net" # Or your deployed bot URL: https://your-bot-app.azurewebsites.net
$apiKey = "TA22F532AF1E3BEBC52B15F1B4FA484UO"
$chatId = "19:9a1b9b0c-9d11-4139-9807-446e5af06585_b07ee26b-4a6f-4b61-9a50-d92c8314bb13@unq.gbl.spaces" # Replace with a real Chat ID
$tenantId = "TENANTID" # Replace with your Tenant ID

# 2. Construct the request URI
$uri = "$botUrl/api/teamsmessage/send"

# 3. Define the request headers
#$headers = @{
#    "Content-Type" = "application/json"
#    "X-Api-Key"    = $apiKey
#}

# 4. Create the request body as a PowerShell object
$body = @{
    text       = "Hasdfafasfello from PowerShell! This is a proactive message."
    chatId     = $chatId
    serviceUrl = "https://smba.trafficmanager.net/amer/"
    tenantId   = $tenantId
}

# 5. Convert the body to a JSON string and send the request
#Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body ($body | ConvertTo-Json)


# 1. --- Configuration: Fill in these details ---

# Azure AD and Test Client Details
$tenantId = "4851961a-a473-455d-840f-221dc8c83528"
$clientId = "55d0f5e9-be6e-4d7c-9456-9a3d3b6a81be"         # The Application (client) ID of EchoBot3-TestClient
$clientSecret = "client secret"      # The secret value you copied for EchoBot3-TestClient

# API and Target Channel Details
#$botApiUrl = "http://localhost:3978"           # Your bot's deployed URL
$apiScope = "api://25a3e11b-149c-47a3-9a5a-61c51125447f/.default"   # The Application ID URI of EchoBot3-API, with /.default scope
#$chatId = "19:xxxxxxxxxxxxxxxxxxxx@thread.tacv2"
$serviceUrl = "https://smba.trafficmanager.net/amer/"

# 2. --- Authenticate and Get Access Token ---

Write-Host "Requesting access token from Azure AD..."

$tokenEndpoint = "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token"

$tokenBody = @{
    client_id     = $clientId
    scope         = $apiScope
    client_secret = $clientSecret
    grant_type    = "client_credentials"
}

try {
    $tokenResponse = Invoke-RestMethod -Uri $tokenEndpoint -Method Post -Body $tokenBody
    $accessToken = $tokenResponse.access_token
    Write-Host "Successfully acquired access token."
} catch {
    Write-Host "Error acquiring access token: $($_.Exception.Message)"
    return
}

echo $accessToken
# 3. --- Call the Secured API Endpoint ---

Write-Host "Calling the secured API endpoint..."

$uri = "$botUrl/api/teamsmessage/send"

$headers = @{
    "Authorization" = "Bearer $accessToken"
    "Content-Type"  = "application/json"
}

$body = @{
    text       = "This message was sent using Azure AD authentication."
    chatId     = $chatId
    serviceUrl = $serviceUrl
    tenantId   = $tenantId
}
echo ($body |ConvertTo-Json)
echo $uri

try {
    $apiResponse = Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body ($body | ConvertTo-Json)
    Write-Host "Success! API responded:"
    $apiResponse | ConvertTo-Json
} catch {
    # --- Improved Error Handling Block ---
    Write-Host "An error occurred during the API call."
    
    # Check if there is a response object in the exception
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        $responseStream = $_.Exception.Response.GetResponseStream()
        $streamReader = New-Object System.IO.StreamReader($responseStream)
        $responseBody = $streamReader.ReadToEnd()
        
        Write-Host "HTTP Status Code: $statusCode"
        Write-Host "Response Body: $responseBody"
    } else {
        # If there's no response, just print the exception message
        Write-Host "Exception Message: $($_.Exception.Message)"
    }
}