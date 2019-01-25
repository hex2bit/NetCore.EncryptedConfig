using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Hex2bit.NetCore.EncryptedConfig
{
    /// <summary>
    /// Configuration provider for the encrypted JSON files
    /// </summary>
    public class EncryptedConfigurationProvider : FileConfigurationProvider
    {
        // the certificate thumbprint to search file
        private readonly string thumbprint;
        // the certificate store to search in
        private readonly StoreName storeName;
        // the certificate location to search in
        private readonly StoreLocation storeLocation;

        // constructor
        public EncryptedConfigurationProvider(EncryptedConfigurationSource source, string thumbprint, StoreLocation storeLocation = StoreLocation.LocalMachine, StoreName storeName = StoreName.My)
            : base(source)
        {
            this.thumbprint = thumbprint;
            this.storeName = storeName;
            this.storeLocation = storeLocation;
        }

        // takes the stream of the encrypted json, decrypts it, and loads it into the configuration dictionary
        public override void Load(Stream stream)
        {
            // buffer to read from the stream
            byte[] buffer = new byte[8192];
            // bytes read
            int readSize;
            // decrypted string
            string decryptedString;
            // encryption service
            EncryptionService encryptionService;

            // use encryption service to decrypte the stream data
            encryptionService = new EncryptionService(thumbprint, storeLocation, storeName);

            using (MemoryStream ms = new MemoryStream())
            {
                while ((readSize = stream.Read(buffer, 0, buffer.Length)) > 0)
                    ms.Write(buffer, 0, readSize);

                decryptedString = encryptionService.Decrypt(ms.ToArray());
            }
            
            // use JSON file parser (copied from Microsoft's code) to parse the JSON config file into a dictionary
            Data = JsonConfigurationFileParser.Parse(new MemoryStream(Encoding.UTF8.GetBytes(decryptedString)));
        }
    }
}
