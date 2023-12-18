using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using FpkCodes;


namespace FpkCodes;

public static class Program
{
	public static void Main()
	{
	    //FPKUnpacker.FPKExtract("0000.fpk","0000_backup");
		FPKPacker.FPKRepack("0000.fpk", "0000", "0000_repack.fpk");
		
	}
}
