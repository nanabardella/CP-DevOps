IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Usuarios] (
    [Id] int NOT NULL IDENTITY,
    [Nome] nvarchar(100) NOT NULL,
    [Email] nvarchar(150) NOT NULL,
    CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
);

CREATE TABLE [Transacoes] (
    [Id] int NOT NULL IDENTITY,
    [Descricao] nvarchar(200) NOT NULL,
    [Valor] decimal(18,2) NOT NULL,
    [Data] datetime2 NOT NULL,
    [UsuarioId] int NOT NULL,
    CONSTRAINT [PK_Transacoes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Transacoes_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Transacoes_UsuarioId] ON [Transacoes] ([UsuarioId]);

CREATE UNIQUE INDEX [IX_Usuarios_Email] ON [Usuarios] ([Email]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261001224624_InitialCreate', N'9.0.10');

COMMIT;
GO

