using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColecoVisionCartridgeReader
{
    class Presenter
    {
        #region Private Fields

        private readonly MainWindow _view;

        private byte[] _cartridgeBuffer;
        private long _cartridgeSize;

        #endregion

        #region Constructor

        public Presenter(MainWindow view)
        {
            _view = view;
        }

        #endregion

        #region Public Methods

        public bool OpenFile()
        {
            // Configure open file dialog box
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".rom",
                Filter = "ColecoVision Cartridge (*.rom)|*.rom|All Files (*.*)|*.*"
            };

            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                ClearCartridgeData();

                // Open document 
                LoadFile(dlg.FileName);
            }
        }

        #endregion

        #region Private Methods

        private void ClearCartridgeData()
        {
            // Clear Previous Cartridge Data
            _cartridgeBuffer = null;
            _cartridgeSize = 0;
            CartridgeData.Text = string.Empty;
            FileSaveAs.IsEnabled = false;
        }

        private void LoadFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            Title = ApplicationTitle + " [" + fileName + "]";

            _cartridgeSize = new FileInfo(filePath).Length;

            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using (var reader = new BinaryReader(fileStream))
                {
                    fileStream = null;
                    _cartridgeBuffer = reader.ReadBytes((int)_cartridgeSize);
                } // reader
            }
            catch
            {
                if (fileStream != null)
                {
                    fileStream.Dispose();
                }
                throw;
            }

            FileSaveAs.IsEnabled = true;

            DisplayFile();
            CartridgeData.Select(0, 0);
        }

        #endregion


    }
}
