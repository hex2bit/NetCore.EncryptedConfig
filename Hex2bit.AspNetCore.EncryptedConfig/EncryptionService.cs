using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Hex2bit.AspNetCore.EncryptedConfig
{
    public class EncryptionService : IDisposable
    {
        // crypto provider
        private RSA rsaProvider;

        public EncryptionService(string certThumbprint, StoreLocation storeLocation, StoreName storeName)
        {
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);
                foreach (var cert in store.Certificates)
                {
                    if (cert.Thumbprint.Equals(certThumbprint, StringComparison.InvariantCultureIgnoreCase))
                    {
                        rsaProvider = cert.GetRSAPrivateKey();
                        if (rsaProvider == null)
                        {
                            throw new Exception("Certificate associated with the provided Thumbprint [" + certThumbprint + "] does not contain a private key");
                        }
                        return;
                    }
                }
            }

            throw new Exception("Certificate not found with Thumbprint: " + certThumbprint + ", Store Name: " + storeName + ", and Store Location: " + storeLocation);
        }

        public byte[] Encrypt(string text)
        {
            // make sure appropriate parameters were passed
            if (string.IsNullOrEmpty(text))
            {
                throw new InvalidDataException("must pass in a non-empty password and text");
            }

            // generate random key
            RNGCryptoServiceProvider rngcp = new RNGCryptoServiceProvider();
            byte[] random = new byte[32];
            rngcp.GetBytes(random);
            DeriveBytes rgb = new Rfc2898DeriveBytes(Encoding.UTF8.GetString(random), 32, 10000);
            SymmetricAlgorithm algorithm = new AesManaged();
            byte[] key = rgb.GetBytes(algorithm.KeySize >> 3);
            byte[] iv = rgb.GetBytes(algorithm.BlockSize >> 3);

            // encrypt the key using asymetric key
            byte[] encryptedKey = rsaProvider.Encrypt(key.Concat(iv).ToArray(), RSAEncryptionPadding.Pkcs1);

            // encrypte data using symetric key
            byte[] encryptedData = Encrypt(text, key, iv);

            // grab sizes as bytes so we can concatenate everything together
            byte[] keySize = BitConverter.GetBytes(encryptedKey.Length);
            byte[] dataSize = BitConverter.GetBytes(encryptedData.Length);

            // concat everythig together, this data will be signed
            byte[] finalData = keySize.Concat(dataSize).Concat(encryptedKey).Concat(encryptedData).ToArray();

            // calc signature for the data
            byte[] signature = rsaProvider.SignData(finalData, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);

            // return the encrypted data followed by the signature
            return finalData.Concat(signature).ToArray();
        }

        public string Decrypt(byte[] encryptedBytes)
        {
            if (encryptedBytes == null || encryptedBytes.Length < (BitConverter.GetBytes(0).Length * 2 + 1))
            {
                throw new InvalidDataException("Encrypted data is not large enough");
            }

            // grab the sizes to the key and data
            int keySize = BitConverter.ToInt32(encryptedBytes, 0);
            int dataSize = BitConverter.ToInt32(encryptedBytes, BitConverter.GetBytes(0).Length);

            // check data size
            if (encryptedBytes.Length < keySize + dataSize + 1)
            {
                throw new InvalidDataException("Encrypted data is not large enough");
            }

            // get the separate pieces of data
            byte[] encryptedKey = encryptedBytes.Skip(BitConverter.GetBytes(0).Length * 2).Take(keySize).ToArray();
            byte[] encryptedData = encryptedBytes.Skip(BitConverter.GetBytes(0).Length * 2 + keySize).Take(dataSize).ToArray();
            byte[] finalData = encryptedBytes.Take(BitConverter.GetBytes(0).Length * 2 + keySize + dataSize).ToArray();
            byte[] signature = encryptedBytes.Skip(BitConverter.GetBytes(0).Length * 2 + keySize + dataSize).Take(encryptedBytes.Length - keySize - dataSize).ToArray();

            // check the signature of the encrypted data
            if (!rsaProvider.VerifyData(finalData, signature, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1))
            {
                throw new InvalidDataException("Signature verification failed");
            }

            // decrypt the key and extra the key and iv parts
            SymmetricAlgorithm algorithm = new AesManaged();
            byte[] symetricKey = rsaProvider.Decrypt(encryptedKey, RSAEncryptionPadding.Pkcs1);
            byte[] key = symetricKey.Take(algorithm.KeySize >> 3).ToArray();
            byte[] iv = symetricKey.Skip(key.Length).Take(algorithm.BlockSize >> 3).ToArray();

            // decrypt and return the result
            return Decrypt(encryptedData, key, iv);
        }

        private byte[] Encrypt(string text, byte[] key, byte[] iv)
        {
            SymmetricAlgorithm algorithm = new AesManaged();

            using (MemoryStream encryptedStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(encryptedStream, algorithm.CreateEncryptor(key, iv), CryptoStreamMode.Write))
                using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                    streamWriter.Write(text);

                return encryptedStream.ToArray();
            }
        }

        private string Decrypt(byte[] encryptedBytes, byte[] key, byte[] iv)
        {
            SymmetricAlgorithm algorithm = new AesManaged();

            using (MemoryStream encryptedStream = new MemoryStream(encryptedBytes))
            using (CryptoStream cryptoStream = new CryptoStream(encryptedStream, algorithm.CreateDecryptor(key, iv), CryptoStreamMode.Read))
            using (StreamReader streamReader = new StreamReader(cryptoStream))
                return streamReader.ReadToEnd();
        }

        public void Dispose()
        {
            rsaProvider.Dispose();
        }
    }
}