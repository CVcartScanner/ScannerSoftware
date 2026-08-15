using System;
using System.Globalization;

namespace CVcartScanner
{
    internal static class ComPortName
    {
        private const String Prefix = "COM";

        public static bool TryCreate(String portNumberText, out String portName)
        {
            portName = null;
            if (String.IsNullOrWhiteSpace(portNumberText))
            {
                return false;
            }

            Int32 portNumber;
            if (!Int32.TryParse(portNumberText.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out portNumber) || portNumber < 1)
            {
                return false;
            }

            portName = Prefix + portNumber.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        public static bool TryGetNumber(String portName, out String portNumberText)
        {
            portNumberText = null;
            if (String.IsNullOrWhiteSpace(portName) || !portName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            String normalizedPortName;
            if (!TryCreate(portName.Substring(Prefix.Length), out normalizedPortName))
            {
                return false;
            }

            portNumberText = normalizedPortName.Substring(Prefix.Length);
            return true;
        }

        public static bool IsValid(String portName)
        {
            String portNumberText;
            return TryGetNumber(portName, out portNumberText);
        }
    }
}
