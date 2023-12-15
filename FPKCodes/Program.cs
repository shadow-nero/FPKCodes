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
	    FPKUnpacker.FPKExtract("0001.fpk","0001");
	}
}
