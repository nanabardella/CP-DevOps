# Prints selecionados — 3 de outubro de 2026

21 capturas copiadas dos arquivos originais, sem alterar as imagens. Use a sequência abaixo para montar o PDF.

| Arquivo | Horário do original |
| --- | --- |
| [01-azure-grupo-recursos.png](01-azure-grupo-recursos.png) | 110919 |
| [02-application-insights-vinculo.png](02-application-insights-vinculo.png) | 111614 |
| [03-azure-sql-servidor.png](03-azure-sql-servidor.png) | 112355 |
| [04-usuarios-get-inicial-vazio-200.png](04-usuarios-get-inicial-vazio-200.png) | 112021 |
| [05-usuarios-post-id2-201.png](05-usuarios-post-id2-201.png) | 112123 |
| [06-usuarios-get-id2-200.png](06-usuarios-get-id2-200.png) | 112153 |
| [07-transacoes-post-id3-201.png](07-transacoes-post-id3-201.png) | 113637 |
| [08-sql-transacao-id3-criada.png](08-sql-transacao-id3-criada.png) | 113655 |
| [09-sql-usuario-id4.png](09-sql-usuario-id4.png) | 113729 |
| [10-transacoes-get-200.png](10-transacoes-get-200.png) | 113809 |
| [11-transacoes-put-id3-204.png](11-transacoes-put-id3-204.png) | 113912 |
| [12-sql-transacao-id3-atualizada.png](12-sql-transacao-id3-atualizada.png) | 113931 |
| [13-transacoes-delete-id3-204.png](13-transacoes-delete-id3-204.png) | 114027 |
| [14-sql-transacao-id3-excluida.png](14-sql-transacao-id3-excluida.png) | 114041 |
| [15-transacoes-consulta-id3-excluido-404.png](15-transacoes-consulta-id3-excluido-404.png) | 114130 |
| [16-usuarios-put-id4-204.png](16-usuarios-put-id4-204.png) | 114249 |
| [17-sql-usuario-id4-atualizado.png](17-sql-usuario-id4-atualizado.png) | 114303 |
| [18-usuarios-delete-id4-204.png](18-usuarios-delete-id4-204.png) | 114336 |
| [19-sql-usuario-id4-excluido.png](19-sql-usuario-id4-excluido.png) | 114353 |
| [20-application-insights-requests.png](20-application-insights-requests.png) | 114525 |
| [21-application-insights-dependencias-sql.png](21-application-insights-dependencias-sql.png) | 114539 |

## Como interpretar

- 201 no POST e 204 no PUT/DELETE são respostas de sucesso. O rótulo Undocumented no Swagger não invalida essas respostas.
- O 404 da captura 15 é esperado após excluir a transação 3; use junto com o DELETE e a consulta SQL vazia.
- A captura 20 contém operações bem-sucedidas e respostas 400/404. Ela comprova coleta de requests; o 404 corresponde à consulta após exclusão. O motivo do 400 não é demonstrado por estes prints.
- A captura 21 mostra dependências SQL com success=True.
- O POST de usuário mostra ID 2, enquanto a sequência da transação usa o usuário 4 (Ana). Não apresente esses registros como o mesmo usuário. A captura 09 comprova o usuário 4 no banco.

## Capturas que não precisam entrar no PDF

- 111015: Activity log de configuração; não comprova requests da API.
- 111100, 111113, 111150 e 111250: telas de navegação, boas-vindas ou histórico vazio.
- 111345 e 111504: consultas de telemetria sem resultados, superadas pelas capturas 20 e 21.
- 111759, 111831 e 111938: telemetria anterior, substituída pelas capturas finais mais completas.
- 112458: servidor SQL repetido; selecionada a captura 112355.
- 113134: tabela de transações vazia antes da criação; não é necessária para demonstrar o CRUD.

## Limites e possíveis complementos

Estes prints não demonstram o reinício da API nem o cascade: a transação 3 foi excluída antes do usuário 4. Para comprovar esses pontos visualmente, adicione capturas específicas, caso exigidas no PDF. Os testes registrados em ../azure-crud.json e ../EXECUCAO-AZURE.md podem complementar o relato, distinguindo-os desta sequência.

Se for exigido CRUD completo em prints para cada entidade, falta a consulta SQL imediatamente após o POST do usuário 2. O POST do usuário 4 também não está neste lote. Uma visão geral do Swagger com a URL publicada pode complementar a apresentação.
