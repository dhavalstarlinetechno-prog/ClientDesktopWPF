using ClientDesktop.Core.Config;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace ClientDesktop.Infrastructure.Helpers
{
    public static class AESHelper
    {
        #region Keys And Configuration
        private static readonly byte[] Key = Encoding.UTF8.GetBytes(AppConfig.EncryptionKey); // 32 bytes
        private static readonly byte[] IV = Encoding.UTF8.GetBytes(AppConfig.EncryptionIV); // 16 bytes
        #endregion

        #region Compression And Encryption
        // Compress the plaintext data, then encrypt it
        public static string CompressAndEncryptString(string plainText)
        {
            using (var ms = new MemoryStream())
            {
                // Compress data using GZipStream
                using (var gzip = new GZipStream(ms, CompressionMode.Compress))
                using (var sw = new StreamWriter(gzip))
                {
                    sw.Write(plainText);
                }

                // Get compressed data
                byte[] compressedData = ms.ToArray();

                // Encrypt the compressed data
                return EncryptString(Convert.ToBase64String(compressedData));
            }
        }

        // Encrypt a string (Base64 encoded data)
        public static string EncryptString(string plainText)
        {
            using (var aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
        #endregion

        #region Decryption And Decompression
        // Decrypt a string and return the original plaintext
        public static string DecryptString(string cipherText)
        {
            var buffer = Convert.FromBase64String(cipherText);

            using (var aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (var msDecrypt = new MemoryStream(buffer))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        // Method to decompress and decrypt data
        public static string DecompressAndDecryptString(string encryptedText)
        {
            // Decrypt the encrypted string first
            string decryptedData = DecryptString(encryptedText);

            // Now decompress the decrypted data using GZipStream
            byte[] compressedData = Convert.FromBase64String(decryptedData);
            using (var ms = new MemoryStream(compressedData))
            using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
            using (var sr = new StreamReader(gzip, Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }
        #endregion

        #region Base64 Utility Methods
        // Method to convert a string into Base64 URL Safe encoding
        public static string ToBase64UrlSafe(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var base64 = Convert.ToBase64String(bytes);
            return base64.Replace('+', '-').Replace('/', '_').Replace("=", "");
        }

        // Method to convert Base64 URL Safe encoding back to string
        public static string FromBase64UrlSafe(string input)
        {
            // Replace URL-safe characters back to base64 characters
            string base64 = input.Replace('-', '+').Replace('_', '/');

            // Add padding if necessary
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            // Convert from Base64 to bytes
            var bytes = Convert.FromBase64String(base64);

            // Convert bytes to UTF-8 string
            return Encoding.UTF8.GetString(bytes);
        }
        #endregion
    }
}
