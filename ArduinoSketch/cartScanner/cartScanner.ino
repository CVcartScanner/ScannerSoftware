// ColecoVision / ADAM Cartridge Reader
// for the Arduino UNO
// 2014-11-25 - Initial Version - Matthew Heironimus/MHeironimus
// 2022-02-11 - Updated code - OriginalJohn
//              Tested -  32k, 128k, 256k cartridges, SGM, ORIG, HB
//              Big Thanks to Tursi, ChildOfCV, youki, NIAD and chart45. 
//----------------------------------------------------------------------------------

// Arduino Pins
const int gcChipSelectLine[4] = { A0, A1, A2, A3 };
const int gcShiftRegisterClock = 11;
const int gcStorageRegisterClock = 12;
const int gcSerialAddress = 10;
const int gcDataBit[8] = { 2, 3, 4, 5, 6, 7, 8, 9 };
const unsigned int baseAddress = 0x8000;
const unsigned int bankStart = 0xc000;
const unsigned int bankSize = 0x4000;

// Shifts a 16-bit value out to a shift register.
// Parameters:
//   dataPin - Arduino Pin connected to the data pin of the shift register.
//   clockPin - Arduino Pin connected to the data clock pin of the shift register.
//----------------------------------------------------------------------------------
void shiftOut16(int dataPin, int clockPin, int bitOrder, int value)
{
  // Shift out highbyte for MSBFIRST
  shiftOut(dataPin, clockPin, bitOrder, (bitOrder == MSBFIRST ? (value >> 8) : value));
  // shift out lowbyte for MSBFIRST
  shiftOut(dataPin, clockPin, bitOrder, (bitOrder == MSBFIRST ? value : (value >> 8)));
}

// Select which chip on the cartridge to read (LOW = Active).
// Use -1 to set all chip select lines HIGH.
//----------------------------------------------------------------------------------
void SelectChip(byte chipToSelect)
{
  for (int currentChipLine = 0; currentChipLine < 4; currentChipLine++)
  {
    digitalWrite(gcChipSelectLine[currentChipLine], (chipToSelect != currentChipLine));
  }
}

// Set Address Lines
//----------------------------------------------------------------------------------
void SetAddress(unsigned int address)
{
  SelectChip(-1);

  // Disable shift register output while loading address
  digitalWrite(gcStorageRegisterClock, LOW);

  // Write Out Address
  shiftOut16(gcSerialAddress, gcShiftRegisterClock, MSBFIRST, address);

  // Enable shift register output
  digitalWrite(gcStorageRegisterClock, HIGH);

  int chipToSelect;

  if (address < 0xA000) {
    chipToSelect = 0;
  } else if (address < 0xC000) {
    chipToSelect = 1;
  } else if (address < 0xE000) {
    chipToSelect = 2;
  } else {
    chipToSelect = 3;
  }

  SelectChip(chipToSelect);
}

// Read data lines
//----------------------------------------------------------------------------------
void ReadDataLines(boolean writeToConsole)
{
  const char cHexLookup[16] = {
    '0', '1', '2', '3', '4', '5', '6', '7',
    '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
  };

  int highNibble = 0;
  int lowNibble = 0;
  boolean dataBits[8];
  char byteReadHex[4];

  for (int currentBit = 0; currentBit < 8; currentBit++)
  {
    dataBits[currentBit] = digitalRead(gcDataBit[currentBit]);
  }

  highNibble = (dataBits[7] << 3) + (dataBits[6] << 2) + (dataBits[5] << 1) + dataBits[4];
  lowNibble = (dataBits[3] << 3) + (dataBits[2] << 2) + (dataBits[1] << 1) + dataBits[0];

  if (writeToConsole) {
    Serial.write(cHexLookup[highNibble]);
    Serial.write(cHexLookup[lowNibble]);
    Serial.println();
  }
}

// Read all of the data from a 32k cartridge.
//----------------------------------------------------------------------------------
void ReadCartridge()
{
  unsigned int baseAddress = 0x8000;

  Serial.println("BEGIN:");
  // 128k in hex 0x20000, 256k 0x40000, 512k 0x80000
  // Read Current Chip (cartridge is 32K, each chip is 8k)

  for (unsigned int currentAddress = 0; currentAddress < 0x8000; currentAddress++)
  {
    SetAddress(baseAddress + currentAddress);
    ReadDataLines(true);
  }

  Serial.println(":FINISHED");
}

