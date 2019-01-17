using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Hex2bit.NetCore.EncryptedConfig
{
    /// <summary>
    /// File configuration source for encrypted JSON files.
    /// </summary>
    public class EncryptedConfigurationSource : FileConfigurationSource
    {
        // certificate thumbprint
        public string Thumbprint { private get; set; }
        // store name to look for the certificate
        public StoreName StoreName { private get; set; }
        // store location to look for the certificate
        public StoreLocation StoreLocation { private get; set; }

        // builds teh configruation provider for the eyncrypted JSON files
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            // if the FileProvider is null, default to using the builder's file provider
            FileProvider = FileProvider ?? builder.GetFileProvider();

            // return a new instance of this EncryptedConfigurationProvider
            return new EncryptedConfigurationProvider(this, Thumbprint, StoreLocation, StoreName);
        }
    }
}
