# CP5 — evidências e apresentação

Integrantes: RM561439 — Giovanna Bardella Gomes; RM566059 — Erick Takeshi Andrade Nakajune.

O PDF e os prints de `evidencias/` são da entrega Azure anterior. Esta pasta registra a evolução CP5; os arquivos JSON são respostas reais coletadas pelo script `azure/verify-cp5.ps1`, com URL, horário UTC, status, headers e corpo. A saída de `dotnet test` está em [dotnet-test.txt](dotnet-test.txt).

## Como apresentar

1. Abra https://dimdim-api-561439-nb261001.azurewebsites.net/swagger. No seletor, mostre **v1.0 (DEPRECADA)** e **v2.0**.
2. Abra `GET /api/transacoes?api-version=1.0`: resposta lista. Alternativamente, envie `X-Api-Version: 1.0`. Mostre os headers de versões.
3. Abra `/api/transacoes?page=1&pageSize=2` sem versão: envelope da v2. Repita com `page=2` e confira IDs distintos.
4. Use `page=0` ou `pageSize=9999`: 400 com a regra. Use `page=2147483647&pageSize=100`: 200 com items vazio.
5. Crie um usuário e use o ID em `POST /api/transacoes`. Repita até 429; confira Retry-After e o JSON. Em seguida, abra `/health`: 200 com banco acessível. Aguarde a janela antes de repetir POSTs.
6. Mostre os testes com `dotnet test DimDim.sln` ou a saída salva nesta pasta. Domínio/aplicação não precisam de banco; integração usa SQLite isolado de teste.

## Índice da coleta Azure

| Evidência | Arquivo |
| --- | --- |
| v1 como lista, query + headers de versões | [03-v1-query.json](03-v1-query.json) |
| v1 por header | [04-v1-header.json](04-v1-header.json) |
| omissão de versão → v2, padrões 1/20 | [05-v2-default.json](05-v2-default.json) |
| página 1 | [06-v2-pagina1.json](06-v2-pagina1.json) |
| página 2 | [07-v2-pagina2.json](07-v2-pagina2.json) |
| page inválida → 400 | [08-page-invalida-400.json](08-page-invalida-400.json) |
| pageSize inválido → 400 | [09-pageSize-invalido-400.json](09-pageSize-invalido-400.json) |
| página enorme → 200 vazio | [10-pagina-vazia-200.json](10-pagina-vazia-200.json) |
| OpenAPI v1 com descrição DEPRECADA | [11-swagger-v1.0.json](11-swagger-v1.0.json) |
| OpenAPI v2 e recursos preservados | [11-swagger-v2.0.json](11-swagger-v2.0.json) |
| HTML da UI | [16-swagger-ui.json](16-swagger-ui.json) |
| Configuração real da UI com os dois grupos e deprecação | [17-swagger-config.json](17-swagger-config.json) |
| chamadas POST até 429 + Retry-After | arquivos `12-rate-tentativa-*.json` e [registro completo](execucao-cp5.json) |
| health 200 depois do 429 | [13-health-apos-429.json](13-health-apos-429.json) |
| listagem livre depois do 429 | [14-listagem-apos-429.json](14-listagem-apos-429.json) |
| saída real dos testes | [dotnet-test.txt](dotnet-test.txt) |

O teste usa cinco transações iniciais e pageSize=2. Depois coleta o teto de POST e exclui apenas as transações extras dessa etapa. Os totais capturados pertencem ao horário de cada chamada; novos POSTs alteram os totais. A API fica com dados fictícios para apresentação.

O limite é compartilhado por processo, não por IP. Tentativas de outra pessoa no mesmo processo podem consumir a janela; o arquivo de evidência indica exatamente qual chamada retornou 429. GET /health não participa do teto.

A recriação Azure de 20/10 foi solicitada pelo usuário e limitada a `561439-dimdim-rg`. Os testes CRUD/SQL adicionais ficam em `azure-crud-2026-10-20.json` depois de executados. Os arquivos de 19/10 foram preservados em `evidencias/`.

Relatório final: [RELATORIO-2026-10-20.md](RELATORIO-2026-10-20.md). Telemetria: [azure-telemetria-2026-10-20.json](azure-telemetria-2026-10-20.json).
