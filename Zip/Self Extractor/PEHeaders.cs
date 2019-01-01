#region

using System.IO;

#endregion

namespace HC.Core.Zip.Self_Extractor
{
    public class PEHeaders
    {
        //	#define IMAGE_NUMBEROF_DIRECTORY_ENTRIES    16
        private const uint IMAGE_NUMBEROF_DIRECTORY_ENTRIES = 16;
        //#define IMAGE_SIZEOF_SHORT_NAME              8
        private const uint IMAGE_SIZEOF_SHORT_NAME = 8;

        private PEHeaders()
        {
            // private constructor, no code required.
        }

        #region Nested type: _IMAGE_DATA_DIRECTORY

        public class _IMAGE_DATA_DIRECTORY
        {
            public uint Size;
            public uint VirtualAddress;

            public _IMAGE_DATA_DIRECTORY()
            {
                VirtualAddress = 0;
                Size = 0;
            }

            public int Length
            {
                get { return 8; }
            }

            public void FillStructure(BinaryReader bin)
            {
                VirtualAddress = bin.ReadUInt32();
                Size = bin.ReadUInt32();
            }
        }

        #endregion

        /*
		typedef struct _IMAGE_DOS_HEADER 
				{      // DOS .EXE header
					WORD   e_magic;                     // Magic number
					WORD   e_cblp;                      // Bytes on last page of file
					WORD   e_cp;                        // Pages in file
					WORD   e_crlc;                      // Relocations
					WORD   e_cparhdr;                   // Size of header in paragraphs
					WORD   e_minalloc;                  // Minimum extra paragraphs needed
					WORD   e_maxalloc;                  // Maximum extra paragraphs needed
					WORD   e_ss;                        // Initial (relative) SS value
					WORD   e_sp;                        // Initial SP value
					WORD   e_csum;                      // Checksum
					WORD   e_ip;                        // Initial IP value
					WORD   e_cs;                        // Initial (relative) CS value
					WORD   e_lfarlc;                    // File address of relocation table
					WORD   e_ovno;                      // Overlay number
					WORD   e_res[4];                    // Reserved words
					WORD   e_oemid;                     // OEM identifier (for e_oeminfo)
					WORD   e_oeminfo;                   // OEM information; e_oemid specific
					WORD   e_res2[10];                  // Reserved words
					LONG   e_lfanew;                    // File address of new exe header
				} IMAGE_DOS_HEADER, *PIMAGE_DOS_HEADER;
		*/

        #region Nested type: _IMAGE_DOS_HEADER

        public class _IMAGE_DOS_HEADER
        {
            // DOS .EXE header
            public ushort e_cblp; // Bytes on last page of file
            public ushort e_cp; // Pages in file
            public ushort e_cparhdr; // Size of header in paragraphs
            public ushort e_crlc; // Relocations
            public ushort e_cs; // Initial (relative) CS value
            public ushort e_csum; // Checksum
            public ushort e_ip; // Initial IP value
            public int e_lfanew; // File address of new exe header
            public ushort e_lfarlc; // File address of relocation table
            public ushort e_magic; // Magic number
            public ushort e_maxalloc; // Maximum extra paragraphs needed
            public ushort e_minalloc; // Minimum extra paragraphs needed
            public ushort e_oemid; // OEM identifier (for e_oeminfo)
            public ushort e_oeminfo; // OEM information; e_oemid specific
            public ushort e_ovno; // Overlay number
            public ushort[] e_res; // Reserved words
            public ushort[] e_res2; // Reserved words
            public ushort e_sp; // Initial SP value
            public ushort e_ss; // Initial (relative) SS value

            public _IMAGE_DOS_HEADER()
            {
                e_res = new ushort[4];
                e_res2 = new ushort[10];
            }

            public int Length
            {
                get { return 64; }
            }

