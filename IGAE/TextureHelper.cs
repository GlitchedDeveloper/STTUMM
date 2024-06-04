using System.IO;
using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using Microsoft.Toolkit.HighPerformance;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using STTUMM.IGAE.GX2Utils;

namespace STTUMM.IGAE {
    public static class TextureHelper {
        private static readonly byte[] DdsHeader = {
            0x44, 0x44, 0x53, 0x20, // "DDS "
            0x7C, 0x00, 0x00, 0x00, // Version Info
            0x07, 0x10, 0x0A, 0x00, // More Version Info
            0x00, 0x00, 0x00, 0x00, // Width
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // Size
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // Mipmaps 
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x20, 0x00, 0x00, 0x00, // 
            0x04, 0x00, 0x00, 0x00, // 
            0x44, 0x58, 0x54, 0x30, // "DXT0"
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x08, 0x10, 0x40, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00, // 
            0x00, 0x00, 0x00, 0x00 // 
        };
        public static byte[] DeswizzleWiiU(byte[] fileData, uint width, uint height, uint size, uint mipmapCount, IGZ_TextureFormat format, ref bool mode)
        {
            GX2.GX2Surface surf = GX2.CreateGx2Texture(fileData, "", 0, 0, width, height, 1, (uint)(SimplifyTextureFormat(format) == IGZ_TextureFormat.dxt1 ? GX2.GX2SurfaceFormat.T_BC1_UNORM : GX2.GX2SurfaceFormat.T_BC5_UNORM), 0, (uint)GX2.GX2SurfaceDimension.DIM_2D, mipmapCount);
            surf.tileMode = GX2.getDefaultGX2TileMode((uint)GX2.GX2SurfaceDimension.DIM_2D, width, height, 1, (uint)format, 0, 1);
            var surfout = GX2.getSurfaceInfo((GX2.GX2SurfaceFormat)surf.format, surf.width, surf.height, surf.depth, (uint)surf.dim, (uint)surf.tileMode, (uint)surf.aa, 0);
            if (surfout.surfSize > fileData.Length || (surf.mipSize > 0 && surfout.surfSize + surf.mipSize != fileData.Length))
            {
                surf.tileMode = (uint)GX2.GX2TileMode.MODE_1D_TILED_THIN1;
                surf.data = fileData;
                mode = true;
            }
            surf.data = fileData;
            Stream ms = new MemoryStream();
            ms.Write(DdsHeader, 0x00, 0x80);
            ms.Seek(0x0C, SeekOrigin.Begin);
            ms.Write(BitConverter.GetBytes((uint)height), 0x00, 0x04);
            ms.Write(BitConverter.GetBytes((uint)width), 0x00, 0x04);
            ms.Write(BitConverter.GetBytes(size), 0x00, 0x04);
            ms.Seek(0x1C, SeekOrigin.Begin);
            ms.Write(BitConverter.GetBytes(1), 0x00, 0x04);
            ms.Write(BitConverter.GetBytes((uint)format), 0x00, 0x04);
            ms.Seek(0x57, SeekOrigin.Begin);
            switch (SimplifyTextureFormat(format))
            {
                case IGZ_TextureFormat.dxt1:
                    ms.Write(BitConverter.GetBytes((uint)0x31), 0x00, 0x01);
                    break;
                case IGZ_TextureFormat.dxt5:
                    ms.Write(BitConverter.GetBytes((uint)0x35), 0x00, 0x01);
                    break;
            }
            ms.Seek(0x80, SeekOrigin.Begin);
            IGZ_TextureFormat simpleFormat = SimplifyTextureFormat(format);
            byte[] deswizzled_data = GX2.Decode(surf, 0, 0);
            ms.Write(deswizzled_data, 0, deswizzled_data.Length);
            ms.Seek(0, SeekOrigin.Begin);
            BcDecoder dec = new BcDecoder();
            Image<Rgba32> image = dec.DecodeToImageRgba32(ms);
            BcEncoder enc = new BcEncoder();
            enc.OutputOptions.GenerateMipMaps = true;
            enc.OutputOptions.MaxMipMapLevel = (int)mipmapCount;
            enc.OutputOptions.Quality = CompressionQuality.BestQuality;
            enc.OutputOptions.FileFormat = BCnEncoder.Shared.OutputFileFormat.Dds;
            if (simpleFormat == IGZ_TextureFormat.dxt1)
            {
                enc.OutputOptions.Format = BCnEncoder.Shared.CompressionFormat.Bc1;
            }
            else if (simpleFormat == IGZ_TextureFormat.dxt5)
            {
                enc.OutputOptions.Format = BCnEncoder.Shared.CompressionFormat.Bc3;
            }
            Stream tmp = new MemoryStream();
            enc.EncodeToStream(image, tmp);
            tmp.Seek(0x80, SeekOrigin.Begin);
            byte[] data = new byte[tmp.Length - 0x80];
            tmp.Read(data, 0, data.Length);
            ms.Close();
            tmp.Close();
            return data;
        }
        public static byte[] SwizzleWiiU(byte[] imageData, uint width, uint height, uint mipmapCount, IGZ_TextureFormat format, GX2.GX2TileMode tileMode = GX2.GX2TileMode.MODE_DEFAULT)
        {
            if (SimplifyTextureFormat(format) == IGZ_TextureFormat.dxt1_tile_linux) return new byte[0];
            uint tm;
            if (tileMode == GX2.GX2TileMode.MODE_DEFAULT) tm = GX2.getDefaultGX2TileMode((uint)GX2.GX2SurfaceDimension.DIM_2D, width, height, 1, (uint)format, 0, 1);
            else tm = (uint)tileMode;
            IGZ_TextureFormat simpleFormat = SimplifyTextureFormat(format);
            GX2.GX2Surface surf = GX2.CreateGx2Texture(imageData, "", tm, 0, width, height, 1, (uint)(simpleFormat == IGZ_TextureFormat.dxt1 ? GX2.GX2SurfaceFormat.T_BC1_UNORM : GX2.GX2SurfaceFormat.T_BC5_UNORM), 0, (uint)GX2.GX2SurfaceDimension.DIM_2D, mipmapCount);
            return GX2Utils.Utils.CombineByteArray(new byte[][] { surf.data, surf.mipData });
        }
        public static void ExtractDDS(Stream src, Stream dst, int width, int height, uint size, uint mipmapCount, IGZ_TextureFormat format, bool leaveOpen = false)
        {
            if (SimplifyTextureFormat(format) == IGZ_TextureFormat.dxt1_tile_linux) return;
            dst.Write(DdsHeader, 0x00, 0x80);
            dst.Seek(0x0C, SeekOrigin.Begin);
            dst.Write(BitConverter.GetBytes((uint)height), 0x00, 0x04);
            dst.Write(BitConverter.GetBytes((uint)width), 0x00, 0x04);
            dst.Write(BitConverter.GetBytes(size), 0x00, 0x04);
            dst.Seek(0x1C, SeekOrigin.Begin);
            dst.Write(BitConverter.GetBytes(mipmapCount), 0x00, 0x04);
            dst.Write(BitConverter.GetBytes((uint)format), 0x00, 0x04);
            dst.Seek(0x57, SeekOrigin.Begin);
            switch (SimplifyTextureFormat(format))
            {
                case IGZ_TextureFormat.dxt1:
                    dst.Write(BitConverter.GetBytes((uint)0x31), 0x00, 0x01);
                    break;
                case IGZ_TextureFormat.dxt5:
                    dst.Write(BitConverter.GetBytes((uint)0x35), 0x00, 0x01);
                    break;
            }
            dst.Seek(0x80, SeekOrigin.Begin);
            byte[] data = new byte[size];
            src.Read(data, 0x00, (int)size);
            dst.Write(data, 0, (int)size);
            dst.Flush();
            if (!leaveOpen)
            {
                dst.Close();
            }
        }

