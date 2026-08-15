/*
 * CV cartScanner firmware for the Arduino UNO.
 * Copyright (C) 2014 Matthew Heironimus
 * Copyright (C) 2022-2026 OriginalJohn and CartScanner contributors
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 *
 * SPDX-License-Identifier: LGPL-3.0-or-later
 */

#include "FastShiftOut.h"

// ColecoVision / ADAM Cartridge Reader for the Arduino UNO
//
// 2014-11-25 - Initial Version - Matthew Heironimus/MHeironimus
// 2022-02-11 - Updated code - OriginalJohn
// 2022-06-01 - Speed optimizations
// 2026-08-12 - Added 128k, 256k, and 512k SGC cartridge dumping, info provided by Pitou!
// 2026-08-12 - Fixed bank handling and added assorted optimizations
//
// Tested 32k, 64k, 64k Pixelboy, 128k, 256k, 512k, 128k and 256k SGM, 
// including original and homebrew cartridges.
//
//----------------------------------------------------------------------------------

// Arduino pins. The optimized port code below is specific to the Arduino UNO:
// D2-D7 = PD2-PD7, D8-D12 = PB0-PB4, A0-A3 = PC0-PC3.
const uint8_t gcShiftRegisterClock = 11;
const uint8_t gcStorageRegisterClock = 12;
const uint8_t gcSerialAddress = 10;
FastShiftOut FSO(gcSerialAddress, gcShiftRegisterClock, MSBFIRST);

const uint16_t bankSize16k = 0x4000;
const uint16_t bankSize32k = 0x8000;
const uint16_t bankStart = 0xc000;
const uint16_t megaCartReadableBankSize = bankSize16k - 0x40;
const uint16_t sgcWindowSize = 0x2000;
const uint8_t sgcPageCount128k = 16;
const uint8_t sgcPageCount256k = 32;
const uint8_t sgcPageCount512k = 64;
const uint8_t asciiBlockSize = 32;
const uint8_t binaryBlockSize = 64;

bool binaryOutput = false;
uint8_t outputByteCount = 0;
uint8_t binaryBlock[binaryBlockSize];
char asciiBlock[asciiBlockSize * 2];

// Set all four active-low cartridge chip selects HIGH.
//----------------------------------------------------------------------------------
inline void DisableChips()
{
  PORTC |= 0x0f;
}

// Select one of A0-A3 while preserving A4 and A5.
//----------------------------------------------------------------------------------
inline void SelectChip(uint8_t chipToSelect)
{
  uint8_t value = PORTC | 0x0f;
  if (chipToSelect < 4)
  {
    value &= (uint8_t)~(1 << chipToSelect);
  }
  PORTC = value;
}

// Shift and latch a 16-bit address without changing the chip-select state.
//----------------------------------------------------------------------------------
inline void ShiftAddress(uint16_t address)
{
  PORTB &= (uint8_t)~_BV(PB4);
  FSO.write16(address);
  PORTB |= _BV(PB4);
}

// Put an address on the bus, then select its 8k cartridge region.
//----------------------------------------------------------------------------------
inline void SetAddress(uint16_t address)
{
  DisableChips();
  ShiftAddress(address);

  if (address < 0xa000)
  {
    SelectChip(0);
  }
  else if (address < 0xc000)
  {
    SelectChip(1);
  }
  else if (address < 0xe000)
  {
    SelectChip(2);
  }
  else
  {
    SelectChip(3);
  }
}

// Read D0-D7 from the two UNO input ports in one operation per port.
//----------------------------------------------------------------------------------
inline uint8_t ReadDataByte()
{
  return (uint8_t)(((PIND >> 2) & 0x3f) | ((PINB & 0x03) << 6));
}

// Switch the cartridge data bus between reads and SGC register writes.
//----------------------------------------------------------------------------------
inline void SetDataLinesInput()
{
  DDRD &= (uint8_t)~0xfc;
  DDRB &= (uint8_t)~0x03;
  PORTD |= 0xfc;
  PORTB |= 0x03;
}

inline void SetDataLinesOutput()
{
  // The SGC reference begins its register sequence with zero on the data bus.
  // Clear the output latches before enabling the drivers to avoid a HIGH pulse
  // left over from the normal-read pull-ups.
  PORTD &= (uint8_t)~0xfc;
  PORTB &= (uint8_t)~0x03;
  DDRD |= 0xfc;
  DDRB |= 0x03;
}

inline void WriteDataByte(uint8_t value)
{
  PORTD = (uint8_t)((PORTD & 0x03) | ((value & 0x3f) << 2));
  PORTB = (uint8_t)((PORTB & 0xfc) | (value >> 6));
  delayMicroseconds(1);
}

