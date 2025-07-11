# Login to Azure
az login

# Get your Function App's principal ID
$FUNCTION_APP_PRINCIPAL_ID=$(az functionapp identity show --name "echobot3-scheduler-app" --resource-group "anpfunctionechoappplan" --query principalId -o tsv)

# Get your API's service principal object ID
$API_SP_OBJECT_ID=$(az ad sp list --filter "appId eq '25a3e11b-149c-47a3-9a5a-61c51125447f'" --query "[0].id" -o tsv)

# Get the app role ID
$APP_ROLE_ID=$(az ad sp show --id $API_SP_OBJECT_ID --query "appRoles[?value=='Messages.Send.App'].id" -o tsv)

# Assign the role
az rest --method POST --uri "https://graph.microsoft.com/v1.0/servicePrincipals/15ad8d3a-2a64-4d9e-89e2-c31bdf9b4235/appRoleAssignments" --body "{\"principalId\":\"15ad8d3a-2a64-4d9e-89e2-c31bdf9b4235\",\"resourceId\":\"$API_SP_OBJECT_ID\",\"appRoleId\":\"$APP_ROLE_ID\"}"

az rest --method POST --uri "https://graph.microsoft.com/v1.0/servicePrincipals/15ad8d3a-2a64-4d9e-89e2-c31bdf9b4235/appRoleAssignments" --headers "Content-Type=application/json" --body ('{"principalId":"15ad8d3a-2a64-4d9e-89e2-c31bdf9b4235","resourceId":"ea47faf2-c8dc-491f-a82c-2869234b4c8f","appRoleId":"3dad57a6-8209-46a2-b1c5-a5530dd2a854"}' | ConvertTo-Json) --verbose

