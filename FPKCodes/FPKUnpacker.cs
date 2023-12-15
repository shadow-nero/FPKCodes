using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace FpkCodes;

public class DirectoryItem
{
    public string Nome { get; set; }
    public uint Offset { get; set; }
    public uint CompressedSize { get; set; }
    public uint UncompressedSize { get; set; }
}

public class FPKUnpacker 
{         
        public static void FPKExtract(string FPKFile, string Dest) {
		
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
							
							directoryItemList.Add(new DirectoryItem{
							    Nome = Name,
								Offset = offset,
								CompressedSize = compressedSize,
								UncompressedSize = uncompressedSize});
						}			
						
					foreach (var item in directoryItemList) {
					    string[] directories = item.Nome.Split('/');
						
						if (directories.Length > 1)
                    {
                        string extractedDirectoryPath = Dest;

                        for (int i = 0; i < directories.Length - 1; i++)
                        {
                            extractedDirectoryPath = Path.Combine(extractedDirectoryPath, directories[i]);

                            if (!Directory.Exists(extractedDirectoryPath))
                            {
                                Directory.CreateDirectory(extractedDirectoryPath);
                            }
                        }
                    }
					    FPKStream.Seek(item.Offset , SeekOrigin.Begin);
						
						byte[] ByteCompressed = new byte[item.CompressedSize];
                        FPKStream.Read(ByteCompressed, 0, ByteCompressed.Length);
							
					    PRSUncompressor compressor = new PRSUncompressor(ByteCompressed, (int)item.UncompressedSize);
					    Console.WriteLine($"Nome: {item.Nome}, Offset: {item.Offset}, Compressed Size: {item.CompressedSize}, Uncompressed Size: {item.UncompressedSize}");
						Console.WriteLine("----------------------------------");
						byte[] uncompressedData = compressor.Uncompress();
						
						using (FileStream Extract = new FileStream(Path.Combine(Dest, item.Nome), FileMode.Create, FileAccess.Write))
                            Extract.Write(uncompressedData, 0, uncompressedData.Length); 
						}
						Console.WriteLine("Extraido com Sucesso!");
		
	    //PRSUncompressor compressor = new PRSUncompressor(input, outputLength);
        }
		}
		
		}
}
