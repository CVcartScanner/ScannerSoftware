using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace CVcartScanner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Public Events

        public event RoutedEventHandler Exit;
        public event RoutedEventHandler FileSaveAs;
        public event RoutedEventHandler InfoDialog;
        public event RoutedEventHandler SettingsDialog;
        public event RoutedEventHandler Read32;
        public event RoutedEventHandler Read64;
        public event RoutedEventHandler Read64Alt1;
        public event RoutedEventHandler Read128;
        public event RoutedEventHandler Read256;
        public event RoutedEventHandler Read512;
        public event RoutedEventHandler SavedDialog;
        public event RoutedEventHandler HexDisplay;


        #endregion

        #region Public Properties

        public bool CartridgeLoaded
        {

            get
            {
                return _cartridgeLoaded;
            }
            set
            {
                _cartridgeLoaded = value;
            }
        }

        #endregion


        #region Private Fields

        bool _cartridgeLoaded;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            CartridgeLoaded = false;
            _SaveResults.IsEnabled = false;
            _DisplayOutput.IsEnabled = false;
        }

        #region Event Handlers

        private void FileExit_Click(object sender, RoutedEventArgs e)
        {
            Exit?.Invoke(sender, e);
        }


        private void FileSaveAs_Click(object sender, RoutedEventArgs e)
        {
            FileSaveAs?.Invoke(sender, e);
        }

  
        private void Info_Click(object sender, RoutedEventArgs e)
        {
            InfoDialog?.Invoke(sender, e);
        }

        public void UpdateProgress(double value)
        {
            Label1.Content = value + "%";
            ProgressBar.Value = value;
        }

        public void Click_32k(object sender, RoutedEventArgs e)
        {
            if (CheckComPort())
            {
                _32kButton.Background = Brushes.LightBlue;
                DisableAllButtons();
                Read32?.Invoke(sender, e);
            }
        }

        public void Click_64k(object sender, RoutedEventArgs e)
        {
            if (CheckComPort())
            {
                _64kButton.Background = Brushes.LightBlue;
                DisableAllButtons();
                Read64?.Invoke(sender, e);
            }
        }

        public void Click_64kAlt1(object sender, RoutedEventArgs e)
        {
            if (CheckComPort())
            {
                _64kAlternate1.Background = Brushes.LightBlue;
                DisableAllButtons();
                Read64Alt1?.Invoke(sender, e);
            }
        }

        public void Click_128k(object sender, RoutedEventArgs e)
        {
            if (CheckComPort())
            {
                _128kButton.Background = Brushes.LightBlue;
                DisableAllButtons();
                Read128?.Invoke(sender, e);
            }
        }

        public void Click_256k(object sender, RoutedEventArgs e)
        {
            if (CheckComPort())
            {
                _256kButton.Background = Brushes.LightBlue;
                DisableAllButtons();
                Read256?.Invoke(sender, e);
            }
        }

        private void Click_512k(object sender, RoutedEventArgs e)
        {
            if (CheckComPort())
            {
                _512kButton.Background = Brushes.LightBlue;
                DisableAllButtons();
                Read512?.Invoke(sender, e);
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsDialog?.Invoke(sender, e);
        }

        private void SavedDialog_Click(object sender, RoutedEventArgs e)
        {
            SavedDialog?.Invoke(sender, e);
        }

        private void Display_Output_Click(object sender, RoutedEventArgs e)
        {
            HexDisplay?.Invoke(sender, e);
        }

        private void DisableAllButtons()
        {
            _32kButton.IsHitTestVisible = false;
            _32kButton.Opacity = .5;
            _64kButton.IsHitTestVisible = false;
            _64kButton.Opacity = .5;
            _64kAlternate1.IsHitTestVisible = false;
            _64kAlternate1.Opacity = .5;
            _128kButton.IsHitTestVisible = false;
            _128kButton.Opacity = .5;
            _256kButton.IsHitTestVisible = false;
            _256kButton.Opacity = .5;
            _512kButton.IsHitTestVisible = false;
            _512kButton.Opacity = .5;
            _SettingsButton.IsEnabled = false;
            _SaveResults.IsEnabled = false;
            _DisplayOutput.IsEnabled = false;
        }

        private bool CheckComPort()
        {
            bool response = true;
            if (Properties.Settings.Default.COMPort.Length == 0 || Properties.Settings.Default.COMPort.Contains("No")) {
                response = false;
                _ = MessageBox.Show("No COM port set.  Please open settings.", "COM Port Not Set", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            return response;
        }

        #endregion
    }
}