        public static bool Extract(Stream src, Stream dst, int width, int height, uint size, uint mipmapCount, IGZ_TextureFormat format, bool leaveOpen = false) {
            if (SimplifyTextureFormat(format) == IGZ_TextureFormat.dxt1_tile_linux) return false;
            dst.Write(DdsHeader, 0x00, 0x80);
            dst.Seek(0x0C, SeekOrigin.Begin); // 12
            dst.Write(BitConverter.GetBytes((uint)height), 0x00, 0x04);
            dst.Write(BitConverter.GetBytes((uint)width), 0x00, 0x04);
            dst.Write(BitConverter.GetBytes(size), 0x00, 0x04);
            dst.Seek(0x1C, SeekOrigin.Begin);
            dst.Write(BitConverter.GetBytes(mipmapCount), 0x00, 0x04);
            dst.Write(BitConverter.GetBytes((uint) format), 0x00, 0x04);
            dst.Seek(0x57, SeekOrigin.Begin);
            switch (SimplifyTextureFormat(format)) {
                case IGZ_TextureFormat.dxt1:
                    dst.Write(BitConverter.GetBytes((uint)0x31), 0x00, 0x01);
                    break;
                case IGZ_TextureFormat.dxt5:
                    dst.Write(BitConverter.GetBytes((uint)0x35), 0x00, 0x01);
                    break;
            }

            dst.Seek(0x80, SeekOrigin.Begin);

            //Copy data to buffer so we can unswizzle it
            byte[] data = new byte[size];
            src.Read(data, 0x00, (int)size);
            int stride = (SimplifyTextureFormat(format) == IGZ_TextureFormat.dxt1 ? 0x08 : 0x10);

            //Unsizzle data if needed
            bool mode = false;
            if (format.ToString().EndsWith("_tile_cafe"))
            {
                byte[] deswizzled_data;
                deswizzled_data = DeswizzleWiiU(data, (uint)width, (uint)height, size, mipmapCount, format, ref mode);
                dst.Write(deswizzled_data, 0, deswizzled_data.Length);
            }
            else
            {
                for (var i = 0; i < size; i += stride)
                {
                    if (format.ToString().EndsWith("_big_wii"))
                    {
                        //width must be rounded up to nearest 4
                        int alignedWidth = ((width + 3) / 4) * 4;

                        //if we wanted the actual x and y block indexes we would've used (width >> 2) but since 2x2 sets of blocks are stored as 4x1 sets of blocks, we pretend the image is double the width
                        int xindex = ((i / stride) % (alignedWidth >> 1)) >> 1;
                        int yindex = ((i / stride) / (alignedWidth >> 1)) << 1;

                        //We're reading 4x1 sets of blocks
                        byte[] currentBlocks = new byte[stride * 4];
                        Array.Copy(data, i, currentBlocks, 0, currentBlocks.Length);

                        //Fixing the endianness of the 2 r5g6b5 colours
                        for (int j = 0; j < 4; j++)
                        {
                            Array.Reverse(currentBlocks, stride * j + 0, 2);
                            Array.Reverse(currentBlocks, stride * j + 2, 2);
                        }

                        //Flipping the pixels within each block horizontally
                        for (int j = 0; j < currentBlocks.Length; j += 8)
                        {
                            byte[] currentRow = new byte[4];
                            Array.Copy(currentBlocks, j + 4, currentRow, 0, 4);
                            for (int k = 0; k < 4; k++)
                            {
                                currentRow[k] = (byte)(((currentRow[k] & 3) << 6) | ((currentRow[k] & 12) << 2) | ((currentRow[k] & 48) >> 2) | ((currentRow[k] & 192) >> 6));
                            }

                            Array.Copy(currentRow, 0, currentBlocks, j + 4, 4);
                        }

                        //Writing the two blocks that would normally be on this row
                        dst.Seek(0x80 + (xindex + yindex * (alignedWidth >> 2)) * stride, SeekOrigin.Begin);
                        dst.Write(currentBlocks, 0, stride * 2);

                        //Writing the two blocks that would be above the previous two
                        dst.Seek(0x80 + (xindex + (yindex + 1) * (alignedWidth >> 2)) * stride, SeekOrigin.Begin);
                        dst.Write(currentBlocks, stride * 2, stride * 2);

                        //We dealt with 4 blocks instead of 1 so here we make sure the for loop doesn't run more than it has to
                        i += stride * 3;
                    }
                    else
                    {
                        dst.Write(data, i, stride);
                    }
                }
            }
            dst.Flush();
            if (!leaveOpen)
            {
                dst.Close();
            }

            return mode;
        }

