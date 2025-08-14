-- Suggested schema for a table named [dbo].[Items]
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Items]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[Items](
        [Id] NVARCHAR(256) NOT NULL PRIMARY KEY,
        [Payload] NVARCHAR(MAX) NOT NULL,    -- JSON payload (StorageItem.Data)
        [CreatedUtc] DATETIME2 NOT NULL,
        [UpdatedUtc] DATETIME2 NULL,
        [ETag] ROWVERSION                     -- optimistic concurrency token
    );
END
GO
