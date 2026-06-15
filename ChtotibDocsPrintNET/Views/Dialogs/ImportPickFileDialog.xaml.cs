using System.Windows;
using ChtotibDocsPrintNET.Services;
using Microsoft.Win32;

namespace ChtotibDocsPrintNET.Views.Dialogs;

public partial class ImportPickFileDialog : Window
{
    public string? SelectedPath { get; private set; }

    public ImportPickFileDialog(ImportDataKind kind)
    {
        InitializeComponent();
        var hint = ImportFormatHints.Get(kind);
        Title = hint.WindowTitle;
        TbTitle.Text = hint.WindowTitle;
        TbDescription.Text = hint.Description;
        TbSample.Text = hint.SampleTable;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Выберите файл для импорта",
            Filter = "Excel и CSV (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Excel (*.xlsx;*.xls)|*.xlsx;*.xls|CSV (*.csv)|*.csv|Все файлы (*.*)|*.*",
            FileName = string.IsNullOrEmpty(TbPath.Text) ? "" : System.IO.Path.GetFileName(TbPath.Text),
            InitialDirectory = string.IsNullOrEmpty(TbPath.Text)
                ? null
                : System.IO.Path.GetDirectoryName(TbPath.Text),
        };
        if (dlg.ShowDialog(this) != true) return;
        TbPath.Text = dlg.FileName;
        SelectedPath = dlg.FileName;
        BtnImport.IsEnabled = true;
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SelectedPath))
        {
            MessageBox.Show(this, "Сначала выберите файл.", Title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
