using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ZoksIntelHex
{
    /// <summary>
    /// Supports read(I8HEX, I16HEX, I32HEX) and write(I8HEX) of Intel Hex Format files.
    /// </summary>
    public class IntelHexFile
    {
        /// <summary>
        /// Data segments that are read by ReadIntelHex or that should be used for CreateIntelHex.
        /// </summary>
        public List<DataSegment> DataSegments { get; } = new List<DataSegment>();

        public ushort CS { get; set; }
        public ushort IP { get; set; }
        public uint EIP { get; set; }

        private uint _currentAddress = 0;

        private const byte RecordTypeData = 0;
        private const byte RecordTypeEof = 1;
        private const byte RecordTypeExtendedSegmentAddress = 2;
        private const byte RecordTypeStartSegmentAddress = 3;
        private const byte RecordTypeExtendedLinearAddress = 4;
        private const byte RecordTypeStartLinearAddress = 5;

        /// <summary>
        /// Create Intel hex format string of data in DataSegments.
        /// </summary>
        /// <returns></returns>
        public string CreateIntelHex()
        {
            var data = string.Concat(DataSegments.Select(segment => segment.ToIntelHex()));
            return data + $":000000{RecordTypeEof:X2}FF\r\n"; // add eof record
        }

        /// <summary>
        /// Read Intel hex format data.
        /// </summary>
        /// <param name="stream"></param>
        public void ReadIntelHex(Stream stream)
        {
            using var r = new StreamReader(stream, Encoding.ASCII, true, 1024, leaveOpen: true);
            var line = r.ReadLine();
            while (line != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line))
                {
                    line = r.ReadLine();
                    continue;
                }

                try
                {
                    if (line[0] != ':')
                    {
                        throw new InvalidDataException($"Invalid data format. Expected ':' instead of '{line[0]}'.");
                    }

                    if (!int.TryParse(line.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var dataLen))
                    {
                        throw new InvalidDataException($"Invalid data format. Expected line length number instead of '{line.Substring(1, 2)}'.");
                    }

                    if (!int.TryParse(line.Substring(3, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var recordAddress))
                    {
                        throw new InvalidDataException($"Invalid data format. Expected record address instead of '{line.Substring(3, 4)}'.");
                    }

                    if (!int.TryParse(line.Substring(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var recordType))
                    {
                        throw new InvalidDataException($"Invalid data format. Expected record type instead of '{line.Substring(7, 2)}'.");
                    }

                    var dataAscii = line.Substring(9, dataLen * 2);
                    if (!byte.TryParse(line.Substring(9 + dataLen * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var checkSum))
                    {
                        throw new InvalidDataException($"Invalid data format. Expected checksum instead of '{line.Substring(9 + dataLen * 2, 2)}'.");
                    }

                    try
                    {
                        // get data as bytes
                        var dataBytes = new byte[dataLen];
                        for (int i = 0; i < dataLen; i++)
                        {
                            dataBytes[i] = Convert.ToByte(dataAscii.Substring(i * 2, 2), 16);
                        }

                        // verify checksum
                        var sum = (byte)dataLen;
                        sum += (byte)recordType;
                        sum += (byte)(recordAddress & 0xFF);
                        sum += (byte)((recordAddress >> 8) & 0xFF);
                        foreach (var b in dataBytes) sum += b;

                        var calculatedChecksum = (byte)((sum ^ 0xFF) + 1);

                        if (checkSum != calculatedChecksum)
                        {
                            throw new InvalidDataException($"Invalid record checksum. Expected '{checkSum:X2}' but calculated '{calculatedChecksum:X2}'.");
                        }

                        // read record
                        switch (recordType)
                        {
                            case RecordTypeData: // Data
                                GetSegment((uint)(recordAddress + _currentAddress)).Bytes.AddRange(dataBytes);
                                break;
                            case RecordTypeEof: // End of file
                                return;
                            case RecordTypeExtendedSegmentAddress: // Extended Segment Address
                                _currentAddress = Convert.ToUInt32(dataAscii, 16) * 16U;
                                break;
                            case RecordTypeStartSegmentAddress: // Start Segment Address
                                CS = Convert.ToUInt16(dataAscii.Substring(0, 2), 16);
                                IP = Convert.ToUInt16(dataAscii.Substring(2, 2), 16);
                                break;
                            case RecordTypeExtendedLinearAddress: // Extended Linear Address
                                _currentAddress = Convert.ToUInt32(dataAscii, 16) << 16;
                                break;
                            case RecordTypeStartLinearAddress: // Start Linear Address
                                EIP = Convert.ToUInt32(dataAscii.Substring(0, 4), 16);
                                break;
                            default:
                                throw new InvalidDataException($"Invalid data format. Unknown record type: '{recordType:D2}'.");
                        }
                    }
                    catch (FormatException)
                    {
                        throw new InvalidDataException($"Invalid data format. Data payload is invalid: '{dataAscii}'.");
                    }

                    line = r.ReadLine();
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new InvalidDataException($"Invalid data format. Unexpected end of line: '{line}'.");
                }
            }
        }

        /// <summary>
        /// Get appropriate segment or create new segment.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private DataSegment GetSegment(uint address)
        {
            var seg =
                DataSegments.FirstOrDefault(segment => segment.StartAddress < address && segment.EndAddress >= address);
            if (seg == null)
            {
                seg = new DataSegment { StartAddress = address };
                DataSegments.Add(seg);
            }
            return seg;
        }
    }
}
