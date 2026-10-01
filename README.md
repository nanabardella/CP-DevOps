# DimDim Cloud — RM 561439

API financeira para o checkpoint: usuários e suas transações persistidos em Azure SQL Database, com implantação por Azure CLI e monitoramento no Application Insights.

## Tecnologias e arquitetura

.NET 9, ASP.NET Core Controllers, EF Core 9.0.10, SQL Server/Azure SQL, Swagger (Swashbuckle), OpenAPI, Application Insights e PowerShell/Azure CLI.
Controllers recebem DTOs validados e usam AppDbContext para persistir entidades. DTOs de resposta evitam ciclos JSON.
Usuario **1:N** Transacao: Usuario.Id é PK; Transacao.UsuarioId é FK com exclusão em cascata. Email possui índice UNIQUE; Valor é decimal(18,2). As transações retornam também nome/email do usuário.

```text
DimDim.Api/
  Controllers/       CRUDs
  Data/              AppDbContext
  DTOs/              Contratos e validações
  Models/            Usuario e Transacao
  Migrations/        InitialCreate e snapshot
  Program.cs         Serviços, Swagger e telemetria
azure/               deploy-azure.ps1
database/            ddl.sql e verify-persistence.sql
postman/             Coleção de testes
evidencias/          Roteiro de capturas reais
DimDim.sln
```

## Pré-requisitos

