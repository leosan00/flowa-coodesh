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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102011420_InitialCreate'
)
BEGIN
    CREATE TABLE [AssetExposures] (
        [Symbol] nvarchar(16) NOT NULL,
        [Value] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_AssetExposures] PRIMARY KEY ([Symbol])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102011420_InitialCreate'
)
BEGIN
    CREATE TABLE [Outbox] (
        [Id] uniqueidentifier NOT NULL,
        [Type] nvarchar(64) NOT NULL,
        [Key] nvarchar(16) NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        [ProcessedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Outbox] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102011420_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Symbol', N'Value') AND [object_id] = OBJECT_ID(N'[AssetExposures]'))
        SET IDENTITY_INSERT [AssetExposures] ON;
    EXEC(N'INSERT INTO [AssetExposures] ([Symbol], [Value])
    VALUES (N''PETR4'', 0.0),
    (N''VALE3'', 0.0),
    (N''VIIA4'', 0.0)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Symbol', N'Value') AND [object_id] = OBJECT_ID(N'[AssetExposures]'))
        SET IDENTITY_INSERT [AssetExposures] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102011420_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Outbox_ProcessedAtUtc] ON [Outbox] ([ProcessedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102011420_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102011420_InitialCreate', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102155845_AddOrderHistory'
)
BEGIN
    CREATE TABLE [OrderHistory] (
        [EventId] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [Symbol] nvarchar(16) NOT NULL,
        [Side] nvarchar(8) NOT NULL,
        [Quantity] int NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [Accepted] bit NOT NULL,
        [Exposure] decimal(18,2) NOT NULL,
        [Reason] nvarchar(500) NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_OrderHistory] PRIMARY KEY ([EventId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102155845_AddOrderHistory'
)
BEGIN
    CREATE INDEX [IX_OrderHistory_OccurredAtUtc] ON [OrderHistory] ([OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102155845_AddOrderHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102155845_AddOrderHistory', N'10.0.14');
END;

COMMIT;
GO

