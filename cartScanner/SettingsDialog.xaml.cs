using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using System;
using System.Windows.Forms;
using System.IO.Ports;
using Cursors = System.Windows.Input.Cursors;

namespace CVcartScanner
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog
    {
        
        public SettingsDialog()
        {
            InitializeComponent();
            GetSettings();
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

        //Launches windows open file window
        private void Browse_Button_Click(object sender, EventArgs e)
        {

            OpenFileDialog dialog = new OpenFileDialog();
            if (System.Windows.Forms.DialogResult.OK == dialog.ShowDialog())
            {
                Address_Box.Text = dialog.FileName;
            }
        }

        private void Label_Click(object sender, RoutedEventArgs e)
        {
            Check_Box.IsChecked = true;
        }
        //Saves all settings then repopulates the forms by pulling data out of properties.settings and inserts them into the proper textboxes.  Shows confirmation form.
        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Check_Box.IsChecked == true && Address_Box.Text.Length < 1) {
                Check_Box.IsChecked = false;
            }

            SaveSettings();
            GetSettings();
            SavedDialog dialog = new SavedDialog
            {
                Owner = this
            };
            dialog.Show();
        }

        //This retrieves the settings which are controlled by windows and inserts them into the proper text boxes on the form.
        private void GetSettings()
        {
            Address_Box.Text = Properties.Settings.Default.EmulatorLocation;
            Check_Box.IsChecked = Properties.Settings.Default.SaveRunState;
            CmdLineOptions.Text = Properties.Settings.Default.CMDOptions;
            PortTextBox.Text = Properties.Settings.Default.COMPort; 
        }

        //This saves settings to the programs properties.settings file.  Windows handles the rest.
        private void SaveSettings()
        {
            Properties.Settings.Default.CMDOptions = CmdLineOptions.Text;
            Properties.Settings.Default.EmulatorLocation = Address_Box.Text; 
            Properties.Settings.Default.SaveRunState = (bool) Check_Box.IsChecked;
            Properties.Settings.Default.COMPort = PortTextBox.Text;
            Properties.Settings.Default.Save();
        }

        //Tied to the settings dialog form, this method is activated when the 'Detect cartScanner' button is pressed.  It gets a list of active ports from 
        //windows.  If no active ports are found, the port window will return the appropriate message.  If there are active ports in the list, then the
        //port validation method is called to retrieve the cartScanner com port.  If all ports are scanned and none are found, the appropriate message is
        //added to the port text box on the settings form.

        private void PortScanButton_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
           
            var serialPorts = SerialPort.GetPortNames();

            if (serialPorts.Length < 1)
            {
                PortTextBox.Text = "No Active Ports";
            }
            else
            {
                String activePort = ValidateSerialPort(serialPorts);

                if (null != activePort)
                {
                    PortTextBox.Text = activePort;
                }
                else
                {
                    PortTextBox.Text = "Not Found";
                }
            }
            Cursor = Cursors.Arrow; 
        }

        //Receives a string array of active ports on host computer.  Each port is then sent a request for status.  If the cartScanner sees this, it returns
        //the proper status and a break occurs, preventing further ports to be checked.  If a port is written to that isn't the cartscanner, windows throws
        //an exception which is ignored as it isn't the port that we're looking for.  When finished, the activeport is set in settings and can be saved for
        //future scans.
        private String ValidateSerialPort(String[] ports)
        {
            String activePort = null;
            int nextElement = ports.Length - 1;
            for (int a = 0; a <= nextElement; a += 1)
            {
                using (var serialPort = new SerialPort(ports[a], 57600))
                {
                    serialPort.ReadTimeout = 500;
                    serialPort.WriteTimeout = 500;

                    try
                    {
                        serialPort.Open();
                        serialPort.DiscardInBuffer();
                        serialPort.WriteLine(Properties.Resources.cStatus);
                        
                        var readLine = serialPort.ReadLine().Trim();
                        if (Properties.Resources.cReady.Equals(readLine, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return activePort = ports[a];
                            
                        }
                    }
                    catch
                    {
                        
                    }
                    
                    serialPort.Close();
                }

            }
            return activePort;
        }

        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Check_Box.IsChecked == false)
            {
                Check_Box.IsChecked = true;
            }
            else
            {
                Check_Box.IsChecked = false;
            }
        }
    }
}
