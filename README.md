# AspCoreNet.EncryptedConfig
This project implements a configuration provider, primarily intended for .NET Core applications, which supports loading AES encrypted Json files, as well as a desktop editor that allows you to create and edit these encrypted files.  Files are encrypted utilizing certificates installed on the local machine.

# How to use this library
You can add the Nuget package to your project, named **Hex2bit.AspNetCore.EncryptedConfig**, which can also be found here:
https://www.nuget.org/packages/Hex2bit.AspNetCore.EncryptedConfig/

In your application, you'd specify an encrypted config file to load through the ConfigurationBuilder using the new **AddEncryptedConfigFile** method.
```csharp
IConfigurationRoot config = new ConfigurationBuilder()  // config builder
  .AddEncryptedConfigFile(                              // add encrypted config
  new EncryptedConfigurationOptions                     // config options
  {
    ConfigFilename = "settings.ejson",                  // encrypted config file
    Thumbprint = "123456...abcdef"                      // certificate thumbprint
  })
  .Build();
```
### EncryptedConfigurationOptions
There are a number of ways to configure how to find and load the encrypted file config using the **EncryptedConfigurationOptions** class.  All of the options allow you to specify a specific value and/or an environment variable to look in to find the value (those ending in **Env**). Below are all the possible configuration options.
* **ConfigFilename** or **ConfigFilenameEnv**: filename of the configuration file to load
* **Thumbprint** or **ThumbprintEnv**: the thumbprint of the certificate to use to decrypte the configuration file
* **StoreLocation** or **StoreLocationEnv**: the certificate store location to search in for the certificate
* **StoreName** or **StoreNameEnv**: the certificate store name to search in for the certificate
* **ReloadOnChange** oe **ReloadOnChangeEnv**: if the configuration file should be reloaded if it is changed
* **Optional** or **OptionalEnv**: if this configuration file is optional

Note, environment configuration options will take precedence over the non-environment configuration settings.  This allows you to use the environment variables to specify configuration settings, but also provide a fallback value in the configuration in case the environment variable isn't found.  For example:
```csharp
ConfigFilenameEnv = "SettingsFile",       // environment variable "SettingsFile" is expected to have the config file location
ConfigFileName = "falback_settings.ejson" // fallback config file to load
```
