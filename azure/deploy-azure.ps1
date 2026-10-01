# Execute SOMENTE após revisar os comandos e autorizar os custos.
# Provision cria recursos; Deploy publica em recursos já existentes.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Provision','Deploy')][string]$Stage,
    [Parameter(Mandatory)][string]$LOCATION,
    [string]$ResourceGroup = '561439-dimdim-rg',
    [string]$SqlServer = 'dimdim-sql-561439',
    [string]$Database = 'dimdim-db',
    [string]$Plan = '561439-dimdim-plan',
    [string]$WebApp = '561439-dimdim-api',
    [string]$Insights = '561439-dimdim-insights',
    [string]$Workspace = '561439-dimdim-logs',
    [string]$SqlAdmin = 'dimdimadmin',
    [string]$DevelopmentIp,
    [switch]$EnableSwagger
)
$ErrorActionPreference = 'Stop'
function Invoke-Az {
    param([string[]]$Arguments)
    $result = & az @Arguments --only-show-errors
    if ($LASTEXITCODE -ne 0) { throw "Azure CLI falhou. Revise o erro acima." }
    return $result
}
$root = Split-Path $PSScriptRoot -Parent
# Não muda a assinatura. Confirme az account show e policies antes de executar.
Invoke-Az @('account','show','--query','{name:name,id:id}','-o','table')
if ($Stage -eq 'Provision') {
    if (!$DevelopmentIp) { throw 'Informe -DevelopmentIp com seu IPv4 público atual.' }
    $ip = [System.Net.IPAddress]::Parse($DevelopmentIp)
    if ($ip.AddressFamily -ne [System.Net.Sockets.AddressFamily]::InterNetwork) { throw 'Use IPv4.' }
    $runtimes = Invoke-Az @('webapp','list-runtimes','--os','linux','-o','tsv')
    if (($runtimes -join ' ') -notmatch 'DOTNETCORE[:|]9.0') { throw '.NET 9 não consta nos runtimes disponíveis. Revise antes de criar recursos.' }
    $secret = Read-Host 'Senha do administrador SQL (não será gravada)' -AsSecureString
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
    try {
        $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
        Invoke-Az @('group','create','--name',$ResourceGroup,'--location',$LOCATION,'-o','none')
        Invoke-Az @('sql','server','create','-g',$ResourceGroup,'-n',$SqlServer,'-l',$LOCATION,'-u',$SqlAdmin,'-p',$password,'-o','none')
        Invoke-Az @('sql','db','create','-g',$ResourceGroup,'-s',$SqlServer,'-n',$Database,'--service-objective','Basic','-o','none')
        Invoke-Az @('sql','server','firewall-rule','create','-g',$ResourceGroup,'-s',$SqlServer,'-n','DevelopmentIp','--start-ip-address',$DevelopmentIp,'--end-ip-address',$DevelopmentIp,'-o','none')
        Invoke-Az @('appservice','plan','create','-g',$ResourceGroup,'-n',$Plan,'-l',$LOCATION,'--is-linux','--sku','B1','-o','none')
        Invoke-Az @('webapp','create','-g',$ResourceGroup,'-n',$WebApp,'--plan',$Plan,'--runtime','DOTNETCORE:9.0','-o','none')
        Invoke-Az @('webapp','update','-g',$ResourceGroup,'-n',$WebApp,'--https-only','true','-o','none')
        # Permite apenas os IPs de saída possíveis deste App Service, sem regra 0.0.0.0.
        $outbound = Invoke-Az @('webapp','show','-g',$ResourceGroup,'-n',$WebApp,'--query','possibleOutboundIpAddresses','-o','tsv')
        if (!$outbound) { throw 'Não foi possível obter os IPs de saída do Web App.' }
        $index = 0
        foreach ($address in ($outbound -split ',' | Select-Object -Unique)) {
            Invoke-Az @('sql','server','firewall-rule','create','-g',$ResourceGroup,'-s',$SqlServer,'-n',"AppService-$index",'--start-ip-address',$address,'--end-ip-address',$address,'-o','none')
            $index++
        }
        Invoke-Az @('monitor','log-analytics','workspace','create','-g',$ResourceGroup,'-n',$Workspace,'-l',$LOCATION,'--retention-time','30','-o','none')
        $workspaceId = Invoke-Az @('monitor','log-analytics','workspace','show','-g',$ResourceGroup,'-n',$Workspace,'--query','id','-o','tsv')
        # Requer extensão application-insights; instalação documentada no README.
        Invoke-Az @('monitor','app-insights','component','create','-g',$ResourceGroup,'--app',$Insights,'-l',$LOCATION,'--application-type','web','--workspace',$workspaceId,'-o','none')
        $telemetry = Invoke-Az @('monitor','app-insights','component','show','-g',$ResourceGroup,'--app',$Insights,'--query','connectionString','-o','tsv')
        $csb = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
        $csb['Data Source'] = "tcp:$SqlServer.database.windows.net,1433"
        $csb['Initial Catalog'] = $Database
        $csb['User ID'] = $SqlAdmin
        $csb['Password'] = $password
        $csb['Encrypt'] = $true
        $csb['TrustServerCertificate'] = $false
        $csb['Connect Timeout'] = 30
        Invoke-Az @('webapp','config','connection-string','set','-g',$ResourceGroup,'-n',$WebApp,'--connection-string-type','SQLAzure','--settings',"DefaultConnection=$($csb.ConnectionString)",'-o','none')
        $swaggerValue = $EnableSwagger.IsPresent.ToString().ToLowerInvariant()
        Invoke-Az @('webapp','config','appsettings','set','-g',$ResourceGroup,'-n',$WebApp,'--settings',"APPLICATIONINSIGHTS_CONNECTION_STRING=$telemetry","Swagger__Enabled=$swaggerValue",'ASPNETCORE_ENVIRONMENT=Production','-o','none')
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
        $password = $null; $secret = $null; $csb = $null
    }
    Write-Host 'Recursos preparados. Aplique a migration manualmente após autorização, antes de Deploy.'
} else {
    $publish = Join-Path $root 'artifacts/publish'
    $zip = Join-Path $root 'artifacts/dimdim.zip'
    # Diretório novo por execução evita arquivos antigos no pacote.
    $publish = "$publish-$(Get-Date -Format yyyyMMddHHmmssfff)"
    & dotnet publish (Join-Path $root 'DimDim.Api/DimDim.Api.csproj') -c Release -o $publish
    if ($LASTEXITCODE -ne 0) { throw 'Publish falhou.' }
    Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $zip -Force
    Invoke-Az @('webapp','deploy','-g',$ResourceGroup,'-n',$WebApp,'--src-path',$zip,'--type','zip','-o','none')
    Write-Host "API: https://$WebApp.azurewebsites.net"
}

