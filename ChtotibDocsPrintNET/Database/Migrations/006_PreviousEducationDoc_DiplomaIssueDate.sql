USE ChtotibDocPrint;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Students') AND name = N'PreviousEducationDoc')
    ALTER TABLE dbo.Students ADD PreviousEducationDoc NVARCHAR(300) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Diplomas') AND name = N'IssueDate')
    ALTER TABLE dbo.Diplomas ADD IssueDate DATE NULL;
GO
