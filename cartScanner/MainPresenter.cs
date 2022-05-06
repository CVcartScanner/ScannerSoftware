using System;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Shell;
using System.Diagnostics;
using System.Windows.Media;

namespace CVcartScanner
{
    class MainPresenter {

        #region Private Fields

        private int cartridgeSize;
        private int chipSize;
        private int cartridgeAddressStart;
        private string commandToBeSent;

        private readonly MainWindow _mainView;

        private byte[] _cartridgeBuffer;
        private int _cartridgeSize;
        public static string SetValueForText1 = "";

        #endregion

        #region Constructor

        public MainPresenter()
        {
            _mainView = new MainWindow();
            
            _mainView.Exit += MainWindow_Exit;
            _mainView.FileSaveAs += MainView_FileSaveAs;
            _mainView.Read32 += MainView_Read32k;
            _mainView.Read64 += MainView_Read64k;
            _mainView.Read64Alt1 += MainView_Read64kAlt1;
            _mainView.Read128 += MainView_Read128k;
            _mainView.Read256 += MainView_Read256k;
            _mainView.Read512 += MainView_Read512k;
            _mainView.InfoDialog += MainView_InfoDialog;
            _mainView.SettingsDialog += MainView_SettingsDialog;
            _mainView.HexDisplay += MainView_HexDisplay;
        }

        #endregion

        #region Public Methods

        public void ShowMainView()
        {
            _mainView.Show();
        }

        #endregion

        #region MainWindow Event Handlers

        private void MainWindow_Exit(object sender, EventArgs e)
        {
            _mainView.Close();
        }

        private void MainView_HexDisplay(object sender, EventArgs e)
        {
            var hexDisplay = new HexDisplay
            {
                Owner = _mainView
            };
            
            hexDisplay.Show();
        }

