using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZoksIntelHex
{
    public class DataSegment
    {
        public uint StartAddress { get; set; }
        public List<byte> Bytes { get; set; } = new List<byte>();
        public uint EndAddress => (uint) (StartAddress + Bytes.Count);

        internal string ToIntelHex()
        {
            var str = new StringBuilder((int) (Bytes.Count * 2.5)); // each byte doubled + 24% overhead (doubled again)
            for (var i = 0; i < Bytes.Count; i += 16)
            {
                // TODO: Support I16HEX and I32HEX
                var lineNumBytes = Math.Min(16, Bytes.Count - i);
                var lineAddress = StartAddress + (uint)i;
                
                var lineBytes = new byte[lineNumBytes];
                Bytes.CopyTo(i, lineBytes, 0, lineNumBytes);

                var sum = (byte)lineNumBytes;
                sum += (byte)(lineAddress & 0xFF);
                sum += (byte)((lineAddress >> 8) & 0xFF);
                foreach (var b in lineBytes) sum += b;

                var checksum = (byte)((sum ^ 0xFF) + 1);

                str.AppendFormat(":{0:X2}{1:X4}00", lineNumBytes, lineAddress & 0xFFFF);
                foreach (var b in lineBytes)
                {
                    str.AppendFormat("{0:X2}", b);
                }
                str.AppendFormat("{0:X2}", checksum);
                str.AppendLine();
            }
            return str.ToString();
        }
    }
}
