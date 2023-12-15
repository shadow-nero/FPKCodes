using System;
using System.Linq;
using System.Collections.Generic;

namespace FpkCodes;

    public class PRSUncompressor
    {
        private readonly byte[] input;
        private readonly int outputLength;
        private int inputIndex;
        private int bitsLeft;
        private byte flagByte;

        public PRSUncompressor(byte[] input, int outputLength)
        {
            this.input = input;
            this.outputLength = outputLength;
            inputIndex = 0;
            bitsLeft = 0;
            flagByte = 0;
        }

        public byte[] Uncompress()
        {
            byte[] output = new byte[outputLength];
            int outputPtr = 0;
            int flag;
            int len;
            int pos;
            while (inputIndex < input.Length)
            {
                flag = GetFlagBits(1);
                if (flag == 1) // Uncompressed value
                {
                    if (outputPtr < output.Length)
                        output[outputPtr++] = input[inputIndex++];
                }
                else // Compressed value
                {
                    flag = GetFlagBits(1);
                    if (flag == 0) // Short search (length between 2 and 5)
                    {
                        len = GetFlagBits(2) + 2;
                        pos = input[inputIndex++] & 0xff | unchecked((int)0xffffff00);
                    }
                    else // Long search
                    {
                        pos = (input[inputIndex++] << 8) | unchecked((int)0xffff0000);
                        pos |= input[inputIndex++] & 0xff;
                        len = pos & 0x07;
                        pos >>= 3;
                        if (len == 0)
                        {
                            len = (input[inputIndex++] & 0xff) + 1;
                        }
                        else
                        {
                            len += 2;
                        }
                    }
                    pos += outputPtr;
                    for (int i = 0; i < len; i++)
                    {
                        if (outputPtr < output.Length)
                            output[outputPtr++] = output[pos++];
                    }
                }
            }
            return output;
        }

        private int GetFlagBits(int n)
        {
            int bits = 0;
            while (n > 0)
            {
                bits <<= 1;
                if (bitsLeft == 0)
                {
                    flagByte = input[inputIndex];
                    inputIndex++;
                    bitsLeft = 8;
                }
                if ((flagByte & 0x80) > 0)
                {
                    bits |= 1;
                }
                flagByte <<= 1;
                bitsLeft--;
                n--;
            }
            return bits;
        }
    }


