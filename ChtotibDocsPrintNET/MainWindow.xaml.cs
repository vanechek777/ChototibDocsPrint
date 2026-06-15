using System.Windows;
using ChtotibDocsPrintNET.Services;

namespace ChtotibDocsPrintNET
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AppLogoImage.Source = AppBranding.TryLoadLogo();
            Icon = AppBranding.TryLoadWindowIcon();
        }
    }
}