Instale SDK .NET 9, Git, VS Code (extensão C#) e Azure CLI. Para o banco local, SQL Server ou LocalDB instalado separadamente; para a apresentação, Azure SQL e assinatura com permissão de criação. Não há banco em memória.
Execute os comandos abaixo em PowerShell na raiz do repositório:

```powershell
dotnet --version
az version
git --version
dotnet restore
dotnet build
# Apenas se dotnet-ef ainda não estiver instalado:
dotnet tool install --global dotnet-ef --version 9.0.10
dotnet ef --version
```

## Execução local e configuração segura

appsettings.json possui conexão vazia. A API inicia sem SQL; /health e documentação funcionam, enquanto /api retorna 503 sem tentar conexão.
Configure a conexão exclusivamente em variável de ambiente. Não cole senha em comando salvo, arquivo versionado ou conversa.
LocalDB opcional (se instalado):

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=(localdb)\MSSQLLocalDB;Database=DimDimLocal;Trusted_Connection=True;TrustServerCertificate=True'
# Somente para esse banco LOCAL:
dotnet ef database update --project .\DimDim.Api --startup-project .\DimDim.Api
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project .\DimDim.Api --no-launch-profile --urls http://localhost:5000
```

Abra http://localhost:5000/swagger; OpenAPI nativo em /openapi/v1.json e Swagger JSON em /swagger/v1/swagger.json.
GET /health é verificação da aplicação, sem comprovar disponibilidade do banco.

Para Azure SQL, após criar/autorizar infraestrutura, leia a conexão sem exibi-la nem salvá-la:

```powershell
$connectionSecret = Read-Host 'Cole a connection string Azure SQL completa' -AsSecureString
$connectionPtr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($connectionSecret)
try {
    $env:ConnectionStrings__DefaultConnection = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($connectionPtr)
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($connectionPtr)
    $connectionSecret = $null
}
# Execute SOMENTE após autorização para aplicar no Azure:
dotnet ef database update --project .\DimDim.Api --startup-project .\DimDim.Api
# Ao terminar os testes:
Remove-Item Env:\ConnectionStrings__DefaultConnection
```

Formato esperado: Server=tcp:SERVIDOR.database.windows.net,1433;Initial Catalog=dimdim-db;User ID=ADMIN;Password=SENHA;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
Construa a string com SqlConnectionStringBuilder se a senha tiver caracteres especiais.

## Migrations e DDL

InitialCreate já está criada. Não recrie a migration inicial.
Após mudanças futuras no modelo:

```powershell
dotnet ef migrations add NomeDaAlteracao --project .\DimDim.Api --startup-project .\DimDim.Api
dotnet build
dotnet ef migrations list --project .\DimDim.Api --startup-project .\DimDim.Api --no-connect
dotnet ef migrations script 0 InitialCreate --project .\DimDim.Api --output .\database\ddl.sql
```

database/ddl.sql foi gerado pelo EF e inclui tabelas, PKs, FK cascade, índice UNIQUE e histórico de migrations.
Use database update para implantação; o DDL é uma alternativa/evidência. Não execute ambos indiscriminadamente. O DDL inicial destina-se a um banco novo.

## Azure: revisar antes de criar

O script foi executado com autorização em 1 de outubro de 2026; veja [o registro da implantação](evidencias/EXECUCAO-AZURE.md). SQL Basic, App Service B1 e monitoramento podem gerar cobrança/consumir créditos.
Não assume Brazil South e não altera a assinatura.
Confira a assinatura e as políticas herdadas (inclusive management groups) no Portal/Azure Policy; listar regiões não comprova que uma região seja permitida:

```powershell
az account show --query "{name:name,id:id,tenantId:tenantId}" -o table
az account list-locations --query "[].{name:name,displayName:displayName}" -o table
az policy assignment list --disable-scope-strict-match -o json
az webapp list-runtimes --os linux -o table
az extension add --name application-insights
```

Defina LOCATION como uma região permitida para **todos** os serviços e disponível para sua assinatura. Confira disponibilidade das SKUs SQL Basic e App Service B1 nessa região. Se .NET 9 não estiver disponível no App Service, pare e revise a estratégia de runtime antes de criar recursos.
Os nomes SQL Server/Web App precisam ser globalmente únicos; acrescente um sufixo se necessário. Informe seu IPv4 público atual (VPN/proxy pode mudar o IP).

### Comando de criação, somente após autorização

```powershell
$LOCATION = 'REGIAO_PERMITIDA'
.\azure\deploy-azure.ps1 -Stage Provision -LOCATION $LOCATION -SqlServer 'dimdim-sql-561439-SUFIXO' -WebApp '561439-dimdim-api-SUFIXO' -DevelopmentIp 'SEU_IPV4_PUBLICO' -EnableSwagger
```

O script contém os comandos completos: az group create, az sql server create, az sql db create, az sql server firewall-rule create, az appservice plan create, az webapp create, az webapp update, az monitor log-analytics workspace create, az monitor app-insights component create, az webapp config connection-string set e az webapp config appsettings set.

A senha é solicitada com Read-Host -AsSecureString e usada apenas em memória/processo CLI; não use --debug nem transcrição durante essa execução.
A conexão fica no App Service como SQLAzure com nome DefaultConnection, lida pelo ASP.NET Core.
Firewall permite o IP de desenvolvimento e os possibleOutboundIpAddresses do App Service, sem liberar todos os IPs. Mudanças no plano/infraestrutura podem exigir atualizar essas regras.
Falhas interrompem o script, mas recursos já criados permanecem. Revise o grupo no Portal antes de repetir. Não há exclusão automática.

Aplique a migration manualmente com o comando da seção anterior, após autorização, usando o SQL Server/nome/admin escolhidos e o firewall configurado.

### Deploy, somente após autorização e migration

```powershell
.\azure\deploy-azure.ps1 -Stage Deploy -LOCATION $LOCATION -WebApp '561439-dimdim-api-SUFIXO'
```

O script executa dotnet publish em Release, cria ZIP e usa az webapp deploy.
Acesse https://NOME.azurewebsites.net/health e /swagger. Para habilitar Swagger após a criação:

```powershell
az webapp config appsettings set -g 561439-dimdim-rg -n NOME_WEBAPP --settings Swagger__Enabled=true -o none
```

A API deste checkpoint não possui autenticação: use dados fictícios e habilite Swagger para a apresentação. Para encerrar a exposição da documentação, configure Swagger__Enabled=false; os endpoints CRUD continuam públicos.

## CRUD e Postman

Importe postman/DimDim.postman_collection.json. baseUrl começa em http://localhost:5000; altere para https://NOME.azurewebsites.net no Azure.
Execute a coleção em ordem: os POSTs guardam usuarioId/transacaoId automaticamente. As exclusões estão no final.
POST repetido com o mesmo email retorna 409; altere o email ou conclua a exclusão antes de repetir.

| Método | Rota | Resultado |
|---|---|---|
| GET | /api/usuarios e /api/transacoes | 200 com lista |
| GET | /api/usuarios/{id} e /api/transacoes/{id} | 200 ou 404 |
| POST | /api/usuarios e /api/transacoes | 201 com Location e corpo |
| PUT | /api/usuarios/{id} e /api/transacoes/{id} | 204 ou 404 |
| DELETE | /api/usuarios/{id} e /api/transacoes/{id} | 204 ou 404 |

Validação inválida retorna 400. UsuarioId inexistente retorna 400 com mensagem. Email duplicado retorna 409.
Nome/email/descrição são obrigatórios, com limites 100/150/200; email deve ter formato válido, valor até duas casas, data informada e UsuarioId positivo.
Exemplos de corpos:

```json
{"nome":"Giovanna","email":"giovanna@email.com"}
```

```json
{"descricao":"Pagamento faculdade","valor":1500.00,"data":"2026-10-01T19:00:00","usuarioId":1}
```

Use os IDs reais retornados pelo POST. PUT recebe os mesmos campos, sem Id no corpo.

## Persistência e evidências

No Azure SQL Database, abra Query Editor, autentique e execute database/verify-persistence.sql.
Compare IDs e valores após POST/PUT e ausência após DELETE. Reinicie a API e consulte novamente para provar persistência.
Excluir um usuário remove todas as suas transações. Siga evidencias/README.md para capturas antes das exclusões.
**CRUD, persistência após reinício e telemetria foram validados no Azure.** Resultados reais em `evidencias/azure-crud.json` e `evidencias/azure-telemetria.json`; as capturas para o PDF continuam pendentes.

## Application Insights

A aplicação só registra telemetria quando APPLICATIONINSIGHTS_CONNECTION_STRING ou ApplicationInsights:ConnectionString está configurada.
Local sem configuração funciona normalmente. O script configura a variável no Web App; não é necessário ativar outro agente de instrumentação.
Após chamar endpoints, abra o recurso Application Insights: Live Metrics, Failures, Performance e Logs. A ingestão pode levar alguns minutos.
Em Logs no escopo do Application Insights:

```kusto
requests
| where timestamp > ago(30m)
| project timestamp, name, resultCode, success, duration
| order by timestamp desc
```

```kusto
dependencies
| where timestamp > ago(30m)
| where type == "SQL"
| project timestamp, name, success, duration
```

## GitHub e segurança

.gitignore exclui bin, obj, .vs, configurações locais, .env, credenciais e artifacts.
Não versionar senhas, connection strings reais, publish profiles ou capturas com secrets. User Secrets também não são criptografados.
Revise os arquivos antes de adicionar ao Git. O commit `f3f16db` foi confirmado no GitHub; a implantação está publicada, mas as alterações e evidências desta execução ainda precisam de commit/push.
Se ainda não houver repositório, estes comandos são manuais:

```powershell
git init
git status --short
git add .
git diff --cached
git commit -m "Implementa checkpoint DimDim API e preparação Azure"
# Configure remote e faça push somente após sua revisão/autorização.
```

## Referências

- [ASP.NET Core no Azure App Service](https://learn.microsoft.com/en-gb/azure/app-service/configure-language-dotnetcore)
- [Application Insights e workspace](https://learn.microsoft.com/en-us/azure/azure-monitor/app/create-workspace-resource)
- [SDK Application Insights ASP.NET Core](https://github.com/microsoft/ApplicationInsights-dotnet/blob/main/NETCORE/Readme.md)
