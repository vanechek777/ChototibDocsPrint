-- Упрощение схемы: только поля для печати дипломов и приложений (идемпотентно).
USE ChtotibDocPrint;
GO

DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE dbo.Students DROP COLUMN [' + c.name + N'];'
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'dbo.Students')
  AND c.name IN (
    N'SNILS', N'Phone', N'Email', N'Gender',
    N'PassportSeries', N'PassportNumber', N'PassportIssueDate', N'PassportIssuedBy', N'PassportCode',
    N'PreviousEducationDate',
    N'ExpelledDate', N'ExpelledReason', N'Notes'
  );
IF LEN(@sql) > 0 EXEC sp_executesql @sql;
GO

DECLARE @sql2 NVARCHAR(MAX) = N'';
SELECT @sql2 += N'ALTER TABLE dbo.Groups DROP COLUMN [' + c.name + N'];'
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'dbo.Groups')
  AND c.name IN (
    N'CuratorName', N'GraduationDate',
    N'DemoExamDate1', N'DemoExamDate2', N'DiplomaDefenseDate1', N'DiplomaDefenseDate2'
  );
IF LEN(@sql2) > 0 EXEC sp_executesql @sql2;
GO

DECLARE @sql3 NVARCHAR(MAX) = N'';
SELECT @sql3 += N'ALTER TABLE dbo.Diplomas DROP COLUMN [' + c.name + N'];'
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'dbo.Diplomas')
  AND c.name IN (
    N'RegistrationNumber', N'CommissionChairman', N'DirectorName',
    N'PrintedDate', N'SupplementIsPrinted', N'SupplementPrintedDate', N'Notes'
  );
IF LEN(@sql3) > 0 EXEC sp_executesql @sql3;
GO

PRINT N'Схема упрощена (005_SlimSchema).';
GO
