using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FpkCodes
{
    public static class FateLba
    {
        //the code is from https://github.com/SpudManTwo/UMDReplaceK
        //Version Constants
        //const string VERSION = "20220519K";

        //Int Constants
        //const short DESCRIPTOR_LBA = 16; // 16 = 0x010 = LBA of the first volume descriptor
        //const short TOTAL_SECTORS = 80; // 80 = 0x050 = total number of sectors in the UMD, funnily enough, this also works for PS2 logic.
        //const short TABLE_PATH_LBA = 140; // 132 = 0x08C LBA of the first table path

        //M0 Constants
        const long sectorSize = 2048;// 2048 = 0x800 = LEN_SECTOR_M0 = sector size
        const uint sectorData = 2048; // 2048 = 0x800 = POS_DATA_M0 = sector data length
        const uint dataOffset = 0; // 0 = 0x000 = LEN_DATA_M0 = sector data start

        //Exit Code Definitions as per https://docs.microsoft.com/en-us/windows/win32/debug/system-error-codes
        //const int SUCCESS_EXIT_CODE = 0;
        //const int BAD_APPLICATION_ARGUMENTS_EXIT_CODE = 160;
        //const int FILE_NOT_FOUND_EXIT_CODE = 2;
        //const int FILE_NAME_TOO_LONG = 111;
        //const int CANNOT_OPEN_FILE_EXIT_CODE = 110;

        //static bool fileLock = false;

        static byte[][] originalIsoFile;
        //static byte[][] newIsoFile;
        static Dictionary<string, ulong> filesForReplacement = new Dictionary<string, ulong>();
        static Dictionary<string, uint> oldFileSizes = new Dictionary<string, uint>();
        static Dictionary<string, uint> oldFileLbas = new Dictionary<string, uint>();

        static Dictionary<string, uint> oldFileSectorCounts = new Dictionary<string, uint>();
        //static Dictionary<string, uint> newFileSizes = new Dictionary<string, uint>();
        //static Dictionary<string, uint> newFileSectorCounts = new Dictionary<string, uint>();
        //static Dictionary<string, byte[]> newFileSectors = new Dictionary<string, byte[]>();
        static uint rootLba = 0;
        static uint rootLength = 0;
        //static Random rngGenerator = new Random();


        //lets define originalIsoFileLength for saving memory
        static long originalIsoFileLength = 0x80000;
        static string SectorListName = "/PSP_GAME/USRDIR/data/fpack/sector.lst";
        public static void FIX(string isoName)
        {
            FileStream inputFileStream = new FileStream(isoName, FileMode.Open, FileAccess.ReadWrite);
            originalIsoFile = new byte[(originalIsoFileLength / Array.MaxLength) + 1][];
            for (int i = 0; i < originalIsoFile.Length - 1; i++)
                originalIsoFile[i] = new byte[Array.MaxLength];
            originalIsoFile[^1] = new byte[originalIsoFileLength % Array.MaxLength];

            for (int i = 0; inputFileStream.Position < originalIsoFileLength; i++)
            {
                //With the new addition of 2 dimensional arrays, we're going to await this call to make sure it finishes.
                inputFileStream.Read(originalIsoFile[i]);
                //And then flush the stream immediately after since the buffer is going to be very full.
                inputFileStream.Flush();
            }
            rootLba = BitConverter.ToUInt32(originalIsoFile[0][32926..32930]);
            rootLength = BitConverter.ToUInt32(originalIsoFile[0][32934..32938]);

            //start
            PrepareOldFileForReplacement(SectorListName);
            Console.WriteLine("Please wait...");
            int count = (int)(oldFileSizes[SectorListName] - 0xc) / 136;
            inputFileStream.Seek((oldFileLbas[SectorListName] * sectorSize) + 0xc, SeekOrigin.Begin);
            for (int i = 0; i < count; i++)
            {
                byte[] discBytes = new byte[6];
                inputFileStream.Read(discBytes, 0, 6);
                byte[] nameBytes = new byte[0x7a];
                inputFileStream.Read(nameBytes, 0, 0x7a);
                string name = Encoding.ASCII.GetString(nameBytes).Replace("\0", "");
                PrepareOldFileForReplacement(name);
                //write new lba
                inputFileStream.Write(BitConverter.GetBytes(oldFileLbas[name]), 0, 4);
                //write new size
                inputFileStream.Write(BitConverter.GetBytes(oldFileSizes[name]), 0, 4);
                //Console.WriteLine(name);

            }
            inputFileStream.Flush();
            inputFileStream.Close();
            Console.WriteLine("{0} Lba fixed!!", isoName);
        }
        static ulong Search(string fileName, string path, uint lba, uint len)
        {
            ulong totalSectors = (ulong)((len + sectorSize - 1) / sectorSize);
            for (uint i = 0; i < totalSectors; i++)
            {
                uint nBytes;
                for (long position = 0; position < sectorData && (dataOffset + position + 4) < originalIsoFileLength; position += nBytes)
                {
                    if (sectorSize * (lba + i) + dataOffset + position >= originalIsoFileLength)
                    {
                        break;
                    }
                    //field size
                    long arrayPos = sectorSize * (lba + i) + dataOffset + position;
                    nBytes = originalIsoFile[arrayPos / Array.MaxLength][arrayPos % Array.MaxLength];
                    if (nBytes == 0)
                    {
                        break;
                    }

                    //name size
                    long nCharsPos = sectorSize * (lba + i) + dataOffset + position + 32;
                    byte nChars = originalIsoFile[nCharsPos / Array.MaxLength][nCharsPos % Array.MaxLength]; //0x020 = 32

                    byte[] name = new byte[nChars];
                    //As the comments above and below state, we have to loop around bytes in the array like this. See the above explanation.
                    for (long nameArrayPos = sectorSize * (lba + i) + dataOffset + position + 33, namePos = 0; namePos < name.Length; nameArrayPos++, namePos++)
                        name[namePos] = originalIsoFile[nameArrayPos / Array.MaxLength][(int)(nameArrayPos % Array.MaxLength)];

                    // discard the ";1" final
                    if (nChars > 2 && name[nChars - 2] == 59) //0x3B = 59 = ';' in ASCII
                    {
                        nChars -= 2;
                        name[nChars] = 0;
                    }

                    string nameString = Encoding.ASCII.GetString(name);

                    // check the name except for '.' and '..' entries
                    if ((nChars != 1) || ((name[0] != 0) && (name[0] != 1))) // 0x1 = Repeat previous character in ASCII
                    {
                        //new path name
                        string newPath = $"{path}/{nameString}"; // sprintf is the string format for C. While the syntax looks different, the functionality is supposed to be the same.
                        long directoryMarkerPos = sectorSize * (lba + i) + dataOffset + position + 25;
                        var normalizedName = newPath.Contains('\0') ? newPath.Substring(0, newPath.IndexOf('\0')) : newPath; //Dear Atlus and Sony, why do you do this to me? Why did you hide a string terminating character in your file name with garbage after?

                        if (originalIsoFile[directoryMarkerPos / Array.MaxLength][directoryMarkerPos % Array.MaxLength] == 2) // 0x002 = 2
                        {
                            //Recursive Search Through Folders

                            byte[] uIntByteSwap = new byte[4];

                            //As the comments above and below state, we have to loop around bytes in the array like this. See the above explanation.
                            for (long newLbaPos = sectorSize * (lba + i) + dataOffset + position + 2, uIntBytePos = 0; uIntBytePos < 4; newLbaPos++, uIntBytePos++)
                                uIntByteSwap[uIntBytePos] = originalIsoFile[newLbaPos / Array.MaxLength][(int)(newLbaPos % Array.MaxLength)];

                            // 0x019 = 25, Bitwise & in C compares two bytes for equality.
                            uint newLBA = BitConverter.ToUInt32(uIntByteSwap); // 0x002 = 2

                            //As the comments above and below state, we have to loop around bytes in the array like this. See the above explanation.
                            for (long newLenPos = sectorSize * (lba + i) + dataOffset + position + 10, uIntBytePos = 0; uIntBytePos < 4; newLenPos++, uIntBytePos++)
                                uIntByteSwap[uIntBytePos] = originalIsoFile[newLenPos / Array.MaxLength][(int)(newLenPos % Array.MaxLength)];

                            uint newLen = BitConverter.ToUInt32(uIntByteSwap); // 0x00A = 10

                            ulong found = Search(fileName, newPath, newLBA, newLen);

                            if (found != 0)
                            {
                                return found;
                            }
                        }

                        // compare names - case insensitive
                        else if (fileName.Equals(normalizedName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            // file found
                            if (!filesForReplacement.ContainsKey(fileName))
                                filesForReplacement.Add(fileName, (ulong)((lba + i) * sectorSize + dataOffset + position));
                            return (ulong)((lba + i) * sectorSize + dataOffset + position);
                        }
                    }
                }
            }

            //if not found return 0
            return 0;
        }
        static void PrepareOldFileForReplacement(string normalizedOldFileName)
        {
            ulong foundPosition = Search(normalizedOldFileName, string.Empty, rootLba, rootLength);
            uint foundLBA = (uint)(foundPosition / sectorSize);
            uint foundOffset = (uint)(foundPosition % sectorSize);


            byte[] oldFileSizeBytes = new byte[4];


            //Again, unfortunately, we have to loop around the array. See my above comment as to why.
            for (long oldFileSizePosStart = sectorSize * foundLBA + foundOffset + 10, i = 0; i < 4; oldFileSizePosStart++, i++)
                oldFileSizeBytes[i] = originalIsoFile[oldFileSizePosStart / Array.MaxLength][(int)(oldFileSizePosStart % Array.MaxLength)];


            byte[] oldFileLbaBytes = new byte[4];
            //Again, unfortunately, we have to loop around the array. See my above comment as to why.
            for (long oldFileLbaPosStart = sectorSize * foundLBA + foundOffset + 2, i = 0; i < 4; oldFileLbaPosStart++, i++)
                oldFileLbaBytes[i] = originalIsoFile[oldFileLbaPosStart / Array.MaxLength][(int)(oldFileLbaPosStart % Array.MaxLength)];

            uint oldFileSize = BitConverter.ToUInt32(oldFileSizeBytes);
            uint oldFileLba = BitConverter.ToUInt32(oldFileLbaBytes);
            oldFileSizes.Add(normalizedOldFileName, oldFileSize);
            oldFileLbas.Add(normalizedOldFileName, oldFileLba);
            oldFileSectorCounts.Add(normalizedOldFileName, (oldFileSize + sectorData - 1) / sectorData);
        }
    }
}
