using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace CVcartScanner
{
    /// <summary>
    /// Interaction logic for SavedDialog.xaml
    /// </summary>
    public partial class InfoDialog
    {
        public InfoDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ////ApplicationVersionLabel.Content = "Version " + Assembly.GetExecutingAssembly().GetName().Version;
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
