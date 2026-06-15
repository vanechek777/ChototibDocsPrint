-- Однократно для существующей БД: квалификация студента для бланка диплома
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Students') AND name = N'Qualification')
BEGIN
    ALTER TABLE dbo.Students ADD Qualification NVARCHAR(200) NULL;
END
GO
