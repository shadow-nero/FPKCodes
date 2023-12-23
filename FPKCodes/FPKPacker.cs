using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace FpkCodes;

public class FPKPacker 
{
	public static void FPKRepack(string FPKFile, string Folder, string FileDest)
	{
		
		using (FileStream FPKStream = new FileStream(FPKFile, FileMode.Open, FileAccess.ReadWrite))
		{
			using (BinaryReader FPKBinary = new BinaryReader(FPKStream))
			{
                uint integridade = FPKBinary.ReadUInt32();
                uint AmountOfFiles = FPKBinary.ReadUInt32();
                uint Padding = FPKBinary.ReadUInt32();
                uint FileLength = FPKBinary.ReadUInt32();

                List<DirectoryItem> directoryItemList = new List<DirectoryItem>();


                for (int i = 0; i < AmountOfFiles; i++)
                {
                    byte[] bytesNames = new byte[36];
                    FPKStream.Read(bytesNames, 0, bytesNames.Length);
                    string Name = Encoding.ASCII.GetString(bytesNames).Replace("\0", "");
                    uint offset = FPKBinary.ReadUInt32();
                    uint compressedSize = FPKBinary.ReadUInt32();
                    uint uncompressedSize = FPKBinary.ReadUInt32();

                    directoryItemList.Add(new DirectoryItem
                    {
                        Nome = Name,
                        Offset = offset,
                        CompressedSize = compressedSize,
                        UncompressedSize = uncompressedSize
                    });
                }

                foreach (var item in directoryItemList)
                {

                    FPKStream.Seek(item.Offset, SeekOrigin.Begin);

                    byte[] ByteCompressed = new byte[item.CompressedSize];
                    FPKStream.Read(ByteCompressed, 0, ByteCompressed.Length);

                    PRSUncompressor uncompressor = new PRSUncompressor(ByteCompressed, (int)item.UncompressedSize);

                    byte[] uncompressedData = uncompressor.Uncompress();
                    uint crc32 = CRC32.CalCRC32(uncompressedData);

                    //Console.WriteLine(crc32);
                    item.DataComp = ByteCompressed;
                    item.crc32comp = crc32;
                }



                using (FileStream NEWFPK = new FileStream(FileDest, FileMode.Create, FileAccess.ReadWrite))
                {
                    using (BinaryWriter FPKBinaryWriter = new BinaryWriter(NEWFPK))
                    {

                        int FilesQuant = directoryItemList.Count;

                        FPKBinaryWriter.Write((uint)0x00);
                        FPKBinaryWriter.Write((uint)directoryItemList.Count);
                        FPKBinaryWriter.Write((uint)0x10);
                        FPKBinaryWriter.Write((uint)0x00);

                        for (int i = 0; i < FilesQuant * 12; i++)
                            FPKBinaryWriter.Write((uint)0x00);
                        //start position to calulate integridade
                        long startPos = NEWFPK.Position;
                        foreach (var item in directoryItemList)
                        {
                            byte Padding16 = 0x10;
                            uint crc32folder = 0;

                            using (FileStream TempFile = new FileStream(Path.Combine(Folder, item.Nome), FileMode.Open, FileAccess.Read))
                            {
                                byte[] BodyFile = new byte[TempFile.Length];
                                TempFile.Read(BodyFile, 0, BodyFile.Length);
                                crc32folder = CRC32.CalCRC32(BodyFile);


                                Console.WriteLine(crc32folder);
                                Console.WriteLine(item.crc32comp);
                                item.Offset = (uint)NEWFPK.Position;

                                if (item.crc32comp == crc32folder)
                                {

                                    FPKBinaryWriter.Write(item.DataComp, 0, item.DataComp.Length);

                                }
                                else
                                {
                                    Console.WriteLine("Compressing File...");
                                    PRSCompressor compressor = new PRSCompressor(BodyFile);
                                    byte[] compressedData = compressor.Compress();

                                    FPKBinaryWriter.Write(compressedData, 0, compressedData.Length);

                                    item.CompressedSize = (uint)compressedData.Length;
                                    item.UncompressedSize = (uint)BodyFile.Length;

                                }
                            }
                            Console.WriteLine($"Nome: {item.Nome}, Offset: {item.Offset}, Compressed Size: {item.CompressedSize}, Uncompressed Size: {item.UncompressedSize}, CRC32 Orinal: {item.crc32comp}, CRC32 New: {crc32folder}");
                            Console.WriteLine("----------------------------------");

                            if (NEWFPK.Position % Padding16 != 0)
                                while (NEWFPK.Position % Padding16 != 0)
                                    FPKBinaryWriter.Write((byte)0x0);

                        }
                        //end position to calulate integridade
                        long endPos = NEWFPK.Position;
                        //lets calculate new integridade
                        {
                            uint newIntegridade = 0;
                            NEWFPK.Seek(startPos, SeekOrigin.Begin);
                            while (NEWFPK.Position < endPos)
                            {
                                newIntegridade += (byte)NEWFPK.ReadByte();

                            }
                            newIntegridade &= 0xffff;
                            //write new integridade
                            NEWFPK.Seek((long)0, SeekOrigin.Begin);
                            FPKBinaryWriter.Write((uint)newIntegridade);
                        }
                       
                        NEWFPK.Seek((long)0xc, SeekOrigin.Begin);
                        long tamanhoDoArquivo = NEWFPK.Length;
                        FPKBinaryWriter.Write((uint)tamanhoDoArquivo);

                        foreach (var item in directoryItemList)
                        {

                            string namePadding = item.Nome;
                            namePadding = namePadding.PadRight(36, '\0');
                            byte[] bytesNamePadding = Encoding.ASCII.GetBytes(namePadding);
                            FPKBinaryWriter.Write(bytesNamePadding, 0, bytesNamePadding.Length);
                            FPKBinaryWriter.Write((uint)item.Offset);
                            FPKBinaryWriter.Write((uint)item.CompressedSize);
                            FPKBinaryWriter.Write((uint)item.UncompressedSize);
                        }

                    }
                }
                Console.WriteLine("Repacked successfully!!!");

            }
            //PRSUncompressor compressor = new PRSUncompressor(input, outputLength);
        }
	}

}