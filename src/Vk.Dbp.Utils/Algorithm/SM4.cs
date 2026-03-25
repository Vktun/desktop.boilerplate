using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Text;

namespace Dabp.Utils.Algorithm
{
    public class SM4
    {
        static string Key = "kajdoifjadkvcnoi";

        public static string Encrypt(string plainText)
        {
            // SM4 加密算法的实现
            byte[] keyBytes = Encoding.UTF8.GetBytes(Key.PadRight(16).Substring(0, 16)); // 密钥截断/填充至16字节
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            var engine = new SM4Engine();
            var mode = new CbcBlockCipher(engine); // Wrap SM4Engine in a CBCBlockCipher for compatibility
            var cipher = new PaddedBufferedBlockCipher(mode); // 默认PKCS7填充
            cipher.Init(true, new KeyParameter(keyBytes));

            byte[] cipherBytes = cipher.DoFinal(plainBytes);
            return Convert.ToBase64String(cipherBytes);
        }

        public static string Decrypt(string cipherText)
        {
            //SM4 解密算法的实现
            byte[] keyBytes = Encoding.UTF8.GetBytes(Key.PadRight(16).Substring(0, 16));
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            var engine = new SM4Engine();
            var mode = new CbcBlockCipher(engine); // Wrap SM4Engine in a CBCBlockCipher for compatibility
            var cipher = new PaddedBufferedBlockCipher(mode);
            cipher.Init(false, new KeyParameter(keyBytes));

            byte[] plainBytes = cipher.DoFinal(cipherBytes);
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
