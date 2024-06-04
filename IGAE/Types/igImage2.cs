using System;
using System.Linq;
using System.IO;
using STTUMM.IGAE;

using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using SixLabors.ImageSharp;
using STTUMM.IGAE;

namespace STTUMM.IGAE.Types
{
    public class igImage2 : igObject
	{
		ushort width;
		ushort height;
		ushort depth;
		ushort mipmapCount;
		ushort array;
		IGZ_TextureFormat format;
		uint index;
		uint textureOffset;
		uint textureSize;
        bool mode;

        public igImage2(igObject basic)
		{
			_container = basic._container;
			offset     = basic.offset;
			name       = basic.name;
			itemCount  = basic.itemCount;
			length     = basic.length;
			data       = basic.data;
			fields     = basic.fields;
			children   = basic.children;
		}

		public override void ReadObjectFields()
		{
			if(_container.version == 0x05)
			{
				_container.ebr.BaseStream.Seek(offset + 0x0C, SeekOrigin.Begin);
			}
			else if(_container.version == 0x09)
			{
				_container.ebr.BaseStream.Seek(offset + 0x34, SeekOrigin.Begin);
			}
			else
			{
				_container.ebr.BaseStream.Seek(offset + 0x30, SeekOrigin.Begin);
			}
			width       = _container.ebr.ReadUInt16();
			height      = _container.ebr.ReadUInt16();
			depth       = _container.ebr.ReadUInt16();
			mipmapCount = _container.ebr.ReadUInt16();
			array       = _container.ebr.ReadUInt16();

			_container.ebr.BaseStream.Seek(0x04, SeekOrigin.Current);

			IGZ_EXID exid = _container.fixups.First(x => x.magicNumber == 0x45584944) as IGZ_EXID;
			format = (IGZ_TextureFormat)exid.hashes[_container.ebr.ReadUInt16()];

			_container.ebr.BaseStream.Seek(0x08, SeekOrigin.Current);
			IGZ_TMHN tmhn = _container.fixups.First(x => x.magicNumber == 0x544D484E) as IGZ_TMHN;
			index = _container.ebr.ReadUInt32();
			textureOffset = tmhn.offsets[index];
			textureSize   = tmhn.sizes[index];

			Console.WriteLine($"Image Found: {width}, {height}, {mipmapCount}, {index}, {format.ToString()}");
		}
		public void Extract(Stream output)
		{
			Console.WriteLine(textureOffset.ToString("X08"));
			_container.ebr.BaseStream.Seek(textureOffset, SeekOrigin.Begin);
			mode = TextureHelper.Extract(_container.ebr.BaseStream, output, width, height, textureSize, mipmapCount, format, true);
		}
		public void Replace(Stream input)
		{
			_container.ebr.BaseStream.Seek(textureOffset, SeekOrigin.Begin);
			TextureHelper.Replace(input, _container.ebr.BaseStream, width, height, textureSize, mipmapCount, format, mode);
			input.Close();
        }

        public void ExtractDDS(Stream output)
        {
            Console.WriteLine(textureOffset.ToString("X08"));
            _container.ebr.BaseStream.Seek(textureOffset, SeekOrigin.Begin);
            TextureHelper.ExtractDDS(_container.ebr.BaseStream, output, width, height, textureSize, mipmapCount, format, true);
        }

        public void ReplaceDDS(Stream input)
        {
            _container.ebr.BaseStream.Seek(textureOffset, SeekOrigin.Begin);
            TextureHelper.ReplaceDDS(input, _container.ebr.BaseStream, width, textureSize, format);
            input.Close();
        }
    }
}