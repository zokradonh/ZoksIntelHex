using System.Collections.Generic;
using System.Linq;

namespace ZoksIntelHex
{
    internal static class Utility
    {
        public static byte CalculateChecksum(IEnumerable<byte> bytes)
        {
            var sum = 0;
            foreach (var b in bytes)
            {
                sum += b;
            }
            return (byte) (((sum & 0xFF) ^ 0xFF) + 1);
        }
    }
}
