BEGIN TRANSACTION;
CREATE TABLE [UserSessions] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [LastActivityUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_UserSessions] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260918220500_AddUserSessions', N'10.0.3');

COMMIT;
GO