// Buffered serial output. Binary is used by the current Windows application;
// ASCII remains available for older applications and serial-terminal testing.
//----------------------------------------------------------------------------------
void FlushOutput()
{
  if (outputByteCount == 0)
  {
    return;
  }

  if (binaryOutput)
  {
    Serial.write(binaryBlock, outputByteCount);
  }
  else
  {
    Serial.write((const uint8_t *)asciiBlock, outputByteCount * 2);
    Serial.println();
  }

  outputByteCount = 0;
}

void BeginDump(bool useBinaryOutput)
{
  binaryOutput = useBinaryOutput;
  outputByteCount = 0;
  Serial.println("BEGIN:");
}

inline void OutputByte(uint8_t value)
{
  if (binaryOutput)
  {
    binaryBlock[outputByteCount++] = value;
    if (outputByteCount == binaryBlockSize)
    {
      FlushOutput();
    }
  }
  else
  {
    uint8_t highNibble = value >> 4;
    uint8_t lowNibble = value & 0x0f;
    asciiBlock[outputByteCount * 2] = highNibble < 10 ? '0' + highNibble : 'A' + highNibble - 10;
    asciiBlock[outputByteCount * 2 + 1] = lowNibble < 10 ? '0' + lowNibble : 'A' + lowNibble - 10;
    outputByteCount++;
    if (outputByteCount == asciiBlockSize)
    {
      FlushOutput();
    }
  }
}

void EndDump()
{
  FlushOutput();
  Serial.println(":FINISHED");
}

// Special thanks to Pitou! for sharing their original source and research,
// which made this implementation possible.  
//----------------------------------------------------------------------------------
void SelectSgcPage(uint8_t page)
{
  DisableChips();
  SelectChip(3);

  ShiftAddress(0xfffc);
  SetDataLinesOutput();
  WriteDataByte(0x00);
  ShiftAddress(0xfffd);
  WriteDataByte(page);
  ShiftAddress(0xfffe);
  WriteDataByte(0x00);
  ShiftAddress(0xffff);
  WriteDataByte(0x00);

  DisableChips();
  SetDataLinesInput();
  delay(1);
}

void ReadSgcCartridge(uint8_t pageCount, bool useBinaryOutput)
{
  BeginDump(useBinaryOutput);

  for (uint8_t page = 0; page < pageCount; page++)
  {
    SelectSgcPage(page);
    for (uint16_t currentAddress = 0; currentAddress < sgcWindowSize; currentAddress++)
    {
      SetAddress(bankStart + currentAddress);
      OutputByte(ReadDataByte());
    }
  }

  EndDump();
}

void NormalRead(uint16_t readSize)
{
  for (uint16_t currentAddress = 0; currentAddress < readSize; currentAddress++)
  {
    SetAddress(bankSize32k + currentAddress);
    OutputByte(ReadDataByte());
  }
}

void ReadCartridge(bool useBinaryOutput)
{
  BeginDump(useBinaryOutput);
  NormalRead(bankSize32k);
  EndDump();
}

void Read64kBank(uint16_t shiftAddress)
{
  SetAddress(shiftAddress);

  for (uint16_t currentAddress = 0; currentAddress < bankSize16k; currentAddress++)
  {
    uint16_t absoluteAddress = bankStart + currentAddress;
    if (absoluteAddress > 0xff90)
    {
      SetAddress(shiftAddress);
    }
    SetAddress(absoluteAddress);
    OutputByte(ReadDataByte());
  }
}

void Read64kCartridgeAlternate(bool useBinaryOutput)
{
  BeginDump(useBinaryOutput);
  NormalRead(bankSize16k);
  Read64kBank(0xff90);
  Read64kBank(0xffa0);
  Read64kBank(0xffb0);
  EndDump();
}

// Read MegaCart banks without touching the final 64-byte bank-select window.
// The skipped selector bytes retain the established zero-padding behavior.
// numAdditionalBanks excludes the fixed $8000-$BFFF bank.
//----------------------------------------------------------------------------------
void ReadMegaCart(uint8_t numAdditionalBanks, bool useBinaryOutput)
{
  uint16_t bankAddress = (uint16_t)(0xffff - numAdditionalBanks);
  BeginDump(useBinaryOutput);

  for (uint8_t bank = 0; bank < numAdditionalBanks; bank++)
  {
    SetAddress(bankAddress++);

    for (uint16_t currentAddress = 0; currentAddress < megaCartReadableBankSize; currentAddress++)
    {
      SetAddress(bankStart + currentAddress);
      OutputByte(ReadDataByte());
    }

    for (uint8_t padding = 0; padding < 0x40; padding++)
    {
      OutputByte(0x00);
    }
  }

  for (uint16_t currentAddress = 0; currentAddress < bankSize16k; currentAddress++)
  {
    SetAddress(bankSize32k + currentAddress);
    OutputByte(ReadDataByte());
  }

  EndDump();
}

