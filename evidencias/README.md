# Evidências do checkpoint

As evidências reais de Azure SQL/deploy ainda dependem da criação autorizada dos recursos.
Não há resultados simulados nesta pasta.

1. Salve captura do POST de usuário e transação (201, IDs e Location).
2. Execute database/verify-persistence.sql no Azure SQL Query Editor e capture as linhas.
3. Reinicie a API e faça GET dos mesmos IDs: demonstra persistência além da memória.
4. Execute PUT; capture a resposta 204 e a consulta SQL com os novos valores.
5. Execute DELETE da transação; capture 204 e SQL sem aquela linha.
6. Crie outra transação e exclua o usuário: SQL deve mostrar ausência do usuário e suas transações (cascade).
7. Capture Swagger publicado, recursos Azure e Application Insights com requests/dependencies.
8. Registre data, URL, IDs e resultados. Oculte senhas, strings de conexão e dados pessoais.

Execute a coleção uma requisição por vez para capturar o banco antes das exclusões.

