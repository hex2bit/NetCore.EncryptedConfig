using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Hex2bit.NetCore.EncryptedConfig
{
    /// <summary>
    /// Helper extension method to add the encrypted JSON configuration file into the .NET Core ConfigurationBuilder pipeline.  This
    /// class uses the EncryptedConfigurationOptions class to help defined the settings, so reviews that class for the options available.
    /// </summary>
    public static class EncryptedConfigurationExtensions
    {
        public static IConfigurationBuilder AddEncryptedConfigFile(this IConfigurationBuilder builder, EncryptedConfigurationOptions options)
        {
            // the file provider
            IFileProvider provider = null;

            // grab the config filename as we'll use this in a few spots
            string configFilename = GetEnvironmentVariable(options.ConfigFilenameEnv) != null
                    ? Environment.GetEnvironmentVariable(options.ConfigFilenameEnv)
                    : options.ConfigFilename;

            // use own file provider if full path is given for config filename, else it will be left null and will use the builder's
            // file provider later when the configuration is being built
            if (Path.IsPathRooted(configFilename))
            {
                provider = new PhysicalFileProvider(Path.GetDirectoryName(configFilename));
            }

            // define the configuration source
            EncryptedConfigurationSource source = new EncryptedConfigurationSource
            {
                FileProvider = provider,
                Optional = GetEnvironmentVariable(options.OptionalEnv) != null
                    ? Boolean.Parse(Environment.GetEnvironmentVariable(options.OptionalEnv))
                    : options.Optional,
                Path = configFilename,
                ReloadOnChange = GetEnvironmentVariable(options.ReloadOnChangeEnv) != null
                    ? Boolean.Parse(Environment.GetEnvironmentVariable(options.ReloadOnChangeEnv))
                    : options.ReloadOnChange,
                Thumbprint = GetEnvironmentVariable(options.ThumbprintEnv) != null
                    ? Environment.GetEnvironmentVariable(options.ThumbprintEnv)
                    : options.Thumbprint,
                StoreLocation = GetEnvironmentVariable(options.StoreLocationEnv) != null
                    ? (StoreLocation)Enum.Parse(typeof(StoreLocation), Environment.GetEnvironmentVariable(options.StoreLocationEnv))
                    : options.StoreLocation
                    ,
                StoreName = GetEnvironmentVariable(options.StoreNameEnv) != null
                    ? (StoreName)Enum.Parse(typeof(StoreName), Environment.GetEnvironmentVariable(options.StoreNameEnv))
                    : options.StoreName
            };

            builder.Add(source);
            return builder;
        }

        // helper to check if an environment variable exists and return it's value if found, else null is returned
        private static string GetEnvironmentVariable(string variable)
        {
            try
            {
                if (variable != null)
                {
                    return Environment.GetEnvironmentVariable(variable);
                }
            }
            catch (Exception) { }

            return null;
        }
    }
}
