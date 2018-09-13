using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Hex2bit.AspNetCore.EncryptedConfig
{
    /// <summary>
    /// Defines all the configuration options available for the AddEncryptedConfigFile extension method.  All the properties have
    /// a direct setting, as well as an *Env setting.  If you want to use environment variables to read the settins from, use the
    /// *Env properties to define when environment variables to look in for the setting value.
    /// 
    /// Note: the *Env property will override the direct property, so if you had both ConfigFilename and ConfigFileNameEnv set, it
    /// will pull the value from the environment variable specified in ConfigFilenameEnv.  If the environment variable isn't found,
    /// the AddEncryptedConfigFile method will fall back to using the direct property ConfigFilename.  This allows you use selectively
    /// use environment variables and fallback to hard-coded configruation values if the environment variables haven't been set.
    /// </summary>
    public class EncryptedConfigurationOptions
    {
        // filename of the configuration file to load
        public string ConfigFilename { get; set; }
        // the environment variable to find the configuration file path in to load
        public string ConfigFilenameEnv { get; set; }
        // the thumbprint of the certificate to use to decrypte the configuration file
        public string Thumbprint { get; set; }
        // the environment variable to find the thumbprint value in
        public string ThumbprintEnv { get; set; }
        // the certificate store location to search in for the certificate
        public StoreLocation StoreLocation { get; set; }
        // the environemnt variable to find the store location in
        public string StoreLocationEnv { get; set; }
        // the certificate store name to search in for the certificate
        public StoreName StoreName { get; set; }
        // the environment variable to find the store name in
        public string StoreNameEnv { get; set; }
        // if the configuration file should be reloaded if it is changed
        public bool ReloadOnChange { get; set; }
        // the environment variable with the reload on change setting
        public string ReloadOnChangeEnv { get; set; }
        // if this configuration file is optional
        public bool Optional { get; set; }
        // the environment variable with the Optional setting value
        public string OptionalEnv { get; set; }

        public EncryptedConfigurationOptions()
        {
            // defaults for where to look for certs
            this.StoreLocation = StoreLocation.LocalMachine;
            this.StoreName = StoreName.My;
        }
    }
}
