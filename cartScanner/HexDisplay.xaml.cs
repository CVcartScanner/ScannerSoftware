using System.Windows;

namespace CVcartScanner
{
    /// <summary>
    /// Interaction logic for HexDisplay.xaml
    /// </summary>
    public partial class HexDisplay
    {
        public HexDisplay()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HexDisplayBox.Text = MainPresenter.SetValueForText1;
            
            this.MinHeight = this.ActualHeight;
            this.MinWidth = this.ActualWidth;
            this.MaxWidth = this.ActualWidth;
        }

        private void Button_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
