using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace CVcartScanner
{
    /// <summary>
    /// Interaction logic for SavedDialog.xaml
    /// </summary>
    public partial class SavedDialog
    {
        public SavedDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
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
    }
}
