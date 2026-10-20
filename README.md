# DimDim — CP5: versões, paginação e rate limit

- **RM561439 — Giovanna Bardella Gomes**
- **RM566059 — Erick Takeshi Andrade Nakajune**
- Grupo: DimDim. Domínio financeiro: usuários e transações. SGBD: SQL Server / Azure SQL.
- Repositório: https://github.com/nanabardella/CP-DevOps

Esta evolução mantém CRUD, migrations e telemetria da entrega Azure. A listagem de **transações** ganhou dois contratos. A v1 permanece array e está deprecada; a v2 retorna uma página com totais. Os dois contratos usam o mesmo `TransacaoService`.

## Executar

Pré-requisitos: SDK .NET 9, banco SQL Server ou Azure SQL. Na raiz, em PowerShell:

```powershell
dotnet restore
dotnet build
# Informe a conexão somente em variável de ambiente, sem salvar credenciais no Git:
$env:ConnectionStrings__DefaultConnection = 'SUA_CONEXAO_SQL'
dotnet tool install --global dotnet-ef --version 9.0.10 # apenas se não estiver instalado
dotnet ef database update --project DimDim.Api --startup-project DimDim.Api
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project DimDim.Api --no-launch-profile --urls http://localhost:5000
```

A migration inicial existente é preservada. Para LocalDB, se instalado, use `Server=(localdb)\MSSQLLocalDB;Database=DimDimLocal;Trusted_Connection=True;TrustServerCertificate=True`. O SQL não é substituído por banco em memória na aplicação.

| Recurso | Local | Azure |
| --- | --- | --- |
| Swagger | http://localhost:5000/swagger | https://dimdim-api-561439-nb261001.azurewebsites.net/swagger |
| Health | http://localhost:5000/health | https://dimdim-api-561439-nb261001.azurewebsites.net/health |
| Lista v1 | http://localhost:5000/api/transacoes?api-version=1.0 | https://dimdim-api-561439-nb261001.azurewebsites.net/api/transacoes?api-version=1.0 |
| Lista v2 | http://localhost:5000/api/transacoes?api-version=2.0&page=1&pageSize=2 | https://dimdim-api-561439-nb261001.azurewebsites.net/api/transacoes?api-version=2.0&page=1&pageSize=2 |

`/health` verifica acesso ao banco: 200 com `database: reachable`, ou 503 se indisponível. Sem configuração SQL, Swagger continua acessível e o CRUD responde 503.

## Selecionar versão

Os três modos funcionam na rota `/api/transacoes`:

```http
GET /api/transacoes?api-version=1.0
```

```http
GET /api/transacoes
X-Api-Version: 1.0
```

```http
GET /api/transacoes
```

Omissão usa **2.0**. Query `api-version` e header `X-Api-Version` são os leitores habilitados; não há leitor por segmento URL. Para v2 explícita, use `2.0` em qualquer leitor. As respostas informam `api-supported-versions: 2.0` e `api-deprecated-versions: 1.0`. No Swagger há documentos `v1.0` (DEPRECADA) e `v2.0`, com seletor na UI. Em produção o Swagger depende de `Swagger__Enabled=true`, habilitado no deploy didático.

`GET /api/usuarios` e o CRUD de usuários são `ApiVersionNeutral`, aparecem nos dois documentos e permanecem chamáveis sem versão. GET por ID, POST, PUT e DELETE de transações estão disponíveis em **ambas** as versões; sem versão usam 2.0. O contrato de escrita permanece igual.

## Paginação v2

- `page`: padrão 1, inteiro >= 1.
- `pageSize`: padrão 20, inteiro de 1 a 100. Fora da faixa responde **400**, sem ajuste silencioso.
- Página além do total responde **200** com `items: []`, inclusive `page=2147483647`.
- `totalPages = Ceiling(totalItems / pageSize)`; sem itens, 0 páginas.
- Envelope: `page`, `pageSize`, `totalItems`, `totalPages`, `items`, `hasPrevious`, `hasNext`.
- A v1 ignora parâmetros de paginação e continua retornando a lista inteira.

Exemplo ilustrativo de v2 (os totais reais estão em `docs/`):

```json
{"page":1,"pageSize":2,"totalItems":5,"totalPages":3,"items":[],"hasPrevious":false,"hasNext":true}
```

`PageRequest` em Application valida a faixa. O repositório Infrastructure executa `CountAsync`, `OrderBy(Id)`, `Skip`, `Take` no `IQueryable` antes de `ToListAsync`. `GetById` não pagina. Um offset longo evita overflow; páginas além do total não materializam linhas. Não há `GetAll` seguido de paginação em memória.

## Rate limit

**POST `/api/transacoes`**, em ambas as versões: política nomeada `escrita`, **fixed window de 10 requisições a cada 60 segundos**, fila 0, compartilhada por processo da API. O teto considera chamadas ao endpoint inclusive as que falham na validação; não é por usuário/IP nem distribuído entre instâncias.

A 11ª tentativa na mesma janela responde **429**, `Retry-After` em segundos e JSON Problem Details com `status: 429` e `traceId`. `UseRateLimiter` vem depois de `UseExceptionHandler` e antes de `MapControllers`. GETs, PUTs, DELETEs e o CRUD de usuários não recebem esta política. `/health` tem `DisableRateLimiting`: continua 200 com banco acessível após o teto do POST.

