namespace ChtotibDocsPrintNET.Services;

public readonly record struct PdfGenerationProgress(int Current, int Total, string Message);
