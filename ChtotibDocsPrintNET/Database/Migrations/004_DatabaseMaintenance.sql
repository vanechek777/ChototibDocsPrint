-- Идемпотентная очистка «мусора» (сиротские связи). Можно выполнить в SSMS или через «Очистить БД» в настройках.
USE ChtotibDocPrint;
GO

DELETE FROM Grades WHERE StudentId NOT IN (SELECT Id FROM Students);
DELETE FROM Grades WHERE SubjectId NOT IN (SELECT Id FROM Subjects);
DELETE FROM Diplomas WHERE StudentId NOT IN (SELECT Id FROM Students);
DELETE FROM PrintHistory WHERE StudentId NOT IN (SELECT Id FROM Students);
GO
