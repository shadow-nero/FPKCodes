# FPKCodes

This tool was developed to work with and assist in modding FPK files from the Fate Unlimited Code game. Some games from [Eighting](https://en.wikipedia.org/wiki/Eighting) also function, but only if they are Little Endian (PSP/PS2). I strongly recommend using [GNTOOLS](https://github.com/NicholasMoser/GNTool/tree/main) if you want to work with other Eighting games.

## Table of Contents

- [Usage](#usage)
- [How It Works](#how-it-works)
- [Integrity Check](#integrity-check)
- [Creating an ISO](#creating-an-iso)
- [Special Thanks](#special-thanks)

## Usage

To run the tool, download the latest version from [FPKCodes releases](https://github.com/shadow-nero/FPKCodes/releases). Then execute the tool through the command line following the instructions below.

### Extract FPK and Repackage FPK

Commands are quite simple for extraction:

       FpkCodes.exe -e <FPK path> <Folder path>
       
The first parameter will be the FPK path you want to extract, and the second is the path of the folder where files will be extracted. Ensure the folder exists, as the tool will create it if it doesn't.

Repacking the FPK can be a bit more complicated to understand initially.

       FpkCodes.exe -r <FPK path> <Folder path> <New FPK path>
       
First, provide the original FPK file, as it allows us to determine which files were modified. Then specify the folder containing the FPK files, followed by the name of the new FPK file.

## How It Works

FPK files contain several compressed files using Eighting's PRS algorithm. For more information, refer to [FPK files](https://github.com/NicholasMoser/Naruto-GNT-Hacking/blob/master/gnt4/docs/file_formats/fpk.md). There's not much to say about extraction, so let's focus on FPK recompilation. Similar to GNTools, the compression we use is not identical to Eighting's. To avoid inflated file sizes, we only compress files that were modified, using the original CRC32 as a comparison.

## Integrity Check

Unlike other Eighting games, Fate Unlimited Codes has an integrity check in the first 4 bytes of FPK files to prevent modifications. Fortunately, we managed to patch the game's EBOOT to circumvent this issue. However, we haven't fully deciphered this integrity check, so we cannot replicate it.

## Creating an ISO

After modifying the FPK files, it's necessary to rebuild the ISO, placing each file in its proper position, also known as LBA (Logical Block Addressing). To do this:

       Open the ISO in UmdGen
       Go to File -> File List -> Export
       Make the necessary modifications in the ISO; when finished, do the following
       Go to File -> File List -> Import -> Save the ISO
       
**If the new file exceeds the size of the original FPK file, the game will crash due to "size verification" of the LBA files. Currently, there's no publicly available tool that allows larger files while maintaining the client's integrity.**

## Special Thanks

- **tpu** - Wrote the original PRS uncompression algorithm.
- **RupertAvery** - Wrote the original PRS compression algorithm.
- **[Luigi Auriemma](https://aluigi.altervista.org/quickbms.htm)** - Ported PRS compression/uncompression algorithms to QuickBMS.
- **[Nicholas Moser](https://github.com/NicholasMoser)** - Creator of GNTools; without him, this tool would not exist.

