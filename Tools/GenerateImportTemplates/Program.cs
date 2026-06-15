using ClosedXML.Excel;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var samplesDir = Path.Combine(root, "ImportSamples");
if (!Directory.Exists(samplesDir))
{
    Console.Error.WriteLine($"Папка не найдена: {samplesDir}");
    return 1;
}

var csvFiles = new (string CsvName, string SheetName, string XlsxName)[]
{
    ("1_Специальности.csv", "Специальности", "1_Специальности.xlsx"),
    ("2_Группы.csv", "Группы", "2_Группы.xlsx"),
    ("3_Предметы.csv", "Предметы", "3_Предметы.xlsx"),
    ("4_Студенты.csv", "Студенты", "4_Студенты.xlsx"),
};

using var combined = new XLWorkbook();

foreach (var (csvName, sheetName, xlsxName) in csvFiles)
{
    var csvPath = Path.Combine(samplesDir, csvName);
    if (!File.Exists(csvPath))
    {
        Console.Error.WriteLine($"Пропуск — нет файла: {csvPath}");
        continue;
    }

    var rows = ReadCsv(csvPath);
    WriteSheet(combined, sheetName, rows);
    using var single = new XLWorkbook();
    WriteSheet(single, "Лист1", rows);
    var outPath = Path.Combine(samplesDir, xlsxName);
    single.SaveAs(outPath);
    Console.WriteLine($"Создан: {outPath}");
}

var combinedPath = Path.Combine(samplesDir, "Импорт_ЧТОТиБ.xlsx");
combined.SaveAs(combinedPath);
Console.WriteLine($"Создан: {combinedPath}");
return 0;

static List<string[]> ReadCsv(string path)
{
    var result = new List<string[]>();
    foreach (var line in File.ReadAllLines(path, System.Text.Encoding.UTF8))
    {
        if (string.IsNullOrWhiteSpace(line)) continue;
        result.Add(line.Split(';').Select(c => c.Trim()).ToArray());
    }
    return result;
}

static void WriteSheet(XLWorkbook wb, string sheetName, List<string[]> rows)
{
    var ws = wb.Worksheets.Add(sheetName);
    for (var r = 0; r < rows.Count; r++)
    {
        for (var c = 0; c < rows[r].Length; c++)
            ws.Cell(r + 1, c + 1).Value = rows[r][c];
    }
    ws.Columns().AdjustToContents();
    ws.SheetView.FreezeRows(1);
}
