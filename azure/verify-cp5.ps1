param([string]$BaseUrl = 'https://dimdim-api-561439-nb261001.azurewebsites.net')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(60)
$docs = Join-Path (Split-Path $PSScriptRoot -Parent) 'docs'
$results = [Collections.Generic.List[object]]::new()
function Request {
    param([string]$Name, [string]$Method, [string]$Path, $Body = $null, [hashtable]$Headers = @{})
    $request = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::new($Method), "$BaseUrl$Path")
    foreach ($key in $Headers.Keys) { $request.Headers.Add($key, $Headers[$key]) }
    if ($null -ne $Body) { $request.Content = New-Object System.Net.Http.StringContent(($Body | ConvertTo-Json), [Text.Encoding]::UTF8, 'application/json') }
    $response = $client.SendAsync($request).GetAwaiter().GetResult()
    $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $responseHeaders = [ordered]@{}
    foreach ($header in $response.Headers) { $responseHeaders[$header.Key] = $header.Value -join ', ' }
    foreach ($header in $response.Content.Headers) { $responseHeaders[$header.Key] = $header.Value -join ', ' }
    $bodyObject = if ($text) { try { ConvertFrom-Json -InputObject $text } catch { $text } } else { $null }
    $record = [pscustomobject]@{ name=$Name; executedAtUtc=[DateTime]::UtcNow.ToString('o'); method=$Method; url="$BaseUrl$Path"; requestHeaders=$Headers; status=[int]$response.StatusCode; headers=$responseHeaders; body=$bodyObject; rawBody=$text }
    $results.Add($record)
    $record | ConvertTo-Json -Depth 20 | Set-Content (Join-Path $docs "$Name.json") -Encoding utf8
    $request.Dispose(); $response.Dispose()
    return $record
}
function Assert-Status($result, [int]$expected) { if ($result.status -ne $expected) { throw "Falhou $($result.name): status $($result.status), esperado $expected" } }
try {
    $user = Request '01-usuario-demo' POST '/api/usuarios' @{ nome='Demo CP5'; email="cp5-$([guid]::NewGuid().ToString('N'))@example.com" }
    Assert-Status $user 201
    $transaction = @{ descricao='Compra CP5'; valor=25.50; data='2026-10-20T12:00:00'; usuarioId=$user.body.id }
    1..5 | ForEach-Object { $transaction.descricao="Compra CP5 $_"; $r=Request "02-seed-$_" POST '/api/transacoes' $transaction; Assert-Status $r 201 }
    $v1 = Request '03-v1-query' GET '/api/transacoes?api-version=1.0'; Assert-Status $v1 200
    if (!$v1.rawBody.TrimStart().StartsWith('[')) { throw 'v1 não é lista.' }
    $header = Request '04-v1-header' GET '/api/transacoes' $null @{ 'X-Api-Version'='1.0' }; Assert-Status $header 200
    if ($v1.rawBody -ne $header.rawBody) { throw 'Contratos query/header diferentes.' }
    if (!$v1.headers['api-supported-versions'].Contains('2.0') -or !$v1.headers['api-deprecated-versions'].Contains('1.0')) { throw 'Headers de versão ausentes.' }
    $default = Request '05-v2-default' GET '/api/transacoes'; Assert-Status $default 200
    if ($default.body.page -ne 1 -or $default.body.pageSize -ne 20) { throw 'Default v2 incorreto.' }
    $page1 = Request '06-v2-pagina1' GET '/api/transacoes?api-version=2.0&page=1&pageSize=2'; Assert-Status $page1 200
    $page2 = Request '07-v2-pagina2' GET '/api/transacoes?api-version=2.0&page=2&pageSize=2'; Assert-Status $page2 200
    if (@($page1.body.items).Count -ne 2 -or @($page2.body.items).Count -ne 2) { throw 'Páginas sem dois itens.' }
    foreach ($item in $page1.body.items) { if ($item.id -in $page2.body.items.id) { throw 'Páginas sobrepostas.' } }
    if ($page1.body.totalPages -ne [Math]::Ceiling($page1.body.totalItems / 2.0)) { throw 'Total de páginas incorreto.' }
    $badPage = Request '08-page-invalida-400' GET '/api/transacoes?page=0'; Assert-Status $badPage 400
    $badSize = Request '09-pageSize-invalido-400' GET '/api/transacoes?pageSize=9999'; Assert-Status $badSize 400
    $huge = Request '10-pagina-vazia-200' GET '/api/transacoes?page=2147483647&pageSize=100'; Assert-Status $huge 200
    if (@($huge.body.items).Count -ne 0) { throw 'Página enorme deve ser vazia.' }
    foreach ($version in @('v1.0','v2.0')) {
        $swagger = Request "11-swagger-$version" GET "/swagger/$version/swagger.json"; Assert-Status $swagger 200
        if (!$swagger.body.paths.'/api/usuarios' -or !$swagger.body.paths.'/api/transacoes') { throw 'Swagger perdeu recursos.' }
    }
    $rateCreated = [Collections.Generic.List[int]]::new()
    $limited = $null
    for ($i=1; $i -le 11; $i++) {
        $transaction.descricao='Teste teto CP5'
        $r = Request "12-rate-tentativa-$i" POST '/api/transacoes' $transaction
        if ($r.status -eq 201) { $rateCreated.Add($r.body.id) }
        elseif ($r.status -eq 429) { $limited=$r; break }
        else { throw "Resposta inesperada no rate limit: $($r.status)" }
    }
    if (!$limited -or !$limited.headers['Retry-After'] -or $limited.body.status -ne 429) { throw '429 com Retry-After/JSON não comprovado.' }
    $health = Request '13-health-apos-429' GET '/health'; Assert-Status $health 200
    $getAfter = Request '14-listagem-apos-429' GET '/api/transacoes?pageSize=2'; Assert-Status $getAfter 200
    foreach ($id in $rateCreated) { $deleted=Request "15-limpeza-rate-$id" DELETE "/api/transacoes/$id"; Assert-Status $deleted 204 }
    $null = Request '16-swagger-ui' GET '/swagger/index.html'
    $config = Request '17-swagger-config' GET '/swagger/index.js'; Assert-Status $config 200
    if (!$config.rawBody.Contains('v1.0') -or !$config.rawBody.Contains('v2.0') -or !$config.rawBody.Contains('DEPRECADA')) { throw 'Seletor Swagger não comprovado.' }
    Write-Output "CP5 validado: versões, paginação, erros, 429 e health. Usuário demo: $($user.body.id)."
} finally {
    $results.ToArray() | ConvertTo-Json -Depth 25 | Set-Content (Join-Path $docs 'execucao-cp5.json') -Encoding utf8
    $client.Dispose()
}
