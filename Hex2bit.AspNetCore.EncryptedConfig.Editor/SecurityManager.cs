using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Hex2bit.AspNetCore.EncryptedConfig.Editor
{
    public class SecurityManager
    {
        public string[] GetCertificates(string storeName, string storeLocation)
        {
            List<string> certList = new List<string>();
            string displayName;

            using (var store = new X509Store((StoreName)Enum.Parse(typeof(StoreName), storeName),
                (StoreLocation)Enum.Parse(typeof(StoreLocation), storeLocation)))
            {
                store.Open(OpenFlags.ReadOnly);
                foreach (var cert in store.Certificates)
                {
                    // try accessing private key
                    try
                    {
                        // verify access to private key for decryption
                        if (cert.HasPrivateKey)
                        {
                            // verify encryption/decryption works
                            using (EncryptionService encryptionService = GetNewEncryptionService(storeName, storeLocation, cert.Thumbprint))
                            {
                                encryptionService.Decrypt(encryptionService.Encrypt("test"));
                            }

                            displayName = !string.IsNullOrWhiteSpace(cert.FriendlyName) ? cert.FriendlyName : cert.Subject;
                            if (displayName.Length > 80)
                            {
                                displayName = displayName.Substring(0, 80) + "...";
                            }
                            certList.Add(cert.Thumbprint + " (" + displayName + ")");
                        }
                    }
                    catch (Exception) { }
                }
            }

            return certList.ToArray();
        }

        public EncryptionService GetNewEncryptionService(string storeName, string storeLocation, string thumbprint)
        {
            return new EncryptionService(thumbprint,
                (StoreLocation)Enum.Parse(typeof(StoreLocation), storeLocation),
                (StoreName)Enum.Parse(typeof(StoreName), storeName));
        }
    }
}
