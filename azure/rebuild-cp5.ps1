# Recriação destrutiva autorizada pelo usuário em 20/10/2026, restrita ao grupo DimDim.
$ErrorActionPreference = 'Stop'
$resourceGroup = '561439-dimdim-rg'
$webAppName = 'dimdim-api-561439-nb261001'
$serverName = 'dimdim-sql-561439-nb261001-mx'
$expectedSubscription = '4fafca9b-816f-4e61-9cac-4d4de5dede89'
function Check-Native { if ($LASTEXITCODE -ne 0) { throw 'Comando Azure/.NET falhou; execução interrompida.' } }
$subscription = az account show --query id -o tsv
Check-Native
if ($subscription -ne $expectedSubscription) { throw 'Assinatura diferente da autorizada.' }
$resources = az resource list -g $resourceGroup -o json | ConvertFrom-Json
Check-Native
$allowedNames = @($webAppName, $serverName, "$serverName/master", "$serverName/dimdim-db", '561439-dimdim-plan', '561439-dimdim-logs', '561439-dimdim-insights', 'Application Insights Smart Detection')
foreach ($resource in $resources) { if ($resource.name -notin $allowedNames) { throw 'Grupo contém recurso não reconhecido; não será excluído.' } }
# Recupera a senha existente somente em memória; nenhuma credencial é impressa ou gravada.
$oldConnection = az webapp config connection-string list -g $resourceGroup -n $webAppName --query "[?name=='DefaultConnection'].value | [0]" -o tsv
Check-Native
$builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($oldConnection)
if ([string]::IsNullOrWhiteSpace($builder.Password)) { throw 'Conexão SQL existente não encontrada.' }
$sqlSecret = ConvertTo-SecureString $builder.Password -AsPlainText -Force
$oldConnection = $null
$builder = $null
$developmentIp = (Invoke-RestMethod 'https://api.ipify.org').Trim()
Write-Output 'Excluindo apenas 561439-dimdim-rg na assinatura conferida...'
az group delete --name $resourceGroup --yes --no-wait --only-show-errors
Check-Native
do {
    Start-Sleep -Seconds 15
    $exists = az group exists --name $resourceGroup -o tsv
    Check-Native
    Write-Output "Aguardando exclusão: grupo existe=$exists"
} while ($exists -eq 'true')
Write-Output 'Recriando recursos DimDim em Mexico Central...'
& "$PSScriptRoot/deploy-azure.ps1" -Stage Provision -LOCATION mexicocentral -SqlServer $serverName -WebApp $webAppName -DevelopmentIp $developmentIp -SqlPassword $sqlSecret -EnableSwagger
$sqlSecret = $null
try {
    $env:ConnectionStrings__DefaultConnection = az webapp config connection-string list -g $resourceGroup -n $webAppName --query "[?name=='DefaultConnection'].value | [0]" -o tsv
    Check-Native
    dotnet ef database update --project "$PSScriptRoot/../DimDim.Api" --startup-project "$PSScriptRoot/../DimDim.Api"
    Check-Native
} finally { Remove-Item Env:\ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue }
& "$PSScriptRoot/deploy-azure.ps1" -Stage Deploy -LOCATION mexicocentral -WebApp $webAppName
Write-Output 'Recriação, migration e deploy CP5 concluídos.'
