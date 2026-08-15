using System;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Media;

namespace CVcartScanner
{
    class MainPresenter {

        #region Private Fields

        private int cartridgeSize;
        private int chipSize;
        private int cartridgeAddressStart;
        private string commandToBeSent;
        private bool binaryTransfer;
        private bool truncateBlankTail;

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
            _mainView.ReadSgc128 += MainView_ReadSgc128;
            _mainView.ReadSgc256 += MainView_ReadSgc256;
            _mainView.ReadSgc512 += MainView_ReadSgc512;
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
            StartRead(0x8000, 0x2000, 0x8000, Properties.Resources.cReadCommand, true);
        }

        private void MainView_Read64k(object sender, EventArgs e)
        {
            StartRead(0x10000, 0x4000, 0x8000, Properties.Resources.cRead64kCommand, false);
        }

        private void MainView_Read64kAlt1(object sender, EventArgs e)
        {
            StartRead(0x10000, 0x2000, 0x8000, Properties.Resources.cRead64Alt, false);
        }

        private void MainView_Read128k(object sender, EventArgs e) 
        {
            StartRead(0x20000, 0x2000, 0x8000, Properties.Resources.cRead128kCommand, false);
        }

        private void MainView_Read256k(object sender, EventArgs e)
        {
            StartRead(0x40000, 0x2000, 0x8000, Properties.Resources.cRead256kCommmand, false);
        }

        private void MainView_Read512k(object sender, EventArgs e)
        {
            StartRead(0x80000, 0x2000, 0x0, Properties.Resources.cRead512KCommand, false);
        }

        private void MainView_ReadSgc128(object sender, EventArgs e)
        {
            StartRead(0x20000, 0x2000, 0x0, Properties.Resources.cRead128kSgcCommand, false);
        }

        private void MainView_ReadSgc256(object sender, EventArgs e)
        {
            StartRead(0x40000, 0x2000, 0x0, Properties.Resources.cRead256kSgcCommand, false);
        }

        private void MainView_ReadSgc512(object sender, EventArgs e)
        {
            StartRead(0x80000, 0x2000, 0x0, Properties.Resources.cRead512kSgcCommand, false);
        }

        private void StartRead(int size, int segmentSize, int displayAddress, string command, bool truncate)
        {
            cartridgeSize = size;
            chipSize = segmentSize;
            cartridgeAddressStart = displayAddress;
            commandToBeSent = command;
            binaryTransfer = command.EndsWith(" BINARY", StringComparison.Ordinal);
            truncateBlankTail = truncate;
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
            if (_cartridgeBuffer == null)
            {
                EnableAllButtons();
                throw new InvalidOperationException(Properties.Resources.NoCartridgeFileLoaded);
            }

            var result = new StringBuilder(((_cartridgeSize + 15) / 16) * 80);
            for (int index = 0; index < _cartridgeSize; index += 16)
            {
                result.Append(BuildLine(cartridgeAddressStart + index, _cartridgeBuffer,
                    index, Math.Min(16, _cartridgeSize - index)));
            }

            return result.ToString();
        }