Para testar manualmente no Swagger, crie um usuário e repita POST de transação com seu ID até o 429. Depois chame `/health` e aguarde o tempo de `Retry-After` para novo POST. O teto não interfere no teste das páginas GET.

## Arquitetura

```text
DimDim.Api             Controllers, versões, Swagger, middleware, migrations existentes
      ↓
DimDim.Application     DTOs, serviços compartilhados, PageRequest, envelope, IRepository<T>
      ↓
DimDim.Domain          Usuario, Transacao, regra de precisão monetária, exceções
DimDim.Infrastructure  AppDbContext, EF Core, repositório, consulta paginada SQL
```

A API é a composição e referencia Infrastructure para injetar implementações. Application e Domain não dependem da API nem de EF Core. Controllers não acessam `DbContext`. Os namespaces legados `DimDim.Api.Models`, `DimDim.Api.DTOs` e `DimDim.Api.Data` foram preservados nos arquivos movidos para manter compatibilidade com o snapshot e código existente; a dependência entre projetos é definida pelos `.csproj`, sem referência inversa à API.

O ponto de partida desta evolução era a API Azure, sem projetos de testes ou camadas separadas. Foram adicionados Domain/Application/Infrastructure e seus testes sem alegar que já existiam na entrega anterior. A migration `20261001224624_InitialCreate` e as evidências antigas permanecem preservadas.

## Contratos de escrita e exceções

POST de usuário:

```json
{"nome":"Demo CP5","email":"demo-cp5@example.com"}
```

POST de transação (troque o ID pelo retornado):

```json
{"descricao":"Compra","valor":25.50,"data":"2026-10-20T12:00:00","usuarioId":1}
```

GET por ID: `/api/usuarios/{id}` e `/api/transacoes/{id}`. PUT recebe os mesmos campos do POST; DELETE usa o ID. POST retorna 201 com `Location`; PUT/DELETE retornam 204. Email é único, PK/FK e cascade permanecem no SQL.

| Situação / exceção | HTTP | Resposta |
| --- | --- | --- |
| DTO inválido / ArgumentException (página) / DomainException | 400 | Problem Details com regra e traceId |
| ResourceNotFoundException | 404 | Problem Details |
| ConflictException (email duplicado, SQL 2601/2627) | 409 | Problem Details |
| SQL FK 547 | 400 | DomainException: usuário não existe mais |
| Política de taxa excedida | 429 | Problem Details + Retry-After |
| Conexão não configurada / health sem banco | 503 | Problem Details |
| Exceção inesperada | 500 | Mensagem genérica e traceId; detalhes somente nos logs |

`GlobalExceptionHandler` registra exceções com `traceId`. Requisições também registram método, caminho e status. O SDK Application Insights é ativado quando sua conexão está configurada.

## Testes e evidências

```powershell
dotnet test DimDim.sln
# Teste real contra o Azure, com dados fictícios:
powershell -NoProfile -ExecutionPolicy Bypass -File azure/verify-cp5.ps1
```

- `DimDim.Domain.Tests`: precisão e limites monetários.
- `DimDim.Application.Tests`: intervalos de página, totais, overflow, validação antes do repositório e usuário ausente. Sem API ou banco.
- `DimDim.Api.Tests`: contratos HTTP, query/header/default, Swagger, páginas sem sobreposição, erros, rate limit/health e CRUD/cascade. Usa SQLite relacional isolado somente nos testes.
- [docs/README.md](docs/README.md): índice da execução real no Azure e evidências exigidas.
- [docs/dotnet-test.txt](docs/dotnet-test.txt): saída real de `dotnet test`.
- [database/ddl.sql](database/ddl.sql), [Postman](postman/DimDim.postman_collection.json) e [deploy Azure](azure/deploy-azure.ps1).

O script CP5 cria dados demonstrativos e mantém cinco transações para apresentação; remove apenas as transações adicionais criadas pelo teste do teto. Repetir o script cria outro usuário: os totais globais podem mudar. O Postman antigo de CRUD foi preservado; para lista antiga acrescente `?api-version=1.0`, para paginação use a v2.

## Azure e entrega

Grupo `561439-dimdim-rg`, Mexico Central, SQL Basic, App Service Linux B1, Application Insights e workspace. Custos permanecem enquanto provisionados. O script destrutivo `azure/rebuild-cp5.ps1` só deve ser executado quando a recriação do grupo e seus dados estiver autorizada; não é necessário para rodar localmente nem para avaliar CP5.

O PDF em `evidencias/DimDim_webapp.pdf` documenta a entrega Azure anterior. **A entrega CP5 é o link deste repositório público no portal do aluno**, feita por um integrante. Para remover recursos após avaliação, consulte [o roteiro](azure/REMOVER-APOS-ENTREGA.md).

Referências técnicas: [ASP.NET API Versioning](https://github.com/dotnet/aspnet-api-versioning), [rate limit nativo](https://learn.microsoft.com/aspnet/core/performance/rate-limit?view=aspnetcore-9.0).
