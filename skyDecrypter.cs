using System.Security.Cryptography;

namespace STTUMM
{
    //Original code: https://github.com/SigmaDolphin/SkylanderEditor/blob/master/skyDecrypter.vb
    //Translated by: GlitchedGamer
    public class skyDecrypter
    {
        //decrypts a full skylander byte array
        public static byte[] decryptSkylander(byte[] data)
        {
            byte[] actikey = [0x20, 0x43, 0x6F, 0x70, 0x79, 0x72, 0x69, 0x67, 0x68, 0x74, 0x20, 0x28, 0x43, 0x29, 0x20, 0x32, 0x30, 0x31, 0x30, 0x20, 0x41, 0x63, 0x74, 0x69, 0x76, 0x69, 0x73, 0x69, 0x6F, 0x6E, 0x2E, 0x20, 0x41, 0x6C, 0x6C, 0x20, 0x52, 0x69, 0x67, 0x68, 0x74, 0x73, 0x20, 0x52, 0x65, 0x73, 0x65, 0x72, 0x76, 0x65, 0x64, 0x2E, 0x20];
            byte[] keyInput = new byte[86];
            byte[] decSky = new byte[1024];
            byte[] encData = new byte[16];
            byte[] decData = new byte[16];
            byte[] key = new byte[16];
            int h;
            
            h = 0;
            //we prepare the byte series used to generate the AES key
            Array.Copy(data, 0, keyInput, 0, 32);
            Array.Copy(actikey, 0, keyInput, 33, 53);

            //we avoid decrypting blocks up to 8 since they are never encrypted
            //we also avoid decrypting Imaginator checksums
            //we also avoid decrypting the access control blocks
            do
            {
                if ((h - 3) % 4 == 0 || h < 8 || h == 34 || h == 62)
                {
                    Array.Copy(data, h * 16, decSky, h * 16, 16);
                }
                else
                {
                    keyInput[32] = (byte)h;
                    Array.Copy(data, 16 * h, encData, 0, 16);
                    key = CalculateMD5Hash(keyInput);
                    decData = AESD(encData, key);
                    Array.Copy(decData, 0, decSky, h * 16, 16);
                }
                h = h + 1;
            } while (h <= 63);
            return decSky;
        }

        //encrypts a full skylander byte array
        public static byte[] encryptSkylander(byte[] skyData)
        {
            byte[] actikey = [0x20, 0x43, 0x6F, 0x70, 0x79, 0x72, 0x69, 0x67, 0x68, 0x74, 0x20, 0x28, 0x43, 0x29, 0x20, 0x32, 0x30, 0x31, 0x30, 0x20, 0x41, 0x63, 0x74, 0x69, 0x76, 0x69, 0x73, 0x69, 0x6F, 0x6E, 0x2E, 0x20, 0x41, 0x6C, 0x6C, 0x20, 0x52, 0x69, 0x67, 0x68, 0x74, 0x73, 0x20, 0x52, 0x65, 0x73, 0x65, 0x72, 0x76, 0x65, 0x64, 0x2E, 0x20];
            byte[] keyInput = new byte[86];
            byte[] encSky = new byte[1024];
            byte[] encData = new byte[16];
            byte[] decData = new byte[16];
            byte[] key = new byte[16];
            int h;

            h = 0;
            //we prepare the byte series used to generate the AES key
            Array.Copy(skyData, 0, keyInput, 0, 32);
            Array.Copy(actikey, 0, keyInput, 33, 53);

            //we avoid encrypting blocks up to 8 since they are never encrypted
            //we also avoid encrypting Imaginator checksums
            //we also avoid encrypting the access control blocks
            do
            {
                if ((h - 3) % 4 == 0 || h < 8 || h == 34 || h == 62)
                {
                    Array.Copy(skyData, h * 16, encSky, h * 16, 16);
                }
                else
                {
                    keyInput[32] = (byte)h;
                    Array.Copy(skyData, 16 * h, decData, 0, 16);
                    key = CalculateMD5Hash(keyInput);
                    encData = AESE(decData, key);
                    Array.Copy(encData, 0, encSky, h * 16, 16);
                }
                h = h + 1;
            } while (h <= 63);
            return encSky;
        }

