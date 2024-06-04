using System.IO;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Windows.Input;
using System.Xml.Linq;

namespace STTUMM
{
    public class SkylanderDumps
    {
        //Based on https://github.com/mandar1jn/portfolio/blob/master/src/pages/skylanders/generator.astro
        public static byte[] Generate(int id, int variant)
        {
            byte[] data = new byte[1024];

            byte[] uuidArray = Guid.NewGuid().ToByteArray();
            data[0] = uuidArray[0];
            data[1] = uuidArray[1];
            data[2] = uuidArray[2];
            data[3] = uuidArray[3];
            data[4] = (byte)(data[0] ^ data[1] ^ data[2] ^ data[3]);

            for (int i = 0; i < 16; i++)
            {
                List<byte> uidBytes = uuidArray.Take(4).ToList();
                byte[] keyBytes = getKeyA(i, uidBytes);
                for (int j = 0; j < keyBytes.Length; j++)
                {
                    int offset = (i * 64) + (3 * 16) + j;
                    data[offset] = keyBytes[j];
                }
            }

            byte[] idStringArray = BitConverter.GetBytes(id);
            data[16] = idStringArray[0];
            data[17] = idStringArray[1];

            byte[] variantStringArray = BitConverter.GetBytes(variant);
            data[28] = variantStringArray[0];
            data[29] = variantStringArray[1];

            byte[] crcStringArray = BitConverter.GetBytes(crc16(data, 0, 30));
            data[30] = crcStringArray[0];
            data[31] = crcStringArray[1];

            data[54] = 0x0f;
            data[55] = 0x0f;
            data[56] = 0x0f;
            data[57] = 0x69;

            for (int i = 1; i < 16; i++)
            {
                int offset = 64 * i + 48 + 5;
                data[offset + 1] = 0x7f;
                data[offset + 2] = 0x0f;
                data[offset + 3] = 0x08;
                data[offset + 4] = 0x69;
            }

            //Skip adding data like money, exp, playtime, etc... We don't need it.

            // area counters
            data[0x89] = 0x1;
            data[0x112] = 0x1;

            FixCRCs(ref data);
            data = skyDecrypter.encryptSkylander(data);
            return data;
        }
        //https://github.com/SigmaDolphin/SkylanderEditor/blob/master/Form1.vb
        private static void FixCRCs(ref byte[] skylanderBytes)
        {
            string X;

            //update checksums section A
            X = CRCcalculator.checksumCalc(2, 1, skylanderBytes);
            skylanderBytes[140] = Convert.ToByte(X.Substring(0, 2), 16);
            skylanderBytes[141] = Convert.ToByte(X.Substring(X.Length - 2, 2), 16);

            X = CRCcalculator.checksumCalc(3, 1, skylanderBytes);
            skylanderBytes[138] = Convert.ToByte(X.Substring(0, 2), 16);
            skylanderBytes[139] = Convert.ToByte(X.Substring(X.Length - 2, 2), 16);

            X = CRCcalculator.checksumCalc(4, 1, skylanderBytes);
            skylanderBytes[272] = Convert.ToByte(X.Substring(0, 2), 16);
            skylanderBytes[273] = Convert.ToByte(X.Substring(X.Length - 2, 2), 16);

            X = CRCcalculator.checksumCalc(1, 1, skylanderBytes);
            skylanderBytes[142] = Convert.ToByte(X.Substring(0, 2), 16);
            skylanderBytes[143] = Convert.ToByte(X.Substring(X.Length - 2, 2), 16);

            //update checksums section B
            X = CRCcalculator.checksumCalc(2, 2, skylanderBytes);
            skylanderBytes[588] = Convert.ToByte(X.Substring(0, 2), 16);
            skylanderBytes[589] = Convert.ToByte(X.Substring(X.Length - 2, 2), 16);

            X = CRCcalculator.checksumCalc(3, 2, skylanderBytes);
            skylanderBytes[586] = Convert.ToByte(X.Substring(0, 2), 16);
            skylanderBytes[587] = Convert.ToByte(X.Substring(X.Length - 2, 2), 16);

            X = CRCcalculator.checksumCalc(4, 2, skylanderBytes);
            skylanderBytes[720] = Convert.ToByte(X.Substring(0, 2), 16);
            skylanderBytes[721] = Convert.ToByte(X.Substring(X.Length - 2, 2), 16);

            X = CRCcalculator.checksumCalc(1, 2, skylanderBytes);
            skylanderBytes[590] = Convert.ToByte(X.Substring(0, 2), 16);
            skylanderBytes[591] = Convert.ToByte(X.Substring(X.Length - 2, 2), 16);
        }

        public static int GetID(byte[] data)
        {
            data = skyDecrypter.decryptSkylander(data);
            return BitConverter.ToInt16([data[16], data[17]], 0);
        }
        public static int GetVariantID(byte[] data)
        {
            data = skyDecrypter.decryptSkylander(data);
            return BitConverter.ToInt16([data[28], data[29]], 0);
        }

        public static byte[] SetID(byte[] data, int id)
        {
            data = skyDecrypter.decryptSkylander(data);
            byte[] idStringArray = BitConverter.GetBytes(id);
            data[16] = idStringArray[0];
            data[17] = idStringArray[1];

            byte[] crcStringArray = BitConverter.GetBytes(crc16(data, 0, 30));
            data[30] = crcStringArray[0];
            data[31] = crcStringArray[1];

            FixCRCs(ref data);
            data = skyDecrypter.encryptSkylander(data);
            return data;
        }
        private static long computeCRC48(List<byte> data)
        {
            long polynomial = 0x42f0e1eba9ea3693L;

            long register = 170325570882756;
            for (int i = 0; i < data.Count; i++)
            {
                register ^= (long)data[i] << 40;
                for (int j = 0; j < 8; j++)
                {
                    if ((register & 0x800000000000L) != 0)
                    {
                        register = (register << 1) ^ polynomial;
                    }
                    else
                    {
                        register <<= 1;
                    }

                    register &= 0x0000FFFFFFFFFFFF;
                }
            }
            return register;
        }

        private static byte[] getKeyA(int sector, List<byte> uid)
        {
            if (sector == 0)
            {
                return [75, 11, 32, 16, 124, 203];
            }

            if (sector < 0 || sector > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(sector), "Sector index out of range");
            }

            uid.Add((byte)sector);
            long crc48 = computeCRC48(uid);

            return BitConverter.GetBytes(crc48).Take(6).ToArray();
        }

        private static int crc16(byte[] data, int offset, int length)
        {
            if (data == null || offset < 0 || offset > data.Length - 1 || offset + length > data.Length)
            {
                return 0;
            }

            int crc = 0xFFFF;
            for (int i = 0; i < length; ++i)
            {
                crc ^= data[offset + i] << 8;
                for (int j = 0; j < 8; ++j)
                {
                    crc = (crc & 0x8000) > 0 ? (crc << 1) ^ 0x1021 : crc << 1;
                }
            }
            return crc & 0xFFFF;
        }
    }
}