            public void FillStructure(BinaryReader bin)
            {
                e_magic = bin.ReadUInt16(); // Magic number
                e_cblp = bin.ReadUInt16(); // Bytes on last page of file
                e_cp = bin.ReadUInt16(); // Pages in file
                e_crlc = bin.ReadUInt16(); // Relocations
                e_cparhdr = bin.ReadUInt16(); // Size of header in paragraphs
                e_minalloc = bin.ReadUInt16(); // Minimum extra paragraphs needed
                e_maxalloc = bin.ReadUInt16(); // Maximum extra paragraphs needed
                e_ss = bin.ReadUInt16(); // Initial (relative) SS value
                e_sp = bin.ReadUInt16(); // Initial SP value
                e_csum = bin.ReadUInt16(); // Checksum
                e_ip = bin.ReadUInt16(); // Initial IP value
                e_cs = bin.ReadUInt16(); // Initial (relative) CS value
                e_lfarlc = bin.ReadUInt16(); // File address of relocation table
                e_ovno = bin.ReadUInt16();
                for (int x = 0; x <= 3; x++) // Overlay number
                {
                    e_res[x] = bin.ReadUInt16(); // Reserved words
                }
                e_oemid = bin.ReadUInt16(); // OEM identifier (for e_oeminfo)
                e_oeminfo = bin.ReadUInt16(); // OEM information; e_oemid specific
                for (int x = 0; x <= 9; x++)
                {
                    e_res2[x] = bin.ReadUInt16(); // Reserved words
                }
                e_lfanew = bin.ReadInt32();
            }
        }

        #endregion

        /*
			typedef struct _IMAGE_NT_HEADERS 
				{
					DWORD Signature;
					IMAGE_FILE_HEADER FileHeader;
					IMAGE_OPTIONAL_HEADER32 OptionalHeader;
				} IMAGE_NT_HEADERS32, *PIMAGE_NT_HEADERS32;
		*/


        /* 
			typedef struct _IMAGE_FILE_HEADER 
				{
					WORD    Machine;
					WORD    NumberOfSections;
					DWORD   TimeDateStamp;
					DWORD   PointerToSymbolTable;
					DWORD   NumberOfSymbols;
					WORD    SizeOfOptionalHeader;
					WORD    Characteristics;
				} IMAGE_FILE_HEADER, *PIMAGE_FILE_HEADER;
		*/

        //[StructLayout(LayoutKind.Sequential)]

        #region Nested type: _IMAGE_FILE_HEADER

        public class _IMAGE_FILE_HEADER
        {
            public ushort Characteristics;
            public ushort Machine;
            public ushort NumberOfSections;
            public uint NumberOfSymbols;
            public uint PointerToSymbolTable;
            public ushort SizeOfOptionalHeader;
            public uint TimeDateStamp;

            public int Length
            {
                get { return 20; }
            }

            public void FillStructure(BinaryReader bin)
            {
                Machine = bin.ReadUInt16();
                NumberOfSections = bin.ReadUInt16();
                TimeDateStamp = bin.ReadUInt32();
                PointerToSymbolTable = bin.ReadUInt32();
                NumberOfSymbols = bin.ReadUInt32();
                SizeOfOptionalHeader = bin.ReadUInt16();
                Characteristics = bin.ReadUInt16();
            }
        }

        #endregion

        #region Nested type: _IMAGE_NT_HEADERS

        public class _IMAGE_NT_HEADERS
        {
            public _IMAGE_FILE_HEADER fileHeader;
            public _IMAGE_OPTIONAL_HEADER optionalHeader;
            public uint Signature;

            public _IMAGE_NT_HEADERS()
            {
                fileHeader = new _IMAGE_FILE_HEADER();
                optionalHeader = new _IMAGE_OPTIONAL_HEADER();
            }

            public void FillStructure(BinaryReader bin)
            {
                Signature = bin.ReadUInt32();
                fileHeader.FillStructure(bin);
                optionalHeader.FillStructure(bin);
            }
        }

        #endregion

