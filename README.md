# CV cartScanner 2.0

CV cartScanner reads ColecoVision and ADAM cartridges through an Arduino UNO-based cartridge-reader interface. Version 2.0 adds optimized binary transfers and SGC cartridge dumping at 128 KB, 256 KB, and 512 KB.

This project is based on the original hardware, Arduino firmware, and Windows software by Matthew Heironimus: https://github.com/MHeironimus/ColecoVisionCartridgeReader

## Repository contents

- `Release/CVcartScanner.exe` — compiled Windows interface, version 2.0.
- `cartScanner/` — complete C# and WPF source required to build the Windows interface.
- `ArduinoSketch/cartScanner/cartScanner.ino` — current Arduino UNO firmware.
- `ArduinoSketch/cartScanner/FastShiftOut.*` — bundled address-shift dependency used by the firmware.

Generated `bin` and `obj` directories are intentionally excluded from the repository.

## Firmware

To update firmware, first connect your cartScanner and review Settings.  If you do not have a com port showing, click the Detect cartScanner button.  A com port should show up.  
Open `ArduinoSketch/cartScanner/cartScanner.ino` in the Arduino IDE and select **Arduino Uno** as the target board. The required FastShiftOut source is included in the sketch directory, so no separate library installation is required.

The Windows application and Arduino firmware must be updated together because version 2.0 uses binary serial commands for cartridge transfers.

## Windows application

Open `cartScanner/cartScanner.csproj` with Visual Studio and build the **Release** configuration. The project targets .NET Framework 4.5 and uses only framework references to ensure compatibility on older Windows systems.

## Licensing

Except for the bundled FastShiftOut files, CV cartScanner is licensed under the **GNU Lesser General Public License version 3 or later**. See `LICENSE`.

FastShiftOut 0.4.3 is copyright Rob Tillaart and is distributed under the MIT License. See `ArduinoSketch/cartScanner/FastShiftOut-LICENSE.txt`.

Support: cartScanner@yahoo.com
