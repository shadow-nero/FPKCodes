using System;
using System.Linq;
using System.Collections.Generic;

namespace FpkCodes;

public class CRC32 
{         
        public static uint CalCRC32(byte[] input)
    {
        uint crc32 = 0xFFFFFFFF;
        byte[] bytes = input;

        for (int i = 0; i < bytes.Length; i++)
        {
            crc32 ^= (uint)bytes[i];
            for (int j = 0; j < 8; j++)
            {
                if ((crc32 & 1) == 1)
                {
                    crc32 = (crc32 >> 1) ^ 0xEDB88320; // Polynomial
                }
                else
                {
                    crc32 = crc32 >> 1;
                }
            }
        }

        crc32 ^= 0xFFFFFFFF;
        return crc32;
    }
}
