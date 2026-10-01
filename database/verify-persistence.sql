-- Execute no Query Editor do Azure SQL após cada etapa do Postman.
SELECT Id, Nome, Email FROM dbo.Usuarios ORDER BY Id;
SELECT t.Id, t.Descricao, t.Valor, t.Data, t.UsuarioId, u.Nome, u.Email
FROM dbo.Transacoes t JOIN dbo.Usuarios u ON u.Id = t.UsuarioId ORDER BY t.Id;
-- Troque pelos IDs retornados no POST para comprovar PUT/DELETE e cascade.
-- SELECT * FROM dbo.Usuarios WHERE Id = <usuarioId>;
-- SELECT * FROM dbo.Transacoes WHERE UsuarioId = <usuarioId>;

