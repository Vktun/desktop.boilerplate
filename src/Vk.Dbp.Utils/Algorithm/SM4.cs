using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Dabp.Utils.Algorithm
{
    /// <summary>
    /// SM4/CBC/PKCS7 加解密工具。
    /// 密钥必须由调用方提供（建议从 appsettings.local.json 或环境变量读取）。
    /// 每次加密生成随机 16 字节 IV，IV 前置于密文输出。
    /// </summary>
    public static class SM4
    {
        private const int KeySize = 16; // SM4 要求 128-bit 密钥
        private const int IvSize = 16;  // CBC 模式 IV 与分组大小相同

        /// <summary>
        /// SM4/CBC/PKCS7 加密。输出格式: Base64(IV + CipherText)。
        /// </summary>
        /// <param name="plainText">明文（不能为 null 或空）</param>
        /// <param name="key">16 字节密钥字符串（不能为 null 或空）</param>
        public static string Encrypt(string plainText, string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(plainText, nameof(plainText));
            ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

            byte[] keyBytes = DeriveKeyBytes(key);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            // 生成随机 IV
            byte[] iv = new byte[IvSize];
            RandomNumberGenerator.Fill(iv);

            var engine = new SM4Engine();
            var mode = new CbcBlockCipher(engine);
            var cipher = new PaddedBufferedBlockCipher(mode);
            cipher.Init(true, new ParametersWithIV(new KeyParameter(keyBytes), iv));

            byte[] cipherBytes = cipher.DoFinal(plainBytes);

            // IV + CipherText 拼接后 Base64
            byte[] result = new byte[iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// SM4/CBC/PKCS7 解密。输入格式: Base64(IV + CipherText)。
        /// </summary>
        /// <param name="cipherText">由 Encrypt 产生的 Base64 字符串</param>
        /// <param name="key">加密时使用的同一密钥</param>
        public static string Decrypt(string cipherText, string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cipherText, nameof(cipherText));
            ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

            byte[] keyBytes = DeriveKeyBytes(key);
            byte[] allBytes = Convert.FromBase64String(cipherText);

            if (allBytes.Length < IvSize + 1)
                throw new ArgumentException("密文数据过短，无法提取 IV。", nameof(cipherText));

            // 提取 IV 和密文
            byte[] iv = new byte[IvSize];
            byte[] actualCipher = new byte[allBytes.Length - IvSize];
            Buffer.BlockCopy(allBytes, 0, iv, 0, IvSize);
            Buffer.BlockCopy(allBytes, IvSize, actualCipher, 0, actualCipher.Length);

            var engine = new SM4Engine();
            var mode = new CbcBlockCipher(engine);
            var cipher = new PaddedBufferedBlockCipher(mode);
            cipher.Init(false, new ParametersWithIV(new KeyParameter(keyBytes), iv));

            byte[] plainBytes = cipher.DoFinal(actualCipher);
            return Encoding.UTF8.GetString(plainBytes);
        }

        private static byte[] DeriveKeyBytes(string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            if (keyBytes.Length < KeySize)
                throw new ArgumentException($"密钥长度不足 {KeySize} 字节（当前 {keyBytes.Length} 字节）。", nameof(key));

            // 截取前 16 字节作为密钥
            byte[] result = new byte[KeySize];
            Buffer.BlockCopy(keyBytes, 0, result, 0, KeySize);
            return result;
        }
    }
}
