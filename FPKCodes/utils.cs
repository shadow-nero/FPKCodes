using System;
using System.Linq;
using System.Collections.Generic;

namespace FpkCodes;

public class DirectoryItem
{
    public string Nome { get; set; }
    public uint Offset { get; set; }
    public uint CompressedSize { get; set; }
    public uint UncompressedSize { get; set; }
	public byte[] DataComp { get; set; }
	public uint crc32comp { get; set; }
}