        /*
		typedef struct _IMAGE_DATA_DIRECTORY {
			DWORD   VirtualAddress;
			DWORD   Size;
		} IMAGE_DATA_DIRECTORY, *PIMAGE_DATA_DIRECTORY;
		*/

        //[StructLayout(LayoutKind.Sequential)]


        /*
		typedef struct _IMAGE_OPTIONAL_HEADER 
				{
					WORD    Magic;
					BYTE    MajorLinkerVersion;
					BYTE    MinorLinkerVersion;
					DWORD   SizeOfCode;
					DWORD   SizeOfInitializedData;
					DWORD   SizeOfUninitializedData;
					DWORD   AddressOfEntryPoint;
					DWORD   BaseOfCode;
					DWORD   BaseOfData;
					DWORD   ImageBase;
					DWORD   SectionAlignment;
					DWORD   FileAlignment;
					WORD    MajorOperatingSystemVersion;
					WORD    MinorOperatingSystemVersion;
					WORD    MajorImageVersion;
					WORD    MinorImageVersion;
					WORD    MajorSubsystemVersion;
					WORD    MinorSubsystemVersion;
					DWORD   Win32VersionValue;
					DWORD   SizeOfImage;
					DWORD   SizeOfHeaders;
					DWORD   CheckSum;
					WORD    Subsystem;
					WORD    DllCharacteristics;
					DWORD   SizeOfStackReserve;
					DWORD   SizeOfStackCommit;
					DWORD   SizeOfHeapReserve;
					DWORD   SizeOfHeapCommit;
					DWORD   LoaderFlags;
					DWORD   NumberOfRvaAndSizes;
					IMAGE_DATA_DIRECTORY DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
				} IMAGE_OPTIONAL_HEADER, *PIMAGE_OPTIONAL_HEADER;
		*/

        //[StructLayout(LayoutKind.Sequential)]

        #region Nested type: _IMAGE_OPTIONAL_HEADER

        public class _IMAGE_OPTIONAL_HEADER
        {
            public uint AddressOfEntryPoint;
            public uint BaseOfCode;
            public uint BaseOfData;
            public uint CheckSum;
            public _IMAGE_DATA_DIRECTORY[] DataDirectory;
            public ushort DllCharacteristics;
            public uint FileAlignment;
            public uint ImageBase;
            public uint LoaderFlags;
            public ushort Magic;
            public ushort MajorImageVersion;
            public byte MajorLinkerVersion;
            public ushort MajorOperatingSystemVersion;
            public ushort MajorSubsystemVersion;
            public ushort MinorImageVersion;
            public byte MinorLinkerVersion;
            public ushort MinorOperatingSystemVersion;
            public ushort MinorSubsystemVersion;
            public uint NumberOfRvaAndSizes;
            public uint SectionAlignment;
            public uint SizeOfCode;
            public uint SizeOfHeaders;
            public uint SizeOfHeapCommit;
            public uint SizeOfHeapReserve;
            public uint SizeOfImage;
            public uint SizeOfInitializedData;
            public uint SizeOfStackCommit;
            public uint SizeOfStackReserve;
            public uint SizeOfUninitializedData;
            public ushort Subsystem;
            public uint Win32VersionValue;

