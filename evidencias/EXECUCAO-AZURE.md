# Execução real no Azure — 1 de outubro de 2026

- API: https://dimdim-api-561439-nb261001.azurewebsites.net
- Swagger: https://dimdim-api-561439-nb261001.azurewebsites.net/swagger
- Grupo: `561439-dimdim-rg` (metadados em East US 2).
- Recursos em Mexico Central: SQL Server `dimdim-sql-561439-nb261001-mx`, banco `dimdim-db`, plano B1 `561439-dimdim-plan`, Web App `dimdim-api-561439-nb261001`, Application Insights `561439-dimdim-insights` e workspace `561439-dimdim-logs`.
- Migration aplicada: `20261001224624_InitialCreate`.

East US 2, East US e South Central US recusaram novos servidores SQL com
`RegionDoesNotAllowProvisioning`. Mexico Central permitiu a criação.

O deploy usa ZIP com barras `/`, compatíveis com Linux. Foi corrigido um filtro no
controller de transações: o ID é filtrado antes da projeção do DTO, permitindo que
o EF Core traduza a consulta SQL no POST e no GET por ID.

## Validação

`azure-crud.json` registra a execução concluída em UTC, com dados fictícios:

- GET de saúde e listas: 200.
- POST de usuário e transações: 201 com Location.
- GET dos IDs após reinício da API: 200, com os mesmos registros no SQL.
- PUT de usuário e transação: 204; SQL confirmou nome e valor atualizados.
- DELETE de transação: 204; SQL confirmou a remoção.
- DELETE de usuário com outra transação: 204; SQL confirmou cascade.

Os registros de teste desta execução e da tentativa anterior foram removidos.
`azure-telemetria.json` contém 27 requests e 25 dependências na coleta realizada,
incluindo dependências SQL bem-sucedidas. A telemetria também preserva erros da
primeira versão e sondagens 404 da plataforma; isso não representa o resultado
final do CRUD, registrado separadamente.

`dotnet build --no-restore` passou com zero erros e zero avisos após a correção.
As capturas de tela e o PDF final continuam pendentes.

## Remoção após entrega

Siga [o roteiro de remoção](../azure/REMOVER-APOS-ENTREGA.md) após salvar as
evidências e concluir a entrega. Os recursos permanecem ativos até a exclusão.
