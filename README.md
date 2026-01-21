# ZoksIntelHex

[![Build and Test](https://github.com/ZokRadonh/ZoksIntelHex/actions/workflows/dotnet.yml/badge.svg)](https://github.com/ZokRadonh/ZoksIntelHex/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/ZoksIntelHex.svg)](https://www.nuget.org/packages/ZoksIntelHex)

A lightweight C# library for reading and writing Intel HEX files.

## Features

- **Read Support**: Parse I8HEX, I16HEX, and I32HEX files.
- **Write Support**: Generate I8HEX format strings from data segments.

## Installation

Install via NuGet:

```bash
dotnet add package ZoksIntelHex
```

## Usage

### Reading an Intel HEX File

```csharp
using System.IO;
using ZoksIntelHex;

// ...

var hexFile = new IntelHexFile();
using (var stream = File.OpenRead("path/to/your.hex"))
{
    hexFile.ReadIntelHex(stream);
}

foreach (var segment in hexFile.DataSegments)
{
    Console.WriteLine($"Segment at 0x{segment.StartAddress:X8}, Length: {segment.Bytes.Count} bytes");
    // Access bytes via segment.Bytes
}
```

### Creating an Intel HEX File

```csharp
using ZoksIntelHex;

var hexFile = new IntelHexFile();

var segment = new DataSegment
{
    StartAddress = 0x0000,
    Bytes = new List<byte> { 0x01, 0x02, 0x03, 0x04 }
};

hexFile.DataSegments.Add(segment);

string hexContent = hexFile.CreateIntelHex();
File.WriteAllText("output.hex", hexContent);
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