        //generates a 0'd skylander array
        public static byte[] cleanSkylander(byte[] skyData)
        {
            byte[] clnSky = new byte[1024];
            byte[] blnkBytes = new byte[16];
            int h = 0;

            //we 0 everything but blocks lower than 5 or the Imaginator checksums
            do
            {
                if (((h - 3) % 4 == 0) || h < 5 || h == 34 || h == 62)
                {
                    Array.Copy(skyData, h * 16, clnSky, h * 16, 16);
                }
                else
                {
                    Array.Copy(blnkBytes, 0, clnSky, h * 16, 16);
                }
                h++;
            } while (h <= 63);

            return clnSky;
        }

        //this function is to detect blank/new skylanders or figures that have been initialized and only have touched SSA
        //state = 0 - normal
        //state = 1 - blank
        //state = 2 - SSA only data
        public static int checkBlankSkylander(byte[] skyData)
        {
            int h;
            bool areaA = false;
            bool areaB = false;

            h = 272;
            do
            {
                if (skyData[h] != 0)
                {
                    areaA = true;
                    break;
                }
                h++;
            } while (h <= 288);

            h = 720;
            do
            {
                if (skyData[h] != 0)
                {
                    areaB = true;
                    break;
                }
                h++;
            } while (h <= 736);

            if (areaA && areaB)
                return 0;

            h = 128;
            do
            {
                if (skyData[h] != 0)
                {
                    areaA = true;
                    break;
                }
                h++;
            } while (h <= 144);

            h = 576;
            do
            {
                if (skyData[h] != 0)
                {
                    areaB = true;
                    break;
                }
                h++;
            } while (h <= 592);

            if (areaA && areaB)
                return 2;

            return 1;
        }

        //function to initialize Giants and forward blocks in case an SSA only figure is detected
        public static void initializeSSA(byte[] skyData)
        {
            byte[] blnkBytes = new byte[16];
            Array.Copy(blnkBytes, 0, skyData, 17 * 16, 16);
            Array.Copy(blnkBytes, 0, skyData, 18 * 16, 16);
            Array.Copy(blnkBytes, 0, skyData, 20 * 16, 16);
            Array.Copy(blnkBytes, 0, skyData, 21 * 16, 16);

            Array.Copy(blnkBytes, 0, skyData, 45 * 16, 16);
            Array.Copy(blnkBytes, 0, skyData, 46 * 16, 16);
            Array.Copy(blnkBytes, 0, skyData, 48 * 16, 16);
            Array.Copy(blnkBytes, 0, skyData, 49 * 16, 16);

            skyData[274] = 1;
            skyData[722] = 2;
        }

        //we require the MD5 to generate the AES key
        public static byte[] CalculateMD5Hash(byte[] input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(input);
                return hash;
            }
        }

        //AES encryption and decryption functions
        public static byte[] AESE(byte[] input, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Padding = PaddingMode.Zeros;
                aes.Key = key;
                aes.Mode = CipherMode.ECB;

                ICryptoTransform DESEncrypter = aes.CreateEncryptor();
                byte[] Buffer = input;
                return DESEncrypter.TransformFinalBlock(Buffer, 0, Buffer.Length);
            }
        }
        public static byte[] AESD(byte[] input, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Padding = PaddingMode.Zeros;
                aes.Key = key;
                aes.Mode = CipherMode.ECB;

                ICryptoTransform DESDecrypter = aes.CreateDecryptor();
                byte[] Buffer = input;
                return DESDecrypter.TransformFinalBlock(Buffer, 0, Buffer.Length);
            }
        }
    }
}