        public static uint CalculateTextureSize(IGZ_TextureFormat simpliefiedFormat, uint width, uint height, uint mipmapCount = 1) {
            switch (simpliefiedFormat) {
                case IGZ_TextureFormat.dxt1:
                case IGZ_TextureFormat.dxt5:
                    uint basicSize = (uint)(Math.Max(1, ((width + 3) / 4)) * Math.Max(1, ((height + 3) / 4))); //Taken from the DDS programming guide
                    uint finalSize = 0;
                    for (uint i = 0; i < mipmapCount; i++) {
                        finalSize += (uint)Math.Max(basicSize / Math.Pow(4, i), (simpliefiedFormat == IGZ_TextureFormat.dxt1 ? 0x08 : 0x10));
                    }

                    return finalSize * (uint)(simpliefiedFormat == IGZ_TextureFormat.dxt1 ? 0x08 : 0x10);
                //case IGZ_TextureFormat.dxt3:
                default:
                    throw new NotImplementedException("This format is unsupported");
            }
        }

        public static async void Replace(Stream src, Stream dst, int width, int height, uint size, uint mipmapCount, IGZ_TextureFormat format, bool mode)
        {
            if (SimplifyTextureFormat(format) == IGZ_TextureFormat.dxt1_tile_linux) return;
            src.Seek(0x00, SeekOrigin.Begin);
            byte[] magic = new byte[4];
            src.Read(magic, 0x00, 0x04);
            Image<Rgba32> image = null;
            src.Seek(0x00, SeekOrigin.Begin);
            if (System.Text.Encoding.ASCII.GetString(magic) == "DDS ") {
                BcDecoder dec = new BcDecoder();
                image = dec.DecodeToImageRgba32(src);
            }
            else {
                image = SixLabors.ImageSharp.Image.Load<Rgba32>(src);
            }

            image.Mutate(n => n.Resize((int)width, (int)height));

            IGZ_TextureFormat simpleFormat = SimplifyTextureFormat(format);

            BcEncoder enc = new BcEncoder();
            enc.OutputOptions.GenerateMipMaps = true;
            enc.OutputOptions.MaxMipMapLevel = (int)mipmapCount;
            enc.OutputOptions.Quality = CompressionQuality.BestQuality;
            enc.OutputOptions.FileFormat = BCnEncoder.Shared.OutputFileFormat.Dds;
            if (simpleFormat == IGZ_TextureFormat.dxt1) {
                enc.OutputOptions.Format = BCnEncoder.Shared.CompressionFormat.Bc1;
            }
            else if (simpleFormat == IGZ_TextureFormat.dxt5) {
                Console.WriteLine("DXT5 Texture");
                enc.OutputOptions.Format = BCnEncoder.Shared.CompressionFormat.Bc3;
            }

            MemoryStream tempStream = new MemoryStream();
            MemoryStream oms = new MemoryStream();
            enc.EncodeToStream(image, tempStream);

            if (format.ToString().EndsWith("_tile_cafe"))
            {
                tempStream.Seek(0, SeekOrigin.Begin);
                tempStream.CopyTo(oms);
            }
            else
            {
                tempStream.Seek(0, SeekOrigin.Begin);
                tempStream.CopyTo(oms);
            }
            tempStream.Close();

            //Copy data to buffer so we can swizzle it
            oms.Seek(0x80, SeekOrigin.Begin);
            byte[] data = new byte[size];
            oms.Read(data, 0x00, (int)size);
            if (format.ToString().EndsWith("_tile_cafe"))
            {
                data = SwizzleWiiU(data, (uint)width, (uint)height, mipmapCount, format, mode ? GX2.GX2TileMode.MODE_1D_TILED_THIN1 : GX2.GX2TileMode.MODE_2D_TILED_THIN1);
            }
            int stride = (simpleFormat == IGZ_TextureFormat.dxt1 ? 0x08 : 0x10);
            //Swizzle data if needed
            for (int i = 0; i < size; i += stride) {
                if (format.ToString().EndsWith("_big_wii")) {
                    //width must be rounded up to nearest 4 cos dxt
                    int alignedWidth = ((width + 3) / 4) * 4;

                    //if we wanted the actual x and y block indexes we would've used (width >> 2) but since 2x2 sets of blocks are stored as 4x1 sets of blocks, we pretend the image is double the width
                    int xindex = ((i / stride) % (alignedWidth >> 1)) >> 1;
                    int yindex = ((i / stride) / (alignedWidth >> 1)) << 1;

                    //Index of the first 2 blocks
                    int index0 = (xindex + yindex * (alignedWidth >> 2)) * stride;
                    //Index of the second 2 blocks
                    int index1 = (xindex + (yindex + 1) * (alignedWidth >> 2)) * stride;

                    //Copy the current group of blocks
                    byte[] currentBlocks = new byte[stride * 4];
                    Array.Copy(data, index0, currentBlocks, 0, stride * 2);
                    Array.Copy(data, index1, currentBlocks, 2 * stride, stride * 2);

                    //Flipping the pixels within each block horizontally
                    for (int j = 0; j < currentBlocks.Length; j += 8) {
                        byte[] currentRow = new byte[4];
                        Array.Copy(currentBlocks, j + 4, currentRow, 0, 4);
                        for (int k = 0; k < 4; k++) {
                            currentRow[k] = (byte)(((currentRow[k] & 3) << 6) | ((currentRow[k] & 12) << 2) | ((currentRow[k] & 48) >> 2) | ((currentRow[k] & 192) >> 6));
                        }

                        Array.Copy(currentRow, 0, currentBlocks, j + 4, 4);
                    }

                    //Unfixing the endianness of the 2 r5g6b5 colours
                    for (int j = 0; j < 4; j++) {
                        Array.Reverse(currentBlocks, stride * j + 0, 2);
                        Array.Reverse(currentBlocks, stride * j + 2, 2);
                    }

                    //Write the swizzled data to the destination
                    dst.Write(currentBlocks, 0x00, stride * 4);

                    //We dealt with 4 blocks instead of 1 so here we make sure the for loop doesn't run more than it has to
                    i += stride * 3;
                }
                else {
                    dst.Write(data, i, stride);
                }
            }

            oms.Close();
            dst.Flush();
        }
        
