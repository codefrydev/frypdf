using System.Collections.Generic;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;

namespace PdfEditorApp.Templates.Technical;

public class ArduinoCheatSheetTemplate : ITemplateDefinition
{
    public string Id => "arduino_cheatsheet";
    public string Name => "Technical Cheat Sheet (Embedded C & Arduino)";
    public string Description => "High-density developer reference poster featuring multi-column code blocks, control structures, pinout tables, and electrical ratings";
    public string Category => "Cheat Sheets";
    public string IconKind => "Chip";
    public string AccentColorHex => "#0D9488";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Arduino_Developer_Reference_CheatSheet.pdf",
            Author = "Embedded Engineering Systems",
            Subject = "Technical Reference Poster: Arduino Language, Hardware Architecture & Pinout Map"
        };

        // Standard Landscape Blueprint Canvas (1920 x 1080 pt widescreen or 1600 x 1131 pt)
        // We use 1600 x 1131 pt for clean viewing on all desktop screens
        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.Poster,
            Orientation = PageOrientation.Landscape,
            Width = 1600,
            Height = 1131,
            BackgroundColorHex = "#0F172A", // Sleek dark blueprint theme
            HeaderLeft = "Embedded Systems Technical Reference Series",
            HeaderCenter = "Arduino & Microcontroller Architecture Reference",
            HeaderRight = "Rev 2.6 • Hardware Spec",
            FooterLeft = "Embedded C/C++ Reference Cheat Sheet • Open-Hardware Reference",
            FooterCenter = "Compatible with ATmega328P, AVR, RP2040 & ESP32 Platforms",
            FooterRight = "Page 1 of 1",
            Elements = new List<PdfElementBase>
            {
                // Top Header Brand Band
                new PdfShapeElement
                {
                    X = 0, Y = 0, Width = 1600, Height = 10,
                    FillColorHex = "#0D9488", StrokeColorHex = "#00000000"
                },

                // Main Header Banner
                new PdfShapeElement
                {
                    X = 30, Y = 25, Width = 1540, Height = 75,
                    CornerRadius = 8, FillColorHex = "#1E293B",
                    StrokeColorHex = "#334155", StrokeThickness = 1.5
                },
                new PdfTextElement
                {
                    X = 50, Y = 36, Width = 1000, Height = 30,
                    Text = "ARDUINO / EMBEDDED C++ QUICK REFERENCE CHEAT SHEET",
                    FontSize = 18, IsBold = true, TextColorHex = "#2DD4BF",
                    FontFamily = "Inter"
                },
                new PdfTextElement
                {
                    X = 50, Y = 68, Width = 1000, Height = 22,
                    Text = "Language Syntax • Hardware Timers • Serial Comm • Digital/Analog I/O • Memory & Pinout Architecture",
                    FontSize = 11, TextColorHex = "#94A3B8",
                    FontFamily = "Inter"
                },
                new PdfShapeElement
                {
                    X = 1350, Y = 38, Width = 200, Height = 48,
                    CornerRadius = 6, FillColorHex = "#0F766E",
                    StrokeColorHex = "#14B8A6", StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 1355, Y = 52, Width = 190, Height = 20,
                    Text = "PLATFORM: ATmega328P",
                    FontSize = 11, IsBold = true, TextColorHex = "#FFFFFF",
                    Alignment = TextAlignmentMode.Center,
                    FontFamily = "Inter"
                },

                // =========================================================================
                // COLUMN 1: STRUCTURE & FLOW (X: 30, W: 365)
                // =========================================================================
                // Card 1.1: Program Structure
                new PdfShapeElement { X = 30, Y = 115, Width = 365, Height = 220, CornerRadius = 6, FillColorHex = "#1E293B", StrokeColorHex = "#334155", StrokeThickness = 1 },
                new PdfShapeElement { X = 30, Y = 115, Width = 365, Height = 30, CornerRadius = 6, FillColorHex = "#0D9488", StrokeColorHex = "#00000000" },
                new PdfTextElement { X = 45, Y = 122, Width = 335, Height = 20, Text = "1. Basic Program Structure", FontSize = 12, IsBold = true, TextColorHex = "#FFFFFF" },
                new PdfTextElement
                {
                    X = 45, Y = 152, Width = 335, Height = 175,
                    Text = "void setup() {\n  // Runs once at power-up or reset\n  pinMode(LED_BUILTIN, OUTPUT);\n  Serial.begin(115200);\n}\n\nvoid loop() {\n  // Runs repeatedly and continuously\n  digitalWrite(LED_BUILTIN, HIGH);\n  delay(500);\n  digitalWrite(LED_BUILTIN, LOW);\n  delay(500);\n}",
                    FontSize = 10, TextColorHex = "#E2E8F0",
                    FontFamily = "RobotoMono"
                },

                // Card 1.2: Control Flow & Loops
                new PdfShapeElement { X = 30, Y = 345, Width = 365, Height = 290, CornerRadius = 6, FillColorHex = "#1E293B", StrokeColorHex = "#334155", StrokeThickness = 1 },
                new PdfShapeElement { X = 30, Y = 345, Width = 365, Height = 30, CornerRadius = 6, FillColorHex = "#0D9488", StrokeColorHex = "#00000000" },
                new PdfTextElement { X = 45, Y = 352, Width = 335, Height = 20, Text = "2. Control Structures & Branching", FontSize = 12, IsBold = true, TextColorHex = "#FFFFFF" },
                new PdfTextElement
                {
                    X = 45, Y = 382, Width = 335, Height = 245,
                    Text = "if (val > 100) {\n  // condition true\n} else if (val == 50) {\n  // secondary branch\n} else {\n  // default fallback\n}\n\nfor (int i = 0; i < 10; i++) {\n  // counted loop\n}\n\nwhile (condition) {\n  // while true\n}\n\ndo { ... } while (condition);\nswitch(var) { case 1: break; default: break; }",
                    FontSize = 9.5, TextColorHex = "#E2E8F0",
                    FontFamily = "RobotoMono"
                },

                // Card 1.3: Data Types & RAM Footprint
                new PdfShapeElement { X = 30, Y = 645, Width = 365, Height = 340, CornerRadius = 6, FillColorHex = "#1E293B", StrokeColorHex = "#334155", StrokeThickness = 1 },
                new PdfShapeElement { X = 30, Y = 645, Width = 365, Height = 30, CornerRadius = 6, FillColorHex = "#0D9488", StrokeColorHex = "#00000000" },
                new PdfTextElement { X = 45, Y = 652, Width = 335, Height = 20, Text = "3. Variable Types & Memory", FontSize = 12, IsBold = true, TextColorHex = "#FFFFFF" },
                new PdfTextElement
                {
                    X = 45, Y = 682, Width = 335, Height = 295,
                    Text = "boolean   1 bit   true / false\nbyte      8 bits  0 to 255 (unsigned char)\nchar      8 bits  -128 to 127 ('A')\nint      16 bits  -32,768 to 32,767\nunsigned 16 bits  0 to 65,535\nword     16 bits  same as unsigned int\nlong     32 bits  -2,147,483,648 to 2,147,483,647\nu_long   32 bits  0 to 4,294,967,295\nfloat    32 bits  3.4028235E+38 (6-7 digits precision)\ndouble   32 bits  identical to float on AVR\nconst    qualifier marks variable read-only\nvolatile qualifier prevents compiler optimization",
                    FontSize = 9.5, TextColorHex = "#E2E8F0",
                    FontFamily = "RobotoMono"
                },

                // =========================================================================
                // COLUMN 2: DIGITAL & ANALOG I/O (X: 420, W: 365)
                // =========================================================================
                // Card 2.1: Digital I/O
                new PdfShapeElement { X = 420, Y = 115, Width = 365, Height = 230, CornerRadius = 6, FillColorHex = "#1E293B", StrokeColorHex = "#334155", StrokeThickness = 1 },
                new PdfShapeElement { X = 420, Y = 115, Width = 365, Height = 30, CornerRadius = 6, FillColorHex = "#2563EB", StrokeColorHex = "#00000000" },
                new PdfTextElement { X = 435, Y = 122, Width = 335, Height = 20, Text = "4. Digital I/O Operations", FontSize = 12, IsBold = true, TextColorHex = "#FFFFFF" },
                new PdfTextElement
                {
                    X = 435, Y = 152, Width = 335, Height = 185,
                    Text = "pinMode(pin, mode);\n  // mode: INPUT, OUTPUT, INPUT_PULLUP\n\ndigitalWrite(pin, value);\n  // value: HIGH (5V/3.3V), LOW (0V)\n\nint state = digitalRead(pin);\n  // returns HIGH or LOW\n\n// Fast direct port manipulation:\nPORTB |= (1 << PB5);  // High Pin 13\nPORTB &= ~(1 << PB5); // Low Pin 13",
                    FontSize = 9.5, TextColorHex = "#E2E8F0",
                    FontFamily = "RobotoMono"
                },

                // Card 2.2: Analog I/O & PWM
                new PdfShapeElement { X = 420, Y = 355, Width = 365, Height = 250, CornerRadius = 6, FillColorHex = "#1E293B", StrokeColorHex = "#334155", StrokeThickness = 1 },
                new PdfShapeElement { X = 420, Y = 355, Width = 365, Height = 30, CornerRadius = 6, FillColorHex = "#2563EB", StrokeColorHex = "#00000000" },
                new PdfTextElement { X = 435, Y = 362, Width = 335, Height = 20, Text = "5. Analog I/O & PWM Duty", FontSize = 12, IsBold = true, TextColorHex = "#FFFFFF" },
                new PdfTextElement
                {
                    X = 435, Y = 392, Width = 335, Height = 205,
                    Text = "analogReference(type);\n  // DEFAULT, INTERNAL, EXTERNAL\n\nint raw = analogRead(A0);\n  // 10-bit ADC returns 0 to 1023 (0 to 5V)\n  // Voltage = raw * (5.0 / 1023.0);\n\nanalogWrite(pin, duty);\n  // 8-bit PWM (0 to 255) at 490Hz / 980Hz\n  // PWM Pins: 3, 5, 6, 9, 10, 11 on Uno",
                    FontSize = 9.5, TextColorHex = "#E2E8F0",
                    FontFamily = "RobotoMono"
                },

                // Card 2.3: Timing & Delays
                new PdfShapeElement { X = 420, Y = 615, Width = 365, Height = 175, CornerRadius = 6, FillColorHex = "#1E293B", StrokeColorHex = "#334155", StrokeThickness = 1 },
                new PdfShapeElement { X = 420, Y = 615, Width = 365, Height = 30, CornerRadius = 6, FillColorHex = "#2563EB", StrokeColorHex = "#00000000" },
                new PdfTextElement { X = 435, Y = 622, Width = 335, Height = 20, Text = "6. Timing & Scheduling", FontSize = 12, IsBold = true, TextColorHex = "#FFFFFF" },
                new PdfTextElement
                {
                    X = 435, Y = 652, Width = 335, Height = 130,
                    Text = "unsigned long ms = millis();\n  // Milliseconds since startup (overflow ~50 days)\n\nunsigned long us = micros();\n  // Microseconds since startup (overflow ~70 mins)\n\ndelay(ms);        // Blocking delay in milliseconds\ndelayMicroseconds(us); // Busy-wait in microseconds",
                    FontSize = 9.5, TextColorHex = "#E2E8F0",
                    FontFamily = "RobotoMono"
                },

                // Card 2.4: Math & Bitwise
                new PdfShapeElement { X = 420, Y = 800, Width = 365, Height = 185, CornerRadius = 6, FillColorHex = "#1E293B", StrokeColorHex = "#334155", StrokeThickness = 1 },
                new PdfShapeElement { X = 420, Y = 800, Width = 365, Height = 30, CornerRadius = 6, FillColorHex = "#2563EB", StrokeColorHex = "#00000000" },
                new PdfTextElement { X = 435, Y = 807, Width = 335, Height = 20, Text = "7. Math & Mapping Utilities", FontSize = 12, IsBold = true, TextColorHex = "#FFFFFF" },
                new PdfTextElement
                {
                    X = 435, Y = 837, Width = 335, Height = 140,
                    Text = "map(val, fromLow, fromHigh, toLow, toHigh);\nconstrain(x, a, b);  // clips x between a and b\nmin(x, y); max(x, y); abs(x); sq(x); sqrt(x);\npow(base, exponent); sin(rad); cos(rad); tan(rad);\nbitRead(x, n); bitSet(x, n); bitClear(x, n);\nbitWrite(x, n, b); bit(n); // (1 << n)",
                    FontSize = 9.5, TextColorHex = "#E2E8F0",
                    FontFamily = "RobotoMono"
                },

                // =========================================================================
                // COLUMN 3: SERIAL & BUS PROTOCOLS (X: 810, W: 365)
                // =========================================================================
                // Card 3.1: Hardware UART Serial
                new PdfShapeElement { X = 810, Y = 115, Width = 365, Height = 250, CornerRadius = 6, FillColorHex = "#1E293B", StrokeColorHex = "#334155", StrokeThickness = 1 },
                new PdfShapeElement { X = 810, Y = 115, Width = 365, Height = 30, CornerRadius = 6, FillColorHex = "#D97706", StrokeColorHex = "#00000000" },
                new PdfTextElement { X = 825, Y = 122, Width = 335, Height = 20, Text = "8. Hardware UART Serial", FontSize = 12, IsBold = true, TextColorHex = "#FFFFFF" },
                new PdfTextElement
                {
                    X = 825, Y = 152, Width = 335, Height = 205,
                    Text = "Serial.begin(baud);\n  // Typical baud: 9600, 115200, 230400\n\nwhile (!Serial); // Wait for USB on Leonardo/RP2040\nSerial.print(\"Value: \");\nSerial.println(val, HEX); // BIN, DEC, HEX, OCT\n\nif (Serial.available() > 0) {\n  int byteIn = Serial.read();\n  String str = Serial.readStringUntil('\\n');\n}\nSerial.flush(); // Waits for transmission complete",
                    FontSize = 9.5, TextColorHex = "#E2E8F0",
                    FontFamily = "RobotoMono"
                },

                // Card 3.2: I2C & SPI Communication
                new PdfShapeElement { X = 810, Y = 375, Width = 365, Height = 270, CornerRadius = 6, FillColorHex = "#1E293B", StrokeColorHex = "#334155", StrokeThickness = 1 },
                new PdfShapeElement { X = 810, Y = 375, Width = 365, Height = 30, CornerRadius = 6, FillColorHex = "#D97706", StrokeColorHex = "#00000000" },
                new PdfTextElement { X = 825, Y = 382, Width = 335, Height = 20, Text = "9. I2C (Wire) & SPI Protocols", FontSize = 12, IsBold = true, TextColorHex = "#FFFFFF" },
                new PdfTextElement
                {
                    X = 825, Y = 412, Width = 335, Height = 225,
                    Text = "#include <Wire.h>\nWire.begin();           // Join as Master\nWire.beginTransmission(0x68); // Device address\nWire.write(0x3B);       // Register address\nWire.endTransmission();\nWire.requestFrom(0x68, 6);\nwhile (Wire.available()) { byte c = Wire.read(); }\n\n#include <SPI.h>\nSPI.begin();\nSPI.beginTransaction(SPISettings(14000000, MSBFIRST, SPI_MODE0));\ndigitalWrite(SS, LOW);\nbyte response = SPI.transfer(0xAA);\ndigitalWrite(SS, HIGH);\nSPI.endTransaction();",
                    FontSize = 9.5, TextColorHex = "#E2E8F0",
                    FontFamily = "RobotoMono"
                },

                // Card 3.3: External Hardware Interrupts
                new PdfShapeElement { X = 810, Y = 655, Width = 365, Height = 330, CornerRadius = 6, FillColorHex = "#1E293B", StrokeColorHex = "#334155", StrokeThickness = 1 },
                new PdfShapeElement { X = 810, Y = 655, Width = 365, Height = 30, CornerRadius = 6, FillColorHex = "#D97706", StrokeColorHex = "#00000000" },
                new PdfTextElement { X = 825, Y = 662, Width = 335, Height = 20, Text = "10. External Interrupts (ISR)", FontSize = 12, IsBold = true, TextColorHex = "#FFFFFF" },
                new PdfTextElement
                {
                    X = 825, Y = 692, Width = 335, Height = 285,
                    Text = "attachInterrupt(digitalPinToInterrupt(pin), ISR, mode);\n\nModes:\n  LOW:     Trigger whenever pin is LOW\n  CHANGE:  Trigger on any state transition\n  RISING:  Trigger on LOW to HIGH\n  FALLING: Trigger on HIGH to LOW\n\nISR Rules:\n  - Keep execution ultra-short (no delay() or Serial)\n  - All shared variables MUST be marked 'volatile'\n\nvolatile int counter = 0;\nvoid myISR() {\n  counter++;\n}\n\ndetachInterrupt(digitalPinToInterrupt(pin));",
                    FontSize = 9.5, TextColorHex = "#E2E8F0",
                    FontFamily = "RobotoMono"
                },

                // =========================================================================
                // COLUMN 4: PINOUT MATRIX & ELECTRICAL RATINGS (X: 1200, W: 370)
                // =========================================================================
                // Card 4.1: Pinout Table
                new PdfShapeElement { X = 1200, Y = 115, Width = 370, Height = 580, CornerRadius = 6, FillColorHex = "#1E293B", StrokeColorHex = "#334155", StrokeThickness = 1 },
                new PdfShapeElement { X = 1200, Y = 115, Width = 370, Height = 30, CornerRadius = 6, FillColorHex = "#9333EA", StrokeColorHex = "#00000000" },
                new PdfTextElement { X = 1215, Y = 122, Width = 340, Height = 20, Text = "11. Hardware Pinout & Port Map", FontSize = 12, IsBold = true, TextColorHex = "#FFFFFF" },

                new PdfTableElement
                {
                    X = 1205, Y = 155, Width = 360, Height = 530,
                    HeaderBackgroundHex = "#6B21A8",
                    HeaderTextHex = "#FFFFFF",
                    BorderColorHex = "#334155",
                    AlternateRowBackgroundHex = "#0F172A",
                    Headers = new List<string> { "Pin", "Port", "Function", "PWM", "Notes" },
                    Rows = new List<List<string>>
                    {
                        new() { "D0", "PD0", "RX", "-", "Hardware Serial In" },
                        new() { "D1", "PD1", "TX", "-", "Hardware Serial Out" },
                        new() { "D2", "PD2", "INT0", "-", "External Interrupt 0" },
                        new() { "D3", "PD3", "INT1", "Yes", "Ext Int 1 / 490Hz" },
                        new() { "D4", "PD4", "T0", "-", "Timer 0 ext clock" },
                        new() { "D5", "PD5", "T1", "Yes", "Timer 1 / 980Hz" },
                        new() { "D6", "PD6", "AIN0", "Yes", "Analog comp / 980Hz" },
                        new() { "D7", "PD7", "AIN1", "-", "Analog comparator" },
                        new() { "D8", "PB0", "ICP1", "-", "Timer 1 input capture" },
                        new() { "D9", "PB1", "OC1A", "Yes", "Timer 1 / 490Hz" },
                        new() { "D10", "PB2", "SS", "Yes", "SPI Chip Select" },
                        new() { "D11", "PB3", "MOSI", "Yes", "SPI Master Out" },
                        new() { "D12", "PB4", "MISO", "-", "SPI Master In" },
                        new() { "D13", "PB5", "SCK", "-", "SPI Clock / Built-in LED" },
                        new() { "A0", "PC0", "ADC0", "-", "Analog input 0" },
                        new() { "A1", "PC1", "ADC1", "-", "Analog input 1" },
                        new() { "A2", "PC2", "ADC2", "-", "Analog input 2" },
                        new() { "A3", "PC3", "ADC3", "-", "Analog input 3" },
                        new() { "A4", "PC4", "SDA", "-", "I2C Data line" },
                        new() { "A5", "PC5", "SCL", "-", "I2C Clock line" }
                    }
                },

                // Card 4.2: Electrical Ratings
                new PdfShapeElement { X = 1200, Y = 705, Width = 370, Height = 280, CornerRadius = 6, FillColorHex = "#1E293B", StrokeColorHex = "#334155", StrokeThickness = 1 },
                new PdfShapeElement { X = 1200, Y = 705, Width = 370, Height = 30, CornerRadius = 6, FillColorHex = "#DC2626", StrokeColorHex = "#00000000" },
                new PdfTextElement { X = 1215, Y = 712, Width = 340, Height = 20, Text = "12. Electrical Limits & Ratings", FontSize = 12, IsBold = true, TextColorHex = "#FFFFFF" },
                new PdfTextElement
                {
                    X = 1215, Y = 742, Width = 340, Height = 235,
                    Text = "Operating Voltage:        5.0V\nInput Voltage (VIN):       7.0V - 12.0V (Limit: 6-20V)\nMax DC Current per I/O:   40 mA (Safe: 20 mA)\nTotal Microcontroller DC: 200 mA (Absolute Max VCC/GND)\n3.3V Pin Current Limit:   50 mA (From onboard LDO)\nFlash Memory:             32 KB (0.5 KB bootloader)\nSRAM:                     2 KB (Static RAM)\nEEPROM:                   1 KB (Internal non-volatile)\nClock Speed:              16 MHz Quartz Crystal\nADC Resolution:           10-bit (1024 discrete steps)",
                    FontSize = 9.5, TextColorHex = "#E2E8F0",
                    FontFamily = "RobotoMono"
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }
}
