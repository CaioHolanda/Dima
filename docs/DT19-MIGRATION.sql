BEGIN TRANSACTION;
IF EXISTS (
    SELECT [Slug]
    FROM [Product]
    GROUP BY [Slug]
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 51019, 'DT-19: existem slugs duplicados em Product. Corrija os dados antes de criar UX_Product_Slug.', 1;
END;

CREATE UNIQUE INDEX [UX_Product_Slug] ON [Product] ([Slug]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260917213413_AddUniqueProductSlug', N'10.0.3');

COMMIT;
GO
