$ErrorActionPreference = 'Stop'
$raiz = Split-Path $PSScriptRoot -Parent
$repo = 'https://github.com/nanabardella/CP-DevOps'
function Html([string]$s) { [System.Net.WebUtility]::HtmlEncode($s) }
$partes = [System.Collections.Generic.List[string]]::new()
$partes.Add(@'
<!doctype html><html lang="pt-BR"><meta charset="utf-8"><title>DimDim — Entrega</title>
<style>
@page { size: A4; margin: 16mm; } body { font-family: Arial,sans-serif; color:#172b42; font-size:11pt; line-height:1.5; }
h1 {font-size:32pt;color:#087e99} h2 {font-size:19pt;color:#087e99} h3 {font-size:14pt}
.page {break-before:page} .cover {padding-top:35mm} a {color:#006b91;overflow-wrap:anywhere}
pre {background:#eff4f7;padding:12px;white-space:pre-wrap;font-size:9pt;color:#172b42;overflow-wrap:anywhere}
img {width:100%;max-height:190mm;object-fit:contain} .caption {font-size:10pt;color:#46566a}
table {width:100%;border-collapse:collapse} td,th {border-bottom:1px solid #ccd8df;text-align:left;padding:7px}
</style><body>
<section class="cover"><h1>DimDim Cloud</h1><h2>Checkpoint — API, Azure SQL e monitoramento</h2>
<p><strong>Grupo: DimDim</strong></p>
<p>RM561439 — Giovanna Bardella Gomes<br>RM566059 — Erick Takeshi Andrade Nakajune</p>
<p>Entrega preparada com evidências de 3 de outubro de 2026.</p>
<p>GitHub: <a href="https://github.com/nanabardella/CP-DevOps">github.com/nanabardella/CP-DevOps</a></p>
<p>API / Swagger:<br><a href="https://dimdim-api-561439-nb261001.azurewebsites.net/swagger">https://dimdim-api-561439-nb261001.azurewebsites.net/swagger</a></p></section>
<section class="page"><h2>1. Arquitetura e banco de dados</h2>
<p>A API financeira usa ASP.NET Core (.NET 9), Entity Framework Core e Azure SQL Database. Os controllers recebem DTOs validados e persistem usuários e transações pelo AppDbContext.</p>
<pre>Swagger / Postman → API no Azure App Service → EF Core → Azure SQL
                              ↓
                       Application Insights

Usuarios.Id (PK) ── 1:N ── Transacoes.UsuarioId (FK)</pre>
<p>Usuarios possui Id, Nome e Email; Transacoes possui Id, Descricao, Valor, Data e UsuarioId. O email é único, o valor usa decimal(18,2) e a FK aplica ON DELETE CASCADE.</p>
<p>Recursos em Mexico Central: grupo 561439-dimdim-rg, Web App dimdim-api-561439-nb261001, SQL Server dimdim-sql-561439-nb261001-mx, banco dimdim-db, plano B1, Application Insights 561439-dimdim-insights e workspace 561439-dimdim-logs.</p>
<h3>Arquivos da implementação</h3>
<ul><li><a href="https://github.com/nanabardella/CP-DevOps/blob/main/README.md">README: instruções completas</a></li>
<li><a href="https://github.com/nanabardella/CP-DevOps/blob/main/azure/deploy-azure.ps1">azure/deploy-azure.ps1: criação e deploy por Azure CLI</a></li>
<li><a href="https://github.com/nanabardella/CP-DevOps/blob/main/database/ddl.sql">database/ddl.sql: DDL</a></li>
<li><a href="https://github.com/nanabardella/CP-DevOps/blob/main/postman/DimDim.postman_collection.json">Coleção Postman</a></li></ul></section>
<section class="page"><h2>2. How-to de implantação</h2>
<ol><li>Instalar SDK .NET 9 e Azure CLI; executar az login e conferir assinatura, políticas e região.</li>
<li>Executar o estágio Provision do script, informando região, nomes únicos e IPv4 de desenvolvimento. A senha SQL é solicitada pelo script.</li>
<li>O script cria grupo, SQL Server, banco Basic, firewall, plano B1, Web App Linux, workspace e Application Insights. Configura DefaultConnection e a telemetria no App Service.</li>
<li>Configurar localmente ConnectionStrings__DefaultConnection com a conexão Azure SQL em memória e aplicar a migration existente.</li>
<li>Executar o estágio Deploy: publicação Release, ZIP e az webapp deploy.</li>
<li>Abrir /swagger, importar a coleção Postman com a URL Azure e executar CRUD; conferir os mesmos IDs no Query Editor e a telemetria no Application Insights.</li></ol>
<pre>dotnet restore
dotnet build
az extension add --name application-insights

.\azure\deploy-azure.ps1 -Stage Provision -LOCATION mexicocentral -SqlServer 'SERVIDOR_UNICO' -WebApp 'WEBAPP_UNICO' -DevelopmentIp 'SEU_IPV4' -EnableSwagger

# Com ConnectionStrings__DefaultConnection configurada em memória:
dotnet ef database update --project .\DimDim.Api --startup-project .\DimDim.Api

.\azure\deploy-azure.ps1 -Stage Deploy -LOCATION mexicocentral -WebApp 'WEBAPP_UNICO'</pre>
<p>Os comandos acima descrevem uma nova implantação. A migration aplicada na execução foi 20261001224624_InitialCreate. A configuração detalhada da conexão está no README; credenciais não fazem parte desta entrega.</p></section>
'@)
$partes.Add('<section class="page"><h2>3. DDL do banco</h2><pre>' + (Html (Get-Content (Join-Path $raiz 'database/ddl.sql') -Raw -Encoding utf8)) + '</pre></section>')
$partes.Add(@'
<section class="page"><h2>4. Leitura das evidências</h2>
<p>As páginas seguintes apresentam as 21 capturas selecionadas. POST retorna 201; GET retorna 200; PUT e DELETE retornam 204. O 404 da transação 3 após o DELETE é esperado.</p>
<p>O primeiro POST de usuário retorna ID 2. A sequência de transações usa a transação 3 vinculada ao usuário 4 (Ana), cuja existência é mostrada no SQL. São registros distintos.</p>
<p>As capturas de requests incluem respostas 400/404 junto com operações bem-sucedidas. O motivo do 400 não é demonstrado pelas imagens. As dependências SQL mostram success=True.</p>
<p>Persistência após reinício e cascade foram validados na execução automatizada registrada em evidencias/azure-crud.json. Os trechos SQL desse registro aparecem ao final. As 21 capturas não comprovam visualmente o reinício ou cascade.</p>
<p>As capturas complementares enviadas no chat mostram usuário 5 e transação 4, mas não foram incorporadas a este arquivo: o registro visual de reinício e a última consulta de cascade não estavam completos.</p></section>
'@)
foreach ($imagem in (Get-ChildItem (Join-Path $PSScriptRoot 'prints-2026-10-03') -Filter '*.png' | Sort-Object Name)) {
    $titulo = $imagem.BaseName -replace '-', ' '
    $partes.Add('<section class="page"><h2>' + (Html $titulo) + '</h2><img src="prints-2026-10-03/' + $imagem.Name + '"><p class="caption">Captura real da execução no Azure em 3 de outubro de 2026.</p></section>')
}
$registro = Get-Content (Join-Path $PSScriptRoot 'azure-crud.json') -Raw -Encoding utf8 | ConvertFrom-Json
$partes.Add('<section class="page"><h2>5. Persistência e cascade — registro de execução</h2><p>Fonte: evidencias/azure-crud.json. Execução registrada em UTC: ' + (Html $registro.executedAtUtc) + '.</p><p>Os resultados abaixo pertencem ao teste automatizado, com IDs próprios; não devem ser confundidos com os IDs da sequência de prints.</p><p><a href="' + $repo + '/blob/main/evidencias/azure-crud.json">Registro JSON completo</a> · <a href="' + $repo + '/blob/main/evidencias/EXECUCAO-AZURE.md">Relato da execução</a></p></section>')
foreach ($resultado in $registro.results) {
    if ($resultado.kind -eq 'sql') {
        $partes.Add('<section class="page"><h2>SQL: ' + (Html $resultado.stage) + '</h2><pre>' + (Html ($resultado | ConvertTo-Json -Depth 15)) + '</pre></section>')
    }
}
$partes.Add('</body></html>')
Set-Content (Join-Path $PSScriptRoot 'DimDim_webapp.html') -Value ($partes -join "`n") -Encoding utf8
Write-Output 'HTML gerado: evidencias/DimDim_webapp.html'