        private static string BuildLine(int address, byte[] data, int offset, int count)
        {
            var result = new StringBuilder(80);
            var asciiVersion = new StringBuilder(16);

            result.AppendFormat("${0:X4} : ", address);

            for (int index = 0; index < count; index++)
            {
                byte t = data[offset + index];
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

            if (count < 16)
            {
                for (int missingByte = 0; missingByte < (16 - count); missingByte++)
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

            UserSettings.Default.TempFile = filePath;
                       
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
            string activePort = UserSettings.Default.COMPort;
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
            _mainView.SetProgressMessage("");

            _mainView.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            cartridgeReaderBackground.RunWorkerAsync(arduinoSettings);

        }

        private void CreateEmptyCartridge(int cartridgeClearSize)
        {
            _cartridgeSize = cartridgeClearSize;
            _cartridgeBuffer = new byte[cartridgeClearSize];
        }

        // Standard CRC-32 (polynomial 0xEDB88320) over the dumped cartridge bytes.
        // .NET Framework 4.5 has no built-in CRC-32, so compute it directly. Run
        // after any tail-truncation so the value matches the saved .rom file.
        private static uint ComputeCrc32(byte[] data, int length)
        {
            uint crc = 0xFFFFFFFF;
            for (int i = 0; i < length; i++)
            {
                crc ^= data[i];
                for (int bit = 0; bit < 8; bit++)
                {
                    crc = (crc >> 1) ^ (0xEDB88320 & (uint)(-(int)(crc & 1)));
                }
            }
            return crc ^ 0xFFFFFFFF;
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
            const int maximumSegments = 4;
            for (int segment = 0; segment < maximumSegments && _cartridgeSize >= chipSize; segment++)
            {
                int segmentStart = _cartridgeSize - chipSize;
                if (IsRangeEmpty(segmentStart, chipSize))
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
                throw new InvalidOperationException(Properties.Resources.BlankCartridge);
            }
        }

        private bool IsRangeEmpty(int start, int length)
        {
            for (int currentByte = start; currentByte < start + length; currentByte++)
            {
                if (_cartridgeBuffer[currentByte] != 0xFF)
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
                throw new InvalidOperationException("cartScanner settings were not specified.");
            }

            const int cUpdateProgressEvery = 0x0250;
            using (var serialPort = new SerialPort(arduinoSettings.SerialPort, arduinoSettings.BaudRate))
            {
                serialPort.ReadTimeout = 2000;
                serialPort.WriteTimeout = 2000;

                serialPort.Open();
                serialPort.DiscardInBuffer();
                serialPort.WriteLine(commandToBeSent);

                string readLine = ReadProtocolLine(serialPort);
                if (!Properties.Resources.cStartMessage.Equals(readLine, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new InvalidOperationException(
                        string.Format(Properties.Resources.ArduinoUnexpectedValueMessage,
                        Properties.Resources.cStartMessage, readLine));
                }

                CreateEmptyCartridge(cartridgeSize);

                int currentAddress = 0;
                int nextProgressUpdate = cUpdateProgressEvery;
                string endMessage;

                if (binaryTransfer)
                {
                    while (currentAddress < cartridgeSize)
                    {
                        if (worker.CancellationPending)
                        {
                            e.Cancel = true;
                            e.Result = false;
                            return;
                        }

                        int remaining = cartridgeSize - currentAddress;
                        currentAddress += ReadBinaryBlock(serialPort, _cartridgeBuffer, currentAddress, remaining);
                        if (currentAddress >= nextProgressUpdate)
                        {
                            ReportReadProgress(worker, currentAddress, cartridgeSize);
                            nextProgressUpdate = currentAddress + cUpdateProgressEvery;
                        }
                    }

                    endMessage = ReadProtocolLine(serialPort);
                }
                else
                {
                    string currentBlock = ReadProtocolLine(serialPort);
                    while (!Properties.Resources.cEndMessage.Equals(currentBlock, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (worker.CancellationPending)
                        {
                            e.Cancel = true;
                            e.Result = false;
                            return;
                        }

                        if (currentBlock.Length == 0 || (currentBlock.Length & 1) != 0)
                        {
                            throw new InvalidOperationException(
                                string.Format(Properties.Resources.ArduinoUnexpectedValueMessage,
                                "an even-length hexadecimal data line", currentBlock));
                        }

                        int blockByteCount = currentBlock.Length / 2;
                        if (blockByteCount > cartridgeSize - currentAddress)
                        {
                            throw new InvalidOperationException(
                                string.Format(Properties.Resources.UnexpectedCartridgeSize,
                                currentAddress + blockByteCount, cartridgeSize));
                        }

                        for (int i = 0; i < currentBlock.Length; i += 2)
                        {
                            _cartridgeBuffer[currentAddress++] = ParseByte(currentBlock.Substring(i, 2));
                        }

                        if (currentAddress >= nextProgressUpdate)
                        {
                            ReportReadProgress(worker, currentAddress, cartridgeSize);
                            nextProgressUpdate = currentAddress + cUpdateProgressEvery;
                        }

                        currentBlock = ReadProtocolLine(serialPort);
                    }

                    endMessage = currentBlock;
                }

                if (!Properties.Resources.cEndMessage.Equals(endMessage, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new InvalidOperationException(
                        string.Format(Properties.Resources.ArduinoUnexpectedValueMessage,
                        Properties.Resources.cEndMessage, endMessage));
                }

                if (currentAddress != cartridgeSize)
                {
                    throw new InvalidOperationException(
                        string.Format(Properties.Resources.UnexpectedCartridgeSize,
                        currentAddress, cartridgeSize));
                }

                if (truncateBlankTail)
                {
                    TruncateCartridge();
                }

                worker.ReportProgress(100);
            }

            e.Result = true;
        }

        private static int ReadBinaryBlock(SerialPort serialPort, byte[] buffer, int offset, int remaining)
        {
            try
            {
                int bytesRead = serialPort.Read(buffer, offset, Math.Min(4096, remaining));
                if (bytesRead <= 0)
                {
                    throw new IOException("No cartridge bytes were received.");
                }
                return bytesRead;
            }
            catch (Exception exception)
            {
                throw new InvalidProgramException("Cartscanner disconnected during cartridge transfer.", exception);
            }
        }

        private static string ReadProtocolLine(SerialPort serialPort)
        {
            try
            {
                return serialPort.ReadLine().Trim();
            }
            catch (Exception exception)
            {
                throw new InvalidProgramException("Cartscanner disconnected during cartridge transfer.", exception);
            }
        }

        private static void ReportReadProgress(BackgroundWorker worker, int currentAddress, int expectedSize)
        {
            worker.ReportProgress((int)((currentAddress / (float)expectedSize) * 90));
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
            bool readSucceeded = e.Error == null && !e.Cancelled && e.Result is bool result && result;

            if (e.Error != null)
            {
                ClearCartridgeData();
                _mainView.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
                MessageBox.Show(_mainView, e.Error.Message, Properties.Resources.CartridgeReadErrorTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (readSucceeded)
            {
                ShowCartridgeData();
                uint crc = ComputeCrc32(_cartridgeBuffer, _cartridgeSize);
                _mainView.SetProgressMessage(string.Format("Finished CRC: {0:X8}", crc));
            }
            else
            {
                ClearCartridgeData();
            }

            // If checkbox isChecked=True then save cartridge data to temp folder then launch using supplied parameters.
            if (readSucceeded && UserSettings.Default.SaveRunState)
            {
                SaveFile("", true);
                try
                {
                    String emulatorLocation = UserSettings.Default.EmulatorLocation + " ";

                    String commandLineOptions = UserSettings.Default.CMDOptions; 
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
                    MessageBox.Show($"Something went wrong starting the emulator.\nEmulator: {UserSettings.Default.EmulatorLocation}\n" 
                        + $"Command Line: {UserSettings.Default.CMDOptions}\n"+ exception.Message, "Error Starting Emulator", 
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
            bool cartridgeAvailable = _cartridgeBuffer != null && _cartridgeSize > 0;
            _mainView._SaveResults.IsEnabled = cartridgeAvailable;
            _mainView._DisplayOutput.IsEnabled = cartridgeAvailable;
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
            _mainView._Sgc128Button.IsEnabled = true;
            _mainView._Sgc128Button.Background = Brushes.LightGray;
            _mainView._Sgc128Button.IsHitTestVisible = true;
            _mainView._Sgc128Button.Opacity = 1;
            _mainView._Sgc256Button.IsEnabled = true;
            _mainView._Sgc256Button.Background = Brushes.LightGray;
            _mainView._Sgc256Button.IsHitTestVisible = true;
            _mainView._Sgc256Button.Opacity = 1;
            _mainView._Sgc512Button.IsEnabled = true;
            _mainView._Sgc512Button.Background = Brushes.LightGray;
            _mainView._Sgc512Button.IsHitTestVisible = true;
            _mainView._Sgc512Button.Opacity = 1;
        }
        #endregion
    }
}
