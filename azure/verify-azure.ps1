# Teste real com dados fictícios; remove somente os registros criados nesta execução.
[CmdletBinding()]
param(
    [string]$ResourceGroup = '561439-dimdim-rg',
    [string]$WebApp = 'dimdim-api-561439-nb261001'
)
$ErrorActionPreference = 'Stop'
$baseUrl = "https://$WebApp.azurewebsites.net"
$results = [Collections.Generic.List[object]]::new()
$connection = $null
$sql = $null
function Record-Http {
    param([string]$Method, [string]$Path, $Body, [int]$Expected)
    $arguments = @{ Uri = "$baseUrl$Path"; Method = $Method; UseBasicParsing = $true; TimeoutSec = 60 }
    if ($null -ne $Body) {
        $arguments.ContentType = 'application/json'
        $arguments.Body = [Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json))
    }
    $response = Invoke-WebRequest @arguments
    if ([int]$response.StatusCode -ne $Expected) { throw "Resposta inesperada em $Method $Path" }
    $content = if ($response.Content) { $response.Content | ConvertFrom-Json } else { $null }
    $results.Add([pscustomobject]@{ kind = 'http'; method = $Method; path = $Path; status = [int]$response.StatusCode; body = $content; location = $response.Headers['Location'] })
    return $content
}
function Record-Sql {
    param([int]$UserId, [string]$Stage)
    $command = $sql.CreateCommand()
    $command.CommandText = 'SELECT Id, Nome, Email FROM Usuarios WHERE Id=@id; SELECT Id, Descricao, Valor, Data, UsuarioId FROM Transacoes WHERE UsuarioId=@id;'
    $null = $command.Parameters.AddWithValue('@id', $UserId)
    $reader = $command.ExecuteReader()
    try {
        $sets = @()
        do {
            $rows = @()
            while ($reader.Read()) {
                $row = [ordered]@{}
                for ($i = 0; $i -lt $reader.FieldCount; $i++) { $row[$reader.GetName($i)] = $reader.GetValue($i) }
                $rows += [pscustomobject]$row
            }
            $sets += ,$rows
        } while ($reader.NextResult())
        $results.Add([pscustomobject]@{ kind = 'sql'; stage = $Stage; usuarios = $sets[0]; transacoes = $sets[1] })
    } finally { $reader.Dispose(); $command.Dispose() }
}
try {
    $connection = az webapp config connection-string list -g $ResourceGroup -n $WebApp --query "[?name=='DefaultConnection'].value | [0]" -o tsv
    if ($LASTEXITCODE -ne 0 -or !$connection) { throw 'Conexão indisponível.' }
    $sql = New-Object System.Data.SqlClient.SqlConnection $connection
    $sql.Open()
    $null = Record-Http GET '/health' $null 200
    $null = Record-Http GET '/api/usuarios' $null 200
    $null = Record-Http GET '/api/transacoes' $null 200
    $userBody = @{ nome = 'Teste checkpoint'; email = "checkpoint-$([guid]::NewGuid().ToString('N'))@example.com" }
    $user = Record-Http POST '/api/usuarios' $userBody 201
    $transactionBody = @{ descricao = 'Teste de persistencia'; valor = 25.50; data = [DateTime]::UtcNow.ToString('o'); usuarioId = $user.id }
    $transaction = Record-Http POST '/api/transacoes' $transactionBody 201
    Record-Sql $user.id 'apos POST'
    az webapp restart -g $ResourceGroup -n $WebApp --only-show-errors
    if ($LASTEXITCODE -ne 0) { throw 'Reinício da API falhou.' }
    $ready = $false
    for ($attempt = 0; $attempt -lt 24; $attempt++) {
        try {
            $health = Invoke-WebRequest "$baseUrl/health" -UseBasicParsing -TimeoutSec 10
            if ($health.StatusCode -eq 200) { $ready = $true; break }
        } catch { Start-Sleep -Seconds 3 }
    }
    if (!$ready) { throw 'API não respondeu após reinício.' }
    $null = Record-Http GET "/api/usuarios/$($user.id)" $null 200
    $null = Record-Http GET "/api/transacoes/$($transaction.id)" $null 200
    Record-Sql $user.id 'apos reinicio da API'
    $userBody.nome = 'Teste atualizado'
    $null = Record-Http PUT "/api/usuarios/$($user.id)" $userBody 204
    $transactionBody.valor = 42.75
    $null = Record-Http PUT "/api/transacoes/$($transaction.id)" $transactionBody 204
    Record-Sql $user.id 'apos PUT'
    $null = Record-Http DELETE "/api/transacoes/$($transaction.id)" $null 204
    Record-Sql $user.id 'apos DELETE transacao'
    $null = Record-Http POST '/api/transacoes' $transactionBody 201
    $null = Record-Http DELETE "/api/usuarios/$($user.id)" $null 204
    Record-Sql $user.id 'apos DELETE usuario com cascade'
} finally {
    if ($sql) { $sql.Dispose() }
    $connection = $null
    $output = Join-Path (Split-Path $PSScriptRoot -Parent) 'evidencias/azure-crud.json'
    [pscustomobject]@{ executedAtUtc = [DateTime]::UtcNow.ToString('o'); baseUrl = $baseUrl; results = $results.ToArray() } |
        ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $output -Encoding UTF8
}
Write-Host "CRUD e consultas SQL concluídos. Evidências: $output"
