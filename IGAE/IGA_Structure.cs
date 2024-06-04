﻿using STTUMM.IGAE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STTUMM.IGAE
{
    static class IGA_Structure
    {
        public static Dictionary<IGA_Version, uint[]> headerData = new Dictionary<IGA_Version, uint[]>()
        {
            {
                IGA_Version.SkylandersSpyrosAdventureWii,
                new uint[]
                {
                    0x00000018,		//Unknown but important address
					0x0000000C,		//Number of Files
					0x00000018,		//Nametable Location
					0x0000001C,		//Nametable Size
					0x0000000C,		//Length of indiviual local file header
					0x00000030,		//Checksum starting location
					0x00000004,		//Size of individual checksum
					0x00000000,		//Position of a local file's starting location inside of a local header
					0x00000004,		//Position of a local file's size inside of a local header
					0x00000008,		//The compression mode of the file
					0x00000030,		//The bit flags for this archive
				}
            },
            {
                IGA_Version.SkylandersSpyrosAdventureWiiU,
                new uint[]
                {
                    0x00000018,		//Unknown but important address
					0x0000000C,		//Number of Files
					0x0000001C,		//Nametable Location
					0x00000020,		//Nametable Size
					0x0000000C,		//Length of indiviual local file header
					0x00000034,		//Checksum starting location
					0x00000004,		//Size of individual checksum
					0x00000000,		//Position of a local file's starting location inside of a local header
					0x00000004,		//Position of a local file's size inside of a local header
					0x00000008,		//The compression mode of the file
					0x00000030,		//The bit flags for this archive
				}
            },
            {
                IGA_Version.SkylandersSwapForce,
                new uint[]
                {
                    0x00000018,		//Unknown but important address
					0x0000000C,		//Number of Files
					0x0000002C,		//Nametable Location
					0x00000030,		//Nametable Size
					0x00000010,		//Length of indiviual local file header
					0x00000038,		//Checksum starting location
					0x00000004,		//Size of individual checksum
					0x00000004,		//Position of a local file's starting location inside of a local header
					0x00000008,		//Position of a local file's size inside of a local header
					0x0000000C,		//Position of a local file's compression mode inside of a local header
					0x00000034,		//The bit flags for this archive
				}
            },
            {
                IGA_Version.SkylandersLostIslands,
                new uint[]
                {
                    0x00000018,		//Unknown but important address
					0x0000000C,		//Number of Files
					0x00000028,		//Nametable Location
					0x00000030,		//Nametable Size
					0x00000010,		//Length of indiviual local file header
					0x00000038,		//Checksum starting location
					0x00000004,		//Size of individual checksum
					0x00000000,		//Position of a local file's starting location inside of a local header
					0x00000008,		//Position of a local file's size inside of a local header
					0x0000000C,		//Position of a local file's compression mode inside of a local header
					0x00000034,		//The bit flags for this archive
				}
            },
            {
                IGA_Version.SkylandersTrapTeam,
                new uint[]
                {
                    0x00000018,		//Unknown
					0x0000000C,		//Number of Files
					0x00000028,		//Nametable Location
					0x00000030,		//Nametable Size
					0x00000010,		//Length of individual local file header
					0x00000038,		//Checksum starting location
					0x00000004,		//Size of individual checksum
					0x00000000,		//Position of a local file's starting location inside of a local header
					0x00000008,		//Position of a local file's size inside of a local header
					0x0000000C,		//The compression mode of the file
					0x00000034,		//The bit flags for this archive
				}
            },
            {
                IGA_Version.SkylandersSuperChargers,
                new uint[]
                {
                    0x00000018,		//Unknown
					0x0000000C,		//Number of Files
					0x0000002C,		//Nametable Location
					0x00000030,		//Nametable Size
					0x00000010,		//Length of individual local file header
					0x00000038,		//Checksum starting location
					0x00000004,		//Size of individual checksum
					0x00000004,		//Position of a local file's starting location inside of a local header
					0x00000008,		//Position of a local file's size inside of a local header
					0x0000000C,		//The compression mode of the file
					0x00000034,		//The bit flags for this archive
				}
            },
            {
                IGA_Version.SkylandersImaginatorsPS4,
                new uint[]
                {
                    0x00000018,		//Unknown
					0x0000000C,		//Number of Files
					0x00000028,		//Nametable Location
					0x00000030,		//Nametable Size
					0x00000010,		//Length of individual local file header
					0x00000038,		//Checksum starting location
					0x00000004,		//Size of individual checksum
					0x00000000,		//Position of a local file's starting location inside of a local header
					0x00000008,		//Position of a local file's size inside of a local header
					0x0000000C,		//The compression mode of the file
					0x00000034,		//The bit flags for this archive
				}
            },
            {
                IGA_Version.CrashNST,
                new uint[]
                {
                    0x00000018,		//Unknown
					0x0000000C,		//Number of Files
					0x00000024,		//Nametable Location
					0x0000002C,		//Nametable Size, unconfirmed
					0x00000010,		//Length of individual local file header
					0x00000038,		//Checksum starting location
					0x00000004,		//Size of individual checksum
					0x00000000,		//Position of a local file's starting location inside of a local header
					0x00000008,		//Position of a local file's size inside of a local header
					0x0000000C,		//The compression mode of the file
					0x00000034,		//The bit flags for this archive
				}
            },
        };
    }
    enum IGA_HeaderData
    {
        Unknown1 = 0000,
        NumberOfFiles = 0001,
        NametableLocation = 0002,
        NametableLength = 0003,
        LocalHeaderLength = 0004,
        ChecksumLocation = 0005,
        ChecksumLength = 0006,
        FileStartInLocal = 0007,
        FileLengthInLocal = 0008,
        ModeInLocal = 0009,
        Flags = 0010
    }
}