        private void MainView_FileSaveAs(object sender, RoutedEventArgs e)
        {
            if (_cartridgeBuffer == null)
            {
                MessageBox.Show(_mainView, Properties.Resources.NoCartridgeLoadedMessage,
                    Properties.Resources.NoCartridgeLoadedTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Configure open file dialog box
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                DefaultExt = ".rom",
                Filter = "Save as (*.rom)|*.rom|All Files (*.*)|*.*",
                OverwritePrompt = true
            };

            // Show save file dialog box
            bool? result = dlg.ShowDialog();

            // Process save file dialog box results 
            if (result == true)
            {
                SaveFile(dlg.FileName, false);
            }
        }

        private void MainView_Read32k(object sender, EventArgs e)
        {
            cartridgeSize = 0x8000;
            chipSize = 0x2000;
            cartridgeAddressStart = 0x8000;
            commandToBeSent = Properties.Resources.cReadCommand;
            ProcessRequest();
        }

        private void MainView_Read64k(object sender, EventArgs e)
        {
            cartridgeSize = 0x10000;
            chipSize = 0x4000;
            cartridgeAddressStart = 0x8000;
            commandToBeSent = Properties.Resources.cRead64kCommand;
            ProcessRequest();
        }

        private void MainView_Read64kAlt1(object sender, EventArgs e)
        {
            cartridgeSize = 0x10000;
            chipSize = 0x2000;
            cartridgeAddressStart = 0x8000;
            commandToBeSent = Properties.Resources.cRead64Alt;
            ProcessRequest();
        }

        private void MainView_Read128k(object sender, EventArgs e) 
        {
            cartridgeSize = 0x20000;
            chipSize = 0x2000;
            cartridgeAddressStart = 0x8000;
            commandToBeSent = Properties.Resources.cRead128kCommand;
            ProcessRequest();
        }

        private void MainView_Read256k(object sender, EventArgs e)
        {
            cartridgeSize = 0x40000;
            chipSize = 0x2000;
            cartridgeAddressStart = 0x8000;
            commandToBeSent = Properties.Resources.cRead256kCommmand;
            ProcessRequest();
        }

        private void MainView_Read512k(object sender, EventArgs e)
        {
            cartridgeSize = 0x80000; 
            chipSize = 0x2000;
            cartridgeAddressStart = 0x0;
            commandToBeSent = Properties.Resources.cRead512KCommand;
            ProcessRequest();
        }

        private void MainView_InfoDialog(object sender, EventArgs e)
        {
            var infoDialog = new InfoDialog
            {
                Owner = _mainView
            };

            infoDialog.Show();
        }

        private void MainView_SettingsDialog(object sender, EventArgs e)
        {
            var settingsDialog = new SettingsDialog
            {
                Owner = _mainView
            };

            settingsDialog.ShowDialog();
        }
        #endregion

        #region Private Properties

        private static string ApplicationTitle
        {
            get
            {
                Assembly currentAssembly = Assembly.GetExecutingAssembly();

                var title = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(
                    currentAssembly, typeof(AssemblyTitleAttribute));

                return title.Title;
            }
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Clear Previous Cartridge Data
        /// </summary>
        private void ClearCartridgeData()
        {
            _cartridgeBuffer = null;
            _cartridgeSize = 0;
            _mainView.CartridgeLoaded = false;
            _mainView.Title = ApplicationTitle;
        }

        private void ShowCartridgeData()
        {
            SetValueForText1 = BuildCartridgeData();
            _mainView.CartridgeLoaded = true;
        }

        private string BuildCartridgeData()
        {
            var result = new StringBuilder();

            if (_cartridgeBuffer == null)
            {
                EnableAllButtons();
                throw new InvalidOperationException(Properties.Resources.NoCartridgeFileLoaded);
            }

            for (int index = 0; index < _cartridgeSize; index += 16)
            {
                result.Append(BuildLine(cartridgeAddressStart + index, _cartridgeBuffer.Skip(index).Take(16).ToArray()));
            }

            return result.ToString();
        }

        private static string BuildLine(int address, byte[] data)
        {
            var result = new StringBuilder(80);
            var asciiVersion = new StringBuilder(16);

            result.AppendFormat("${0:X4} : ", address);

            foreach (byte t in data)
            {
                result.Append(t.ToString("X2"));
                result.Append(' ');

                if ((t >= 32) && (t <= 126))
                {
                    asciiVersion.Append(Convert.ToChar(t));
                }
                else
                {
                    asciiVersion.Append('∙');
                }
            }

            if (data.Length < 16)
            {
                for (int missingByte = 0; missingByte < (16 - data.Length); missingByte++)
                {
                    result.Append("   ");
                    asciiVersion.Append(" ");
                }
            }

            result.Append('|');
            result.Append(asciiVersion);
            result.Append('|');
            result.Append(Environment.NewLine);

            return result.ToString();
        }

        private void SaveFile(string filePath, bool isTempFile)
        {
            
            FileStream fileStream = null;

            if (isTempFile)
            {
                filePath = Path.Combine(Path.GetTempPath() + "cartScannerTMP.rom");
            }

            Properties.Settings.Default.TempFile = filePath;
                       
            try
            {
                fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                using (var writer = new BinaryWriter(fileStream))
                {
                    fileStream = null;
                    writer.Write(_cartridgeBuffer, 0, _cartridgeSize);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (fileStream != null)
                {
                    fileStream.Dispose();
                }
            }
        }

        private void ProcessRequest()
        {
            string activePort = Properties.Settings.Default.COMPort;
            if (activePort != null)
            {
                ClearCartridgeData();
                ReadCartridge(new ArduinoSettings
                {
                    SerialPort = activePort,
                    BaudRate = 57600
                });
            }
            else
            {
                EnableAllButtons();
                throw new InvalidOperationException(Properties.Resources.NoSerialPortsMessage);
            }
            
        }

        private void ReadCartridge(ArduinoSettings arduinoSettings)
        {
            var cartridgeReaderBackground = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            cartridgeReaderBackground.DoWork += CartridgeReaderBackground_DoWork;
            cartridgeReaderBackground.ProgressChanged += CartridgeReaderBackground_ProgressChanged;
            cartridgeReaderBackground.RunWorkerCompleted += CartridgeReaderBackground_RunWorkerCompleted;
               
            _mainView.UpdateProgress(0);

            _mainView.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            cartridgeReaderBackground.RunWorkerAsync(arduinoSettings);

        }

        private void CreateEmptyCartridge(int cartridgeClearSize)
        {
            _cartridgeSize = cartridgeClearSize;
            _cartridgeBuffer = new byte[cartridgeClearSize];
        }

        //check to see if the string sent is a hex value, if not, throw error.
        private static byte ParseByte(string currentLine)
        {

            if (!byte.TryParse(currentLine, System.Globalization.NumberStyles.AllowHexSpecifier, null, out byte currentByte))
            {
                throw new InvalidOperationException(
                    string.Format(Properties.Resources.ArduinoUnexpectedValueMessage,
                        "a hexadecimal value", currentLine));
            }

            return currentByte;
        }

        /// <summary>
        /// Removes any blank 8k segments from the end of the cartridge.
        /// </summary>
        private void TruncateCartridge()
        {
            for (int currentChip = 3; currentChip >= 0; currentChip--)
            {
                if (IsChipEmpty(currentChip))
                {
                    _cartridgeSize -= chipSize;
                }
                else
                {
                    break;
                }
            }

            if (_cartridgeSize <= 0)
            {
                EnableAllButtons();
                throw new InvalidOperationException(Properties.Resources.BlankCartridge);
            }
        }

        /// Is the indicated 8k cartridge blank?
        /// </summary>
        /// <param name="chipIndex">
        /// 0 - 3 : Index of the 8k cartridge (0 = 0x8000 chip, 1 = 0xA000 chip, etc.)
        /// </param>
        /// <returns>
        /// true if indicated 8k cartridge is blank, false if it is not blank.
        /// </returns>
        private bool IsChipEmpty(int chipIndex)
        {
            for (int currentByte = 0; currentByte < chipSize; currentByte++)
            {
                int currentChipAddress = (chipSize*chipIndex) + currentByte;
                if (_cartridgeBuffer[currentChipAddress] != 0xFF)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region CartridgeReaderBackground Events
        private void CartridgeReaderBackground_DoWork(object sender, DoWorkEventArgs e)
        {
            if (!(sender is BackgroundWorker worker))
            {
                throw new ArgumentNullException("sender");
            }
            if (!(e.Argument is ArduinoSettings arduinoSettings))
            {
                EnableAllButtons();
                throw new InvalidOperationException("cartScanner settings were not specified.");
            }


            const int cUpdateProgressEvery = 0x0250;


            using (var serialPort = new SerialPort(arduinoSettings.SerialPort, arduinoSettings.BaudRate))
            {
                // Set the read/write timeouts
                serialPort.ReadTimeout = 500;
                serialPort.WriteTimeout = 500;

                serialPort.Open();
                serialPort.DiscardInBuffer();

                // Tell the Arduino to read all of the cartridge.
                serialPort.WriteLine(commandToBeSent);

                var readLine = serialPort.ReadLine().Trim();
                if (!Properties.Resources.cStartMessage.Equals(readLine, StringComparison.InvariantCultureIgnoreCase))
                {
                    EnableAllButtons();
                    throw new InvalidOperationException(
                        string.Format(Properties.Resources.ArduinoUnexpectedValueMessage,
                        Properties.Resources.cStartMessage, readLine));
                }

                CreateEmptyCartridge(cartridgeSize);

                int currentAddress = 0;

                // Verify the Arduino returns the BEGIN: message.
                string currentBlock = serialPort.ReadLine().Trim();
                while (!Properties.Resources.cEndMessage.Equals(currentBlock, StringComparison.InvariantCultureIgnoreCase)
                    && (currentAddress < cartridgeSize))
                {
                    for (int i = 0; i < currentBlock.Length; i += 2)
                    {
                        _cartridgeBuffer[currentAddress++] = ParseByte(currentBlock.Substring(i, 2));
                    }

                    if ((currentAddress % cUpdateProgressEvery) == 0)
                    {
                        // Check for cancel
                        if (worker.CancellationPending)
                        {
                            e.Cancel = true;
                            e.Result = false;
                            return;
                        }

                        // Update Progress Window
                        worker.ReportProgress((int)((currentAddress / (float)cartridgeSize) * 90));
                    }

                    try
                    {
                        currentBlock = serialPort.ReadLine().Trim();
                    }
                    catch
                    {
                        EnableAllButtons();
                        throw new InvalidProgramException("Cartscanner Disconnected");
                    }

                } // while there is still data

                if (!Properties.Resources.cEndMessage.Equals(currentBlock, StringComparison.InvariantCultureIgnoreCase))
                {
                    EnableAllButtons();
                    throw new InvalidOperationException(
                        string.Format(Properties.Resources.ArduinoUnexpectedValueMessage,
                        Properties.Resources.cEndMessage, currentBlock));
                }

                if (currentAddress != cartridgeSize)
                {
                    EnableAllButtons();
                    throw new InvalidOperationException(
                        string.Format(Properties.Resources.UnexpectedCartridgeSize,
                        currentAddress, cartridgeSize));
                }

                // Update Progress Window
                worker.ReportProgress(100);

                TruncateCartridge();

                // Update Progress Window
                worker.ReportProgress(100);

            } // using serialPort

            e.Result = true;
        }

        private String ConvertHexInputToString(String hexString)
        {
            {
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < hexString.Length; i += 2)
                {
                    string hexStringTemp = hexString.Substring(i, 2);
                    stringBuilder.Append(Convert.ToChar(Convert.ToUInt32(hexStringTemp, 16)));
                }

                return stringBuilder.ToString();
            }
        }

        private void CartridgeReaderBackground_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (_mainView != null)
            {
                _mainView.UpdateProgress(e.ProgressPercentage);
            }
            _mainView.TaskbarItemInfo.ProgressValue = e.ProgressPercentage / 100d;
        }

        private void CartridgeReaderBackground_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                _mainView.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
                MessageBox.Show(_mainView, e.Error.Message, Properties.Resources.CartridgeReadErrorTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (e.Cancelled == false)
            {
                ShowCartridgeData();
            }

            // If checkbox isChecked=True then save cartridge data to temp folder then launch using supplied parameters.
            if (Properties.Settings.Default.SaveRunState)
            {
                SaveFile("", true);
                try
                {
                    String emulatorLocation = Properties.Settings.Default.EmulatorLocation + " ";

                    String commandLineOptions = Properties.Settings.Default.CMDOptions; 
                    if (null != commandLineOptions && commandLineOptions != "")
                    {
                        commandLineOptions += " ";
                    }

                    String tempPath = Path.GetTempPath() + "cartScannerTMP.rom";

                    using (System.Diagnostics.Process emulator = new System.Diagnostics.Process())
                    {
                        emulator.StartInfo.FileName = emulatorLocation;
                        emulator.StartInfo.Arguments = commandLineOptions + tempPath;
                        emulator.Start();
                        emulator.WaitForExit();
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show($"Something went wrong starting the emulator.\nEmulator: {Properties.Settings.Default.EmulatorLocation}\n" 
                        + $"Command Line: {Properties.Settings.Default.CMDOptions}\n"+ exception.Message, "Error Starting Emulator", 
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }

            EnableAllButtons();
            _mainView.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
        }

        private void EnableAllButtons()
        {
            _mainView._SettingsButton.IsEnabled = true;
            _mainView._SaveResults.IsEnabled = true;
            _mainView._DisplayOutput.IsEnabled = true;
            _mainView._32kButton.IsEnabled = true;
            _mainView._32kButton.Background = Brushes.LightGray;
            _mainView._32kButton.Opacity = 1;
            _mainView._32kButton.IsHitTestVisible = true;
            _mainView._64kButton.IsEnabled = true;
            _mainView._64kButton.Background = Brushes.LightGray;
            _mainView._64kButton.Opacity = 1;
            _mainView._64kButton.IsHitTestVisible = true;
            _mainView._64kAlternate1.IsEnabled = true;
            _mainView._64kAlternate1.Background = Brushes.LightGray;
            _mainView._64kAlternate1.Opacity = 1;
            _mainView._64kAlternate1.IsHitTestVisible = true;
            _mainView._128kButton.IsEnabled = true;
            _mainView._128kButton.Background = Brushes.LightGray;
            _mainView._128kButton.IsHitTestVisible = true;
            _mainView._128kButton.Opacity = 1;
            _mainView._256kButton.IsEnabled = true;
            _mainView._256kButton.Background = Brushes.LightGray;
            _mainView._256kButton.IsHitTestVisible = true;
            _mainView._256kButton.Opacity = 1;
            _mainView._512kButton.IsEnabled = true;
            _mainView._512kButton.Background = Brushes.LightGray;
            _mainView._512kButton.IsHitTestVisible = true;
            _mainView._512kButton.Opacity = 1;


            _ = new MainWindow();
        }
        #endregion
    }
}
