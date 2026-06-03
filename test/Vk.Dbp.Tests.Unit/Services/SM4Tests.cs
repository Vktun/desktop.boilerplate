using System;
using Dabp.Utils.Algorithm;
using FluentAssertions;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Services
{
    public class SM4Tests
    {
        private const string DefaultKey = "DabpSm4DefaultKey";

        [Fact]
        public void Encrypt_Then_Decrypt_ReturnsOriginalText()
        {
            // Arrange
            string plainText = "Server=127.0.0.1;Database=DabpCore;Trusted_Connection=True;";

            // Act
            string encrypted = SM4.Encrypt(plainText, DefaultKey);
            string decrypted = SM4.Decrypt(encrypted, DefaultKey);

            // Assert
            decrypted.Should().Be(plainText);
        }

        [Fact]
        public void Encrypt_ProducesDifferentCipherText_EachTime()
        {
            // Arrange
            string plainText = "SamePlainText";

            // Act
            string encrypted1 = SM4.Encrypt(plainText, DefaultKey);
            string encrypted2 = SM4.Encrypt(plainText, DefaultKey);

            // Assert — random IV means different output each time
            encrypted1.Should().NotBe(encrypted2, "每次加密应使用随机IV，产生不同密文");
        }

        [Fact]
        public void Decrypt_WithWrongKey_ThrowsOrReturnsGarbage()
        {
            // Arrange
            string plainText = "SecretMessage";
            string encrypted = SM4.Encrypt(plainText, DefaultKey);

            // Act
            Action act = () => SM4.Decrypt(encrypted, "WrongKey123456789");

            // Assert — wrong key should throw (padding error) or produce garbage
            act.Should().Throw<Exception>("错误的密钥应导致解密失败");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Encrypt_NullOrEmptyPlainText_ThrowsArgumentException(string plainText)
        {
            // Act
            Action act = () => SM4.Encrypt(plainText, DefaultKey);

            // Assert
            act.Should().Throw<ArgumentException>("空或null明文应该抛出异常");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Encrypt_NullOrEmptyKey_ThrowsArgumentException(string key)
        {
            // Act
            Action act = () => SM4.Encrypt("test", key);

            // Assert
            act.Should().Throw<ArgumentException>("空或null密钥应该抛出异常");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Decrypt_NullOrEmptyCipherText_ThrowsArgumentException(string cipherText)
        {
            // Act
            Action act = () => SM4.Decrypt(cipherText, DefaultKey);

            // Assert
            act.Should().Throw<ArgumentException>("空或null密文应该抛出异常");
        }

        [Fact]
        public void Encrypt_Decrypt_ChineseText_RoundTrips()
        {
            // Arrange
            string plainText = "数据库连接字符串测试中文内容";

            // Act
            string encrypted = SM4.Encrypt(plainText, DefaultKey);
            string decrypted = SM4.Decrypt(encrypted, DefaultKey);

            // Assert
            decrypted.Should().Be(plainText, "中文文本加解密应正确还原");
        }

        [Fact]
        public void Encrypt_Decrypt_LongText_RoundTrips()
        {
            // Arrange — text longer than one SM4 block (16 bytes)
            string plainText = new string('A', 500);

            // Act
            string encrypted = SM4.Encrypt(plainText, DefaultKey);
            string decrypted = SM4.Decrypt(encrypted, DefaultKey);

            // Assert
            decrypted.Should().Be(plainText, "长文本加解密应正确还原");
        }

        [Fact]
        public void Decrypt_InvalidBase64_ThrowsFormatException()
        {
            // Act
            Action act = () => SM4.Decrypt("not-valid-base64!!!", DefaultKey);

            // Assert
            act.Should().Throw<FormatException>("无效的Base64密文应抛出格式异常");
        }

        [Fact]
        public void Decrypt_TooShortCipherText_ThrowsArgumentException()
        {
            // Arrange — Base64 of 5 bytes, less than IV size
            string tooShort = Convert.ToBase64String(new byte[5]);

            // Act
            Action act = () => SM4.Decrypt(tooShort, DefaultKey);

            // Assert
            act.Should().Throw<ArgumentException>("密文数据过短应抛出异常");
        }
    }
}
