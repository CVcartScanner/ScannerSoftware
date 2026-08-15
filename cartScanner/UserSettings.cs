using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CVcartScanner
{
    internal sealed class UserSettings
    {
        private const string LegacyPublisherDirectory = "Matthew_Heironimus_Origin";
        private static readonly UserSettings Instance = new UserSettings();
        private readonly string settingsPath;

        internal static UserSettings Default
        {
            get { return Instance; }
        }

        internal bool SaveRunState { get; set; }
        internal string EmulatorLocation { get; set; }
        internal string CMDOptions { get; set; }
        internal string COMPort { get; set; }
        internal string TempFile { get; set; }

        private UserSettings()
        {
            string localApplicationData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            settingsPath = Path.Combine(localApplicationData, "CVcartScanner", "settings.xml");

            EmulatorLocation = string.Empty;
            CMDOptions = string.Empty;
            COMPort = string.Empty;
            TempFile = string.Empty;

            if (File.Exists(settingsPath))
            {
                LoadDocument(XDocument.Load(settingsPath));
            }
            else if (ImportLegacySettings(localApplicationData))
            {
                Save();
            }
        }

        internal void Save()
        {
            string directory = Path.GetDirectoryName(settingsPath);
            Directory.CreateDirectory(directory);

            var document = new XDocument(
                new XElement("CVcartScannerSettings",
                    new XElement("SaveRunState", SaveRunState),
                    new XElement("EmulatorLocation", EmulatorLocation ?? string.Empty),
                    new XElement("CMDOptions", CMDOptions ?? string.Empty),
                    new XElement("COMPort", COMPort ?? string.Empty),
                    new XElement("TempFile", TempFile ?? string.Empty)));

            string temporaryPath = settingsPath + ".tmp";
            document.Save(temporaryPath);
            if (File.Exists(settingsPath))
            {
                File.Replace(temporaryPath, settingsPath, null);
            }
            else
            {
                File.Move(temporaryPath, settingsPath);
            }
        }

        private void LoadDocument(XDocument document)
        {
            XElement root = document.Root;
            if (root == null)
            {
                return;
            }

            bool parsedBoolean;
            XElement saveRunState = root.Element("SaveRunState");
            if (saveRunState != null && bool.TryParse(saveRunState.Value, out parsedBoolean))
            {
                SaveRunState = parsedBoolean;
            }

            EmulatorLocation = ReadValue(root, "EmulatorLocation", EmulatorLocation);
            CMDOptions = ReadValue(root, "CMDOptions", CMDOptions);
            COMPort = ReadValue(root, "COMPort", COMPort);
            TempFile = ReadValue(root, "TempFile", TempFile);
        }

        private bool ImportLegacySettings(string localApplicationData)
        {
            string legacyRoot = Path.Combine(localApplicationData, LegacyPublisherDirectory);
            if (!Directory.Exists(legacyRoot))
            {
                return false;
            }

            IEnumerable<string> legacyFiles;
            try
            {
                legacyFiles = Directory.GetFiles(legacyRoot, "user.config", SearchOption.AllDirectories)
                    .OrderByDescending(File.GetLastWriteTimeUtc);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            foreach (string legacyFile in legacyFiles)
            {
                try
                {
                    XDocument document = XDocument.Load(legacyFile);
                    if (LoadLegacyDocument(document))
                    {
                        return true;
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (System.Xml.XmlException)
                {
                }
            }

            return false;
        }

        private bool LoadLegacyDocument(XDocument document)
        {
            bool foundSetting = false;
            foreach (XElement setting in document.Descendants("setting"))
            {
                XAttribute name = setting.Attribute("name");
                XElement value = setting.Element("value");
                if (name == null || value == null)
                {
                    continue;
                }

                switch (name.Value)
                {
                    case "SaveRunState":
                        bool parsedBoolean;
                        if (bool.TryParse(value.Value, out parsedBoolean))
                        {
                            SaveRunState = parsedBoolean;
                            foundSetting = true;
                        }
                        break;
                    case "EmulatorLocation":
                        EmulatorLocation = value.Value;
                        foundSetting = true;
                        break;
                    case "CMDOptions":
                        CMDOptions = value.Value;
                        foundSetting = true;
                        break;
                    case "COMPort":
                        COMPort = value.Value;
                        foundSetting = true;
                        break;
                    case "TempFile":
                        TempFile = value.Value;
                        foundSetting = true;
                        break;
                }
            }

            return foundSetting;
        }

        private static string ReadValue(XElement root, string name, string defaultValue)
        {
            XElement element = root.Element(name);
            return element == null ? defaultValue : element.Value;
        }
    }
}
