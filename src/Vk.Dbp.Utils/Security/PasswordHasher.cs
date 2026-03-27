using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Dabp.Utils.Security
{
    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;
        private const char Delimiter = '$';

        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = Pbkdf2(password, salt, Iterations, HashSize);

            string base64Salt = Convert.ToBase64String(salt);
            string base64Hash = Convert.ToBase64String(hash);

            return $"PBKDF2{Delimiter}{Iterations}{Delimiter}{base64Salt}{Delimiter}{base64Hash}";
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrEmpty(hashedPassword))
                return false;

            string[] parts = hashedPassword.Split(Delimiter);
            if (parts.Length != 4 || parts[0] != "PBKDF2")
            {
                return SlowEquals(password, hashedPassword);
            }

            if (!int.TryParse(parts[1], out int iterations))
                return false;

            byte[] salt;
            byte[] storedHash;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                storedHash = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            if (salt.Length != SaltSize || storedHash.Length != HashSize)
                return false;

            byte[] computedHash = Pbkdf2(password, salt, iterations, storedHash.Length);

            return ConstantTimeEquals(computedHash, storedHash);
        }

        private byte[] Pbkdf2(string password, byte[] salt, int iterations, int outputSize)
        {
            var pbkdf2 = new Pkcs5S2ParametersGenerator(new Sha256Digest());
            pbkdf2.Init(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations
            );

            var key = (KeyParameter)pbkdf2.GenerateDerivedMacParameters(outputSize * 8);
            return key.GetKey();
        }

        private bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null)
                return false;

            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }

        private bool SlowEquals(string password, string storedValue)
        {
            byte[] dummyHash = Pbkdf2(password, new byte[SaltSize], Iterations, HashSize);
            byte[] dummyStored = Encoding.UTF8.GetBytes(storedValue ?? string.Empty);

            uint diff = (uint)dummyHash.Length ^ (uint)dummyStored.Length;
            for (int i = 0; i < dummyHash.Length && i < dummyStored.Length; i++)
            {
                diff |= (uint)(dummyHash[i] ^ dummyStored[i]);
            }

            return false;
        }
    }
}
