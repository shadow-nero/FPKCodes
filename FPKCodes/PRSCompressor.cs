using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace FpkCodes
{
    public class PRSCompressor
    {
        private readonly byte[] input;
        private readonly byte[] output;

        private int flagIndex;
        private int inputIndex;
        private int outputIndex;
        private int flagBitIndex;
        private int currentCompressionLength;
        private int pos;

        public PRSCompressor(byte[] input)
        {
            this.input = input;
            flagIndex = 0;
            outputIndex = 1;
            flagBitIndex = 7;
            currentCompressionLength = 0;
            pos = 0;
            inputIndex = 0;
            output = new byte[input.Length * 2];
        }

        public byte[] Compress()
        {
            while (inputIndex < input.Length)
            {
                if (CheckWindow())
                {
                    WriteCompressedBytes();
                    inputIndex += currentCompressionLength;
                }
                else if (CheckRunLengthEncoding())
                {
                    WriteCompressedBytes();
                    inputIndex += currentCompressionLength;
                }
                else
                {
                    WriteUncompressedByte();
                    inputIndex++;
                }
            }
            TerminateFile();

            return output.Take(outputIndex + 3).ToArray();
        }

        private void TerminateFile()
        {
            WriteBit(0);
            WriteBit(1);
        }

        private bool CheckRunLengthEncoding()
        {
            currentCompressionLength = 0;
            pos = 1;
            if (inputIndex < 1)
            {
                return false;
            }
            int scanIndex = inputIndex;
            while (scanIndex < input.Length && input[inputIndex - 1] == input[scanIndex]
                && currentCompressionLength < 256)
            {
                currentCompressionLength++;
                scanIndex++;
            }

            return currentCompressionLength > 1;
        }

        private bool CheckWindow()
        {
            if (inputIndex < 1)
            {
                return false;
            }

            int maxMatchLength = 256;
            int bytesLeft = input.Length - inputIndex;
            if (bytesLeft < maxMatchLength)
            {
                maxMatchLength = bytesLeft + 1;
            }
            int matchLength = 1;
            int scanIndex = inputIndex - 1;
            int currentIndex = inputIndex;
            int savedIndex = scanIndex;

            while (((currentIndex - scanIndex) < 8192) && (scanIndex >= 0)
                && (matchLength < maxMatchLength))
            {
                while (Memcmp(currentIndex, scanIndex, matchLength) && (matchLength < maxMatchLength))
                {
                    savedIndex = scanIndex;
                    matchLength++;
                }
                scanIndex--;
            }

            matchLength--;
            currentCompressionLength = matchLength;
            pos = currentIndex - savedIndex;
            if ((matchLength == 2) && (pos > 255))
            {
                return false;
            }

            return matchLength >= 2;
        }

        private bool Memcmp(int index1, int index2, int size)
        {
            if (index1 + size > input.Length)
            {
                byte[] range1 = input.Skip(index1).Take(size).ToArray();
                byte[] range2 = input.Skip(index2).Take(size).ToArray();
                return range1.SequenceEqual(range2);
            }
            for (int i = 0; i < size; i++)
            {
                if (input[index1 + i] != input[index2 + i])
                {
                    return false;
                }
            }
            return true;
        }

        private void WriteBit(int bit)
        {
            if (flagBitIndex == -1)
            {
                flagBitIndex = 7;
                flagIndex = outputIndex;
                outputIndex = flagIndex + 1;
            }
            output[flagIndex] |= (byte)(bit << flagBitIndex);
            flagBitIndex--;
        }

        private void WriteBytesShortCompression(int len, int posy)
        {
            WriteBit(0);
            WriteBit(0);
            len -= 2;
            WriteBit((len >> 1) & 0x01);
            len = (len << 1) & 0x02;
            WriteBit((len >> 1) & 0x01);

            output[outputIndex++] = (byte)(~posy + 1);
        }

        private void WriteBytesLongCompression(int len, int posy)
        {
            WriteBit(0);
            WriteBit(1);

            posy = (~posy + 1) << 3;

            if (len <= 9)
            {
                posy |= ((len - 2) & 0x07);
            }

            output[outputIndex++] = (byte)(posy >> 8);
            output[outputIndex++] = (byte)posy;

            if (len > 9)
            {
                output[outputIndex++] = (byte)(len - 1);
            }
        }

        private void WriteCompressedBytes()
        {
            if (pos > 255 || currentCompressionLength > 5)
            {
                WriteBytesLongCompression(currentCompressionLength, pos);
            }
            else
            {
                WriteBytesShortCompression(currentCompressionLength, pos);
            }
        }

        private void WriteUncompressedByte()
        {
            WriteBit(1);
            output[outputIndex++] = input[inputIndex];
        }

        public override string ToString()
        {
            return $"\n\nPRSCompressor\nflagIndex=0x{flagIndex:X}\ninputIndex=0x{inputIndex:X}\noutputIndex=0x{outputIndex:X}\nflagBitIndex={flagBitIndex}\ncurrentCompressionLength={currentCompressionLength}\npos={pos}\nCurrent Flag Bits={ToBinary(output[flagIndex])}\nCurrent Input Byte=0x{input[inputIndex]:X}";
        }

        private string ToBinary(byte thisByte)
        {
            return string.Join("", Enumerable.Range(0, 8).Select(i => ((thisByte << i) & 0x80) == 0 ? '0' : '1'));
        }
    }
}