// Read all of the data from a 64k cartridge.
//----------------------------------------------------------------------------------
void Read64kCartridge()
{
  const int numBanks64k = (65536 - 0x4000l + bankSize - 1) / bankSize;
  ReadMegaCart(numBanks64k);  
}

// Read all of the data from a 128k cartridge.
//----------------------------------------------------------------------------------
void Read128kCartridge()
{
  const int numBanks128k = (131072 - 0x4000l + bankSize - 1) / bankSize;
  ReadMegaCart(numBanks128k);
}

// Read all of the data from a 256k cartridge.
//----------------------------------------------------------------------------------
void Read256kCartridge()
{
  const int numBanks256 = (262144 - 0x4000l + bankSize - 1) / bankSize;
  ReadMegaCart(numBanks256);
}

// Read all of the data from a 512k cartridge.
//----------------------------------------------------------------------------------
void Read512kCartridge()
{
  const int numBanks256 = (524288 - 0x4000l + bankSize - 1) / bankSize;
  ReadMegaCart(numBanks256);
}


void ReadMegaCart(int numBanks) {
    unsigned int bankAddress = 0xffc0;
  Serial.println("BEGIN:");

   // Read additional banks
  for (int bank = 0; bank < numBanks; bank++)
  {
    SetAddress(bankAddress++);
    for (unsigned int currentAddress = 0; currentAddress < bankSize-0x40; currentAddress++) 
    {
      SetAddress(bankStart + currentAddress);
      ReadDataLines(true);  
    }
    // Simulate reading the last 0x40
    for(unsigned int c = 0; c < 0x40; c++)
    {
      Serial.println("00");
    }
  }

  // Read constant bank
  for (unsigned int currentAddress = 0; currentAddress < 0x4000; currentAddress++) 
  {
    SetAddress(baseAddress + currentAddress);
    ReadDataLines(true);  
  }

    Serial.println(":FINISHED");
}

// Returns the next line from the serial port as a String.
//----------------------------------------------------------------------------------
String SerialReadLine()
{
  const int BUFFER_SIZE = 81;
  char lineBuffer[BUFFER_SIZE];
  int currentPosition = 0;
  int currentValue;

  do
  {
    // Read until we get the next character
    do
    {
      currentValue = Serial.read();
    } while (currentValue == -1);

    // ignore '\r' characters
    if (currentValue != '\r')
    {
      lineBuffer[currentPosition] = currentValue;
      currentPosition++;
    }

  } while ((currentValue != '\n') && (currentPosition < BUFFER_SIZE));
  lineBuffer[currentPosition - 1] = 0;

  return String(lineBuffer);
}

void setup()
{
  // Setup Serial Monitor
  Serial.begin(57600);

  // Setup Chip Select Pins
  for (int chipLine = 0; chipLine < 4; chipLine++)
  {
    pinMode(gcChipSelectLine[chipLine], OUTPUT);
  }

  // Setup Serial Address Pins
  pinMode(gcShiftRegisterClock, OUTPUT);
  pinMode(gcStorageRegisterClock, OUTPUT);
  pinMode(gcSerialAddress, OUTPUT);

  // Setup Data Pins
  for (int currentBit = 0; currentBit < 8; currentBit++)
  {
    pinMode(gcDataBit[currentBit], INPUT_PULLUP);
  }

  while (!Serial) {
    ; // wait for serial port to connect. Needed for Leonardo only.
  }

  SetAddress(0);
}

void loop()
{
  if (Serial.available() > 0)
  {
    String lineRead = SerialReadLine();
    lineRead.toUpperCase();

    if (lineRead == "READ CARTRIDGE")
    {
      ReadCartridge();
    } // lineRead = "Read All"

    if (lineRead == "READ 64K CARTRIDGE")
    {
      Read64kCartridge();
    } // Receive 64k cartridge request and read all.
    
    if (lineRead == "READ 128K CARTRIDGE")
    {
      Read128kCartridge();
    } // Receive 128k cartridge request and read all.

    if (lineRead == "READ 256K CARTRIDGE")
    {
      Read256kCartridge();
    } // Receive 256k cartridge request and read all.

    if (lineRead == "READ 512K CARTRIDGE")
    {
      Read512kCartridge();
    } // Receive 512k cartridge request and read all.

    if (lineRead == "STATUS")
    {
      Serial.println("CVCARTSCANNER");
    } // if status request is received, respond to identify this is the cartscanner.
  }
}