        // GlitchedGamer wrote this
        public static void ReplaceDDS(Stream src, Stream dst, int width, uint size, IGZ_TextureFormat format)
        {
            if (SimplifyTextureFormat(format) == IGZ_TextureFormat.dxt1_tile_linux) return;
            Console.WriteLine(format);
            IGZ_TextureFormat simpleFormat = SimplifyTextureFormat(format);
            src.Seek(0x80, SeekOrigin.Begin);
            byte[] data = new byte[size];
            src.Read(data, 0x00, (int)size);
            dst.Write(data, 0, (int)size);
            src.Close();
            dst.Flush();
        }

        public static IGZ_TextureFormat SimplifyTextureFormat(IGZ_TextureFormat format) {
            switch (format) {
                case IGZ_TextureFormat.dxt1:
                case IGZ_TextureFormat.dxt1_android:
                case IGZ_TextureFormat.dxt1_big_ps3:
                case IGZ_TextureFormat.dxt1_big_wii:
                case IGZ_TextureFormat.dxt1_big_xenon:
                case IGZ_TextureFormat.dxt1_cafe:
                case IGZ_TextureFormat.dxt1_durango:
                case IGZ_TextureFormat.dxt1_dx:
                case IGZ_TextureFormat.dxt1_dx11:
                case IGZ_TextureFormat.dxt1_lgtv:
                case IGZ_TextureFormat.dxt1_linux:
                case IGZ_TextureFormat.dxt1_nx:
                case IGZ_TextureFormat.dxt1_old:
                case IGZ_TextureFormat.dxt1_osx:
                case IGZ_TextureFormat.dxt1_ps4:
                case IGZ_TextureFormat.dxt1_wgl:
                case IGZ_TextureFormat.dxt1_srgb:
                case IGZ_TextureFormat.dxt1_srgb_android:
                case IGZ_TextureFormat.dxt1_srgb_big_ps3:
                case IGZ_TextureFormat.dxt1_srgb_big_wii:
                case IGZ_TextureFormat.dxt1_srgb_big_xenon:
                case IGZ_TextureFormat.dxt1_srgb_cafe:
                case IGZ_TextureFormat.dxt1_srgb_durango:
                case IGZ_TextureFormat.dxt1_srgb_dx:
                case IGZ_TextureFormat.dxt1_srgb_dx11:
                case IGZ_TextureFormat.dxt1_srgb_linux:
                case IGZ_TextureFormat.dxt1_srgb_nx:
                case IGZ_TextureFormat.dxt1_srgb_osx:
                case IGZ_TextureFormat.dxt1_srgb_ps4:
                case IGZ_TextureFormat.dxt1_srgb_wgl:
                case IGZ_TextureFormat.dxt1_tile:
                case IGZ_TextureFormat.dxt1_tile_android:
                case IGZ_TextureFormat.dxt1_tile_big_ps3:
                case IGZ_TextureFormat.dxt1_tile_big_wii:
                case IGZ_TextureFormat.dxt1_tile_big_xenon:
                case IGZ_TextureFormat.dxt1_tile_cafe:
                case IGZ_TextureFormat.dxt1_tile_durango:
                case IGZ_TextureFormat.dxt1_tile_dx:
                case IGZ_TextureFormat.dxt1_tile_dx11:
                case IGZ_TextureFormat.dxt1_tile_linux:
                case IGZ_TextureFormat.dxt1_tile_nx:
                case IGZ_TextureFormat.dxt1_tile_osx:
                case IGZ_TextureFormat.dxt1_tile_ps4:
                case IGZ_TextureFormat.dxt1_tile_wgl:
                    return IGZ_TextureFormat.dxt1;
                case IGZ_TextureFormat.dxt5:
                case IGZ_TextureFormat.dxt5_android:
                case IGZ_TextureFormat.dxt5_big_ps3:
                case IGZ_TextureFormat.dxt5_big_wii:
                case IGZ_TextureFormat.dxt5_big_xenon:
                case IGZ_TextureFormat.dxt5_cafe:
                case IGZ_TextureFormat.dxt5_durango:
                case IGZ_TextureFormat.dxt5_dx:
                case IGZ_TextureFormat.dxt5_dx11:
                case IGZ_TextureFormat.dxt5_lgtv:
                case IGZ_TextureFormat.dxt5_linux:
                case IGZ_TextureFormat.dxt5_nx:
                case IGZ_TextureFormat.dxt5_old:
                case IGZ_TextureFormat.dxt5_osx:
                case IGZ_TextureFormat.dxt5_ps4:
                case IGZ_TextureFormat.dxt5_wgl:
                case IGZ_TextureFormat.dxt5_srgb:
                case IGZ_TextureFormat.dxt5_srgb_android:
                case IGZ_TextureFormat.dxt5_srgb_big_ps3:
                case IGZ_TextureFormat.dxt5_srgb_big_wii:
                case IGZ_TextureFormat.dxt5_srgb_big_xenon:
                case IGZ_TextureFormat.dxt5_srgb_cafe:
                case IGZ_TextureFormat.dxt5_srgb_durango:
                case IGZ_TextureFormat.dxt5_srgb_dx:
                case IGZ_TextureFormat.dxt5_srgb_dx11:
                case IGZ_TextureFormat.dxt5_srgb_linux:
                case IGZ_TextureFormat.dxt5_srgb_nx:
                case IGZ_TextureFormat.dxt5_srgb_osx:
                case IGZ_TextureFormat.dxt5_srgb_ps4:
                case IGZ_TextureFormat.dxt5_srgb_wgl:
                case IGZ_TextureFormat.dxt5_tile:
                case IGZ_TextureFormat.dxt5_tile_android:
                case IGZ_TextureFormat.dxt5_tile_big_ps3:
                case IGZ_TextureFormat.dxt5_tile_big_wii:
                case IGZ_TextureFormat.dxt5_tile_big_xenon:
                case IGZ_TextureFormat.dxt5_tile_cafe:
                case IGZ_TextureFormat.dxt5_tile_durango:
                case IGZ_TextureFormat.dxt5_tile_dx:
                case IGZ_TextureFormat.dxt5_tile_dx11:
                case IGZ_TextureFormat.dxt5_tile_linux:
                case IGZ_TextureFormat.dxt5_tile_nx:
                case IGZ_TextureFormat.dxt5_tile_osx:
                case IGZ_TextureFormat.dxt5_tile_ps4:
                case IGZ_TextureFormat.dxt5_tile_wgl:
                    return IGZ_TextureFormat.dxt5;
                default:
                    //throw new NotImplementedException($"{((uint)format).ToString("X08")} is unsupported");
                    return IGZ_TextureFormat.dxt1_tile_linux;
            }
        }
    }
}