using System;
using System.IO;
using FpkCodes;

namespace FpkCodes
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				ShowOptions();
			}
			else
			{
				switch (args[0])
				{
					case "-r":
						OptionRepack(args);
						break;
					case "-e":
						OptionExtract(args);
						break;
					case "-f":
						FateLba.FIX(args[1].ToString());
						break;
					default:
						Console.WriteLine("Invalid option.");
						break;
				}
			}
		}

		public static void ShowOptions()
		{
			Console.WriteLine("FpkCodes");
			Console.WriteLine("usage: FpkCodes -e <FPK path> <Folder that will extract>");
			Console.WriteLine("       FpkCodes -r <FPK path> <Folder with FPK files> <Name of new FPK>");
            Console.WriteLine("       FpkCodes -f <iso path>");
        }

		public static void OptionExtract(string[] args)
		{
			if (args.Length != 3)
			{
				Console.WriteLine("Insufficient arguments.");
				return;
			}

			string filePath = args[1];
			string extractedDirectoryPath = args[2];

			if (!File.Exists(filePath))
			{
				Console.WriteLine("File doesn't exist.");
				return;
			}

			if (!Directory.Exists(extractedDirectoryPath))
			{
				Directory.CreateDirectory(extractedDirectoryPath);
			}

			FPKUnpacker.FPKExtract(filePath, extractedDirectoryPath);
		}

		public static void OptionRepack(string[] args)
		{
			if (args.Length != 4)
			{
				Console.WriteLine("Insufficient arguments.");
				return;
			}
			string filePath = args[1];
			string extractedDirectoryPath = args[2];

			if (!File.Exists(filePath))
			{
				Console.WriteLine("File doesn't exist.");
				return;
			}
			if (!Directory.Exists(extractedDirectoryPath))
			{
				Console.WriteLine("Folder does not exist.");
				return;
			}
		
			FPKPacker.FPKRepack(filePath, extractedDirectoryPath, args[3]);
		}
	}
}