            public _IMAGE_OPTIONAL_HEADER()
            {
                DataDirectory = new _IMAGE_DATA_DIRECTORY[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
                for (int x = 0; x < IMAGE_NUMBEROF_DIRECTORY_ENTRIES; x += 1)
                {
                    DataDirectory[x] = new _IMAGE_DATA_DIRECTORY();
                }
            }

            public int Length
            {
                get { return 1; }
            }

            public void FillStructure(BinaryReader bin)
            {
                Magic = bin.ReadUInt16();
                MajorLinkerVersion = bin.ReadByte();
                MinorLinkerVersion = bin.ReadByte();
                SizeOfCode = bin.ReadUInt32();
                SizeOfInitializedData = bin.ReadUInt32();
                SizeOfUninitializedData = bin.ReadUInt32();
                AddressOfEntryPoint = bin.ReadUInt32();
                BaseOfCode = bin.ReadUInt32();
                BaseOfData = bin.ReadUInt32();
                ImageBase = bin.ReadUInt32();
                SectionAlignment = bin.ReadUInt32();
                FileAlignment = bin.ReadUInt32();
                MajorOperatingSystemVersion = bin.ReadUInt16();
                MinorOperatingSystemVersion = bin.ReadUInt16();
                MajorImageVersion = bin.ReadUInt16();
                MinorImageVersion = bin.ReadUInt16();
                MajorSubsystemVersion = bin.ReadUInt16();
                MinorSubsystemVersion = bin.ReadUInt16();
                Win32VersionValue = bin.ReadUInt32();
                SizeOfImage = bin.ReadUInt32();
                SizeOfHeaders = bin.ReadUInt32();
                CheckSum = bin.ReadUInt32();
                Subsystem = bin.ReadUInt16();
                DllCharacteristics = bin.ReadUInt16();
                SizeOfStackReserve = bin.ReadUInt32();
                SizeOfStackCommit = bin.ReadUInt32();
                SizeOfHeapReserve = bin.ReadUInt32();
                SizeOfHeapCommit = bin.ReadUInt32();
                LoaderFlags = bin.ReadUInt32();
                NumberOfRvaAndSizes = bin.ReadUInt32();
                for (int x = 0; x < IMAGE_NUMBEROF_DIRECTORY_ENTRIES; x++)
                {
                    DataDirectory[x].FillStructure(bin);
                }
            }
        }

        #endregion

        /*
			typedef struct _IMAGE_SECTION_HEADER {
				BYTE    Name[IMAGE_SIZEOF_SHORT_NAME];
				union {
						DWORD   PhysicalAddress;
						DWORD   VirtualSize;
				} Misc;
				DWORD   VirtualAddress;
				DWORD   SizeOfRawData;
				DWORD   PointerToRawData;
				DWORD   PointerToRelocations;
				DWORD   PointerToLinenumbers;
				WORD    NumberOfRelocations;
				WORD    NumberOfLinenumbers;
				DWORD   Characteristics;
				} IMAGE_SECTION_HEADER, *PIMAGE_SECTION_HEADER;
				*/

        #region Nested type: _IMAGE_SECTION_HEADER

        public class _IMAGE_SECTION_HEADER
        {
            public uint Characteristics;
            public byte[] Name;
            public ushort NumberOfLinenumbers;
            public ushort NumberOfRelocations;

            public uint PhysicalAddressOrVirtualSize;
            public uint PointerToLinenumbers;
            public uint PointerToRawData;
            public uint PointerToRelocations;
            public uint SizeOfRawData;
            public uint VirtualAddress;

            public _IMAGE_SECTION_HEADER()
            {
                Name = new byte[IMAGE_SIZEOF_SHORT_NAME];
                for (int x = 0; x < IMAGE_SIZEOF_SHORT_NAME; x++)
                {
                    Name[x] = new byte();
                }
            }

            public int Length
            {
                get { return 28; }
            }

            public void FillStructure(BinaryReader bin)
            {
                for (int x = 0; x < IMAGE_SIZEOF_SHORT_NAME; x++)
                {
                    Name[x] = bin.ReadByte();
                }
                PhysicalAddressOrVirtualSize = bin.ReadUInt32();
                VirtualAddress = bin.ReadUInt32();
                SizeOfRawData = bin.ReadUInt32();
                PointerToRawData = bin.ReadUInt32();
                PointerToRelocations = bin.ReadUInt32();
                PointerToLinenumbers = bin.ReadUInt32();
                NumberOfRelocations = bin.ReadUInt16();
                NumberOfLinenumbers = bin.ReadUInt16();
                Characteristics = bin.ReadUInt32();
            }
        }

        #endregion
    }
}

