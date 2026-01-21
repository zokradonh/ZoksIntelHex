using System;
using System.IO;
using System.Text;
using Xunit;
using ZoksIntelHex;

namespace ZoksIntelHex.Tests
{
    public class IntelHexTests
    {
        [Fact]
        public void ReadAndWrite_SimpleData_ShouldMatch()
        {
            // Arrange
            var hexFile = new IntelHexFile();
            var segment = new DataSegment { StartAddress = 0x100 };
            segment.Bytes.AddRange(new byte[] { 0x01, 0x02, 0x03, 0x04 });
            hexFile.DataSegments.Add(segment);

            // Act
            var hexString = hexFile.CreateIntelHex();
            
            var newHexFile = new IntelHexFile();
            using (var ms = new MemoryStream(Encoding.ASCII.GetBytes(hexString)))
            {
                newHexFile.ReadIntelHex(ms);
            }

            // Assert
            Assert.Single(newHexFile.DataSegments);
            Assert.Equal(0x100u, newHexFile.DataSegments[0].StartAddress);
            Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04 }, newHexFile.DataSegments[0].Bytes);
        }

        [Fact]
        public void Read_ValidIhexString_ShouldParseCorrectly()
        {
            // Arrange
            // :04 0100 00 01020304 Checksum
            // Checksum = (0x04 + 0x01 + 0x00 + 0x00 + 0x01 + 0x02 + 0x03 + 0x04) = 0x0F
            // (0x0F ^ 0xFF) + 1 = 0xF1
            var ihex = ":0401000001020304F1\r\n:00000001FF";
            var hexFile = new IntelHexFile();

            // Act
            using (var ms = new MemoryStream(Encoding.ASCII.GetBytes(ihex)))
            {
                hexFile.ReadIntelHex(ms);
            }

            // Assert
            Assert.Single(hexFile.DataSegments);
            Assert.Equal(0x100u, hexFile.DataSegments[0].StartAddress);
            Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04 }, hexFile.DataSegments[0].Bytes);
        }

        [Fact]
        public void Read_MultipleSegments_ShouldParseCorrectly()
        {
            // Arrange
            // :02 0100 00 0102 Checksum
            // 02 + 01 + 00 + 00 + 01 + 02 = 06 -> (06^FF)+1 = FA
            // :02 0200 00 0304 Checksum
            // 02 + 02 + 00 + 00 + 03 + 04 = 0B -> (0B^FF)+1 = F5
            var ihex = ":020100000102FA\r\n:020200000304F5\r\n:00000001FF";
            var hexFile = new IntelHexFile();

            // Act
            using (var ms = new MemoryStream(Encoding.ASCII.GetBytes(ihex)))
            {
                hexFile.ReadIntelHex(ms);
            }

            // Assert
            Assert.Equal(2, hexFile.DataSegments.Count);
            Assert.Equal(0x100u, hexFile.DataSegments[0].StartAddress);
            Assert.Equal(new byte[] { 0x01, 0x02 }, hexFile.DataSegments[0].Bytes);
            Assert.Equal(0x200u, hexFile.DataSegments[1].StartAddress);
            Assert.Equal(new byte[] { 0x03, 0x04 }, hexFile.DataSegments[1].Bytes);
        }

        [Fact]
        public void Read_ExtendedLinearAddress_ShouldUpdateCurrentAddress()
        {
            // Arrange
            // :02 0000 04 0001 Checksum
            // 02 + 00 + 00 + 04 + 00 + 01 = 07 -> (07^FF)+1 = F9
            // :02 0100 00 0102 Checksum
            // 02 + 01 + 00 + 00 + 01 + 02 = 06 -> (06^FF)+1 = FA
            var ihex = ":020000040001F9\r\n:020100000102FA\r\n:00000001FF";
            var hexFile = new IntelHexFile();

            // Act
            using (var ms = new MemoryStream(Encoding.ASCII.GetBytes(ihex)))
            {
                hexFile.ReadIntelHex(ms);
            }

            // Assert
            Assert.Single(hexFile.DataSegments);
            Assert.Equal(0x10100u, hexFile.DataSegments[0].StartAddress); // 0x0001 << 16 + 0x0100
            Assert.Equal(new byte[] { 0x01, 0x02 }, hexFile.DataSegments[0].Bytes);
        }
    }
}
