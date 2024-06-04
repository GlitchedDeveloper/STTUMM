using System.IO;
using System.Text;
using System.Buffers.Binary;

namespace STTUMM.Tools
{
    public class LanguagePak
    {
        public Stream file;
        public BinaryReader raw;
        public int igz_version;
        public int length_offset;
        public int text_offset;
        public LanguagePak(Stream fs)
        {
            file = fs;
            raw = new BinaryReader(file);
            file.Seek(0x04, SeekOrigin.Begin);
            igz_version = BinaryPrimitives.ReverseEndianness(raw.ReadInt32());
            Console.WriteLine(igz_version);

            switch (igz_version)
            {
                case 5:
                    Console.WriteLine("SSA WII");
                    file.Seek(0x40, SeekOrigin.Begin);
                    text_offset = BinaryPrimitives.ReverseEndianness(raw.ReadInt32());
                    length_offset = 0x44;
                    break;
                case 6:
                    Console.WriteLine("GIANTS");
                    file.Seek(0x94, SeekOrigin.Begin);
                    text_offset = BinaryPrimitives.ReverseEndianness(raw.ReadInt32());
                    length_offset = 0x94;
                    break;
                case 7:
                    Console.WriteLine("SSA WII U");
                    file.Seek(0x98, SeekOrigin.Begin);
                    text_offset = BinaryPrimitives.ReverseEndianness(raw.ReadInt32());
                    length_offset = 0x9C;
                    break;
                case 8:
                    Console.WriteLine("TRAP TEAM");
                    file.Seek(0x98, SeekOrigin.Begin);
                    text_offset = BinaryPrimitives.ReverseEndianness(raw.ReadInt32());
                    length_offset = 0x9C;
                    break;
                default:
                    Console.WriteLine($"Unknown IGZ version {igz_version} sorry");
                    return;
            }
        }

        public string[] unpack()
        {
            file.Seek(text_offset + 4, SeekOrigin.Begin);
            string raw_text = Encoding.BigEndianUnicode.GetString(raw.ReadBytes((int)(file.Length - file.Position)));
            return raw_text.Split(new string[] { "\0" }, StringSplitOptions.None);
        }


        public void pack(string[] all_texts, Stream output)
        {
            List<byte> all_texts_merged = new List<byte>();
            for (int i = 0; i < all_texts.Length; i++)
            {
                all_texts_merged.AddRange(Encoding.BigEndianUnicode.GetBytes(all_texts[i]));
                if (i != all_texts.Length - 1)
                {
                    all_texts_merged.AddRange(new byte[] { 0x00, 0x00 });
                }
            }
            file.Seek(0, SeekOrigin.Begin);
            output.Write(raw.ReadBytes(text_offset));
            output.Write(new byte[4] { 0x00, 0x00, 0x00, 0x00 });
            output.Write(all_texts_merged.ToArray());
            output.Seek(0, SeekOrigin.Begin);
        }
    }
}