# Validação CP5 — 20 de outubro de 2026

A pedido do usuário, somente o grupo `561439-dimdim-rg` da assinatura de Giovanna foi excluído e recriado. SQL Server/banco, App Service Linux B1, Application Insights e workspace foram recriados em Mexico Central. Credencial SQL existente foi reutilizada em memória, sem gravação em arquivos. A migration original foi aplicada e a versão Release do CP5 foi publicada na mesma URL.

- Swagger: https://dimdim-api-561439-nb261001.azurewebsites.net/swagger
- Health: https://dimdim-api-561439-nb261001.azurewebsites.net/health
- Usuário fictício de demonstração: **2**. Cinco transações permanecem para mostrar as páginas.
- Testes locais: **18 aprovados** — 5 de domínio, 10 de aplicação e 3 de integração HTTP. Saída completa em `dotnet-test.txt`. Logs de erros 400/404 na saída correspondem aos cenários negativos esperados; os testes passaram.

## Resultados reais

| Cenário | Resultado |
| --- | --- |
| v1 por query e header | 200, mesmo array, headers de versão |
| sem versão | 200, envelope v2, page=1 e pageSize=20 |
| página 1 / página 2, pageSize=2 | IDs 3/4 e 5/6, sem sobreposição; 5 itens e 3 páginas |
| page=0 / pageSize=9999 | 400, Problem Details com regra |
| page=2147483647&pageSize=100 | 200, items vazio |
| Swagger | v1.0 deprecada e v2.0; usuários e transações nos documentos |
| POST limitado | 429, Retry-After: 60 e JSON status=429 |
| health depois do 429 | 200, database=reachable |
| listagem depois do 429 | 200 |
| CRUD + persistência após reinício | HTTP e consultas SQL confirmaram os mesmos dados |
| cascade | exclusão do usuário 1 removeu suas transações no teste CRUD |
| Application Insights | requests e 55 dependências SQL com success=True no instante da coleta |

O 429 apareceu na quarta tentativa da etapa de carga, porque a política é compartilhada e a janela já tinha sido usada pelos testes anteriores e pela criação dos cinco exemplos. Isso não altera o teto configurado de 10 chamadas/minuto. A janela se recompõe; não é necessário reiniciar a API para liberar chamadas.

O teste CRUD usou o usuário 1 e removeu seus próprios dados. O teste CP5 criou o usuário 2, manteve as cinco transações iniciais e removeu somente as três transações extras do teste de taxa. `estado-final-demo.json` confirma a contagem final. Os IDs não precisam ser consecutivos após exclusões.

Os JSONs incluem horários UTC dos testes e as respostas reais; não são resultados simulados. Os arquivos de 3/10 e 19/10 foram preservados. O PDF anterior não foi refeito: este CP5 pede o repositório com README e evidências em `/docs/`.

Entrega: um integrante envia https://github.com/nanabardella/CP-DevOps no portal do aluno. Os recursos Azure permanecem ativos e consumindo créditos enquanto provisionados; a remoção posterior está documentada em `azure/REMOVER-APOS-ENTREGA.md`.