void Read64kCartridge(bool useBinaryOutput)
{
  ReadMegaCart(3, useBinaryOutput);
}

void Read128kCartridge(bool useBinaryOutput)
{
  ReadMegaCart(7, useBinaryOutput);
}

void Read256kCartridge(bool useBinaryOutput)
{
  ReadMegaCart(15, useBinaryOutput);
}

void Read512kCartridge(bool useBinaryOutput)
{
  ReadMegaCart(31, useBinaryOutput);
}

void Test()
{
  BeginDump(false);
  NormalRead(bankSize32k);
  EndDump();
}

// Return the next serial line. Commands are at most 80 characters.
//----------------------------------------------------------------------------------
String SerialReadLine()
{
  const uint8_t bufferSize = 81;
  char lineBuffer[bufferSize];
  uint8_t currentPosition = 0;
  int currentValue;

  do
  {
    do
    {
      currentValue = Serial.read();
    } while (currentValue == -1);

    if (currentValue != '\r' && currentPosition < bufferSize - 1)
    {
      lineBuffer[currentPosition++] = (char)currentValue;
    }
  } while (currentValue != '\n');

  if (currentPosition > 0 && lineBuffer[currentPosition - 1] == '\n')
  {
    currentPosition--;
  }
  lineBuffer[currentPosition] = 0;
  return String(lineBuffer);
}

void setup()
{
  Serial.begin(57600);

  // A0-A3 chip selects, D10-D12 shift-register controls.
  DDRC |= 0x0f;
  DDRB |= _BV(PB2) | _BV(PB3) | _BV(PB4);
  DisableChips();
  PORTB &= (uint8_t)~_BV(PB3);
  PORTB |= _BV(PB4);
  SetDataLinesInput();

  while (!Serial)
  {
    ;
  }

  SetAddress(0);
}

void loop()
{
  if (Serial.available() == 0)
  {
    return;
  }

  String lineRead = SerialReadLine();
  lineRead.toUpperCase();

  if (lineRead == "READ CARTRIDGE BINARY")
  {
    ReadCartridge(true);
  }
  else if (lineRead == "READ 64K CARTRIDGE BINARY")
  {
    Read64kCartridge(true);
  }
  else if (lineRead == "READ 64K ALT BINARY")
  {
    Read64kCartridgeAlternate(true);
  }
  else if (lineRead == "READ 128K CARTRIDGE BINARY")
  {
    Read128kCartridge(true);
  }
  else if (lineRead == "READ 256K CARTRIDGE BINARY")
  {
    Read256kCartridge(true);
  }
  else if (lineRead == "READ 512K CARTRIDGE BINARY")
  {
    Read512kCartridge(true);
  }
  else if (lineRead == "READ 128K SGC CARTRIDGE BINARY")
  {
    ReadSgcCartridge(sgcPageCount128k, true);
  }
  else if (lineRead == "READ 256K SGC CARTRIDGE BINARY")
  {
    ReadSgcCartridge(sgcPageCount256k, true);
  }
  else if (lineRead == "READ 512K SGC CARTRIDGE BINARY")
  {
    ReadSgcCartridge(sgcPageCount512k, true);
  }
  else if (lineRead == "READ CARTRIDGE")
  {
    ReadCartridge(false);
  }
  else if (lineRead == "READ 64K CARTRIDGE")
  {
    Read64kCartridge(false);
  }
  else if (lineRead == "READ 64K ALT")
  {
    Read64kCartridgeAlternate(false);
  }
  else if (lineRead == "READ 128K CARTRIDGE")
  {
    Read128kCartridge(false);
  }
  else if (lineRead == "READ 256K CARTRIDGE")
  {
    Read256kCartridge(false);
  }
  else if (lineRead == "READ 512K CARTRIDGE")
  {
    Read512kCartridge(false);
  }
  else if (lineRead == "READ 128K SGC CARTRIDGE")
  {
    ReadSgcCartridge(sgcPageCount128k, false);
  }
  else if (lineRead == "READ 256K SGC CARTRIDGE")
  {
    ReadSgcCartridge(sgcPageCount256k, false);
  }
  else if (lineRead == "READ 512K SGC CARTRIDGE")
  {
    ReadSgcCartridge(sgcPageCount512k, false);
  }
  else if (lineRead == "STATUS")
  {
    Serial.println("CVCARTSCANNER");
  }
  else if (lineRead == "TEST")
  {
    Test();
  }
}
