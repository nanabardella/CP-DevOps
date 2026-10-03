# Evidências do checkpoint

Os recursos foram recriados em 3 de outubro de 2026 e a API foi publicada.
`azure-crud.json` registra respostas HTTP e consultas reais do Azure SQL, inclusive
após reinício da API. As capturas em `prints-2026-10-03` comprovam requests e dependências SQL no novo Application Insights; `azure-telemetria.json` preserva a consulta anterior vazia. As evidências de 1 de outubro, incluindo telemetria real,
estão preservadas em `historico-2026-10-01`. Veja `EXECUCAO-AZURE.md`.
21 capturas selecionadas para o PDF estão em [prints-2026-10-03](prints-2026-10-03/README.md), com índice e limites do que demonstram. Não há resultados simulados.

1. Salve captura do POST de usuário e transação (201, IDs e Location).
2. Execute database/verify-persistence.sql no Azure SQL Query Editor e capture as linhas.
3. Reinicie a API e faça GET dos mesmos IDs: demonstra persistência além da memória.
4. Execute PUT; capture a resposta 204 e a consulta SQL com os novos valores.
5. Execute DELETE da transação; capture 204 e SQL sem aquela linha.
6. Crie outra transação e exclua o usuário: SQL deve mostrar ausência do usuário e suas transações (cascade).
7. Capture Swagger publicado, recursos Azure e Application Insights com requests/dependencies.
8. Registre data, URL, IDs e resultados. Oculte senhas, strings de conexão e dados pessoais.

Execute a coleção uma requisição por vez para capturar o banco antes das exclusões.
