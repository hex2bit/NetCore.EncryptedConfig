# AspCoreNet.EncryptedConfig
This project implements a configuration provider, primarily intended for .NET Core applications, which supports loading AES encrypted Json files, as well as a desktop editor that allows you to create and edit these encrypted files.  Files are encrypted utilizing certificates installed on the local machine so the configuration files are portable to any machine you can move the certs to.

I wrote this library because ASP.NET Core doesn't have a good option for protecting your configuration outside of Azure.  ASP.NET Core Data Protection isn't really intended for long term configuration storage and portability.  This can also replace the use of the "Manage User Secrets" feature in Visual Studio.

# How to use this library
You can add the Nuget package to your project, named **Hex2bit.AspNetCore.EncryptedConfig** (currently pre-release), which can also be found here:
https://www.nuget.org/packages/Hex2bit.NetCore.EncryptedConfig/

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
* **ReloadOnChange** or **ReloadOnChangeEnv**: if the configuration file should be reloaded if it is changed
* **Optional** or **OptionalEnv**: if this configuration file is optional

Note, environment configuration options will take precedence over the non-environment configuration settings.  This allows you to use the environment variables to specify configuration settings, but also provide a fallback value in the configuration in case the environment variable isn't found.  For example:
```csharp
ConfigFilenameEnv = "SettingsFile",       // environment variable "SettingsFile" is expected to have the config file location
ConfigFileName = "falback_settings.ejson" // fallback config file to load
```

# Desktop Config File Editor
Under the **Releases** of this GitHub project you will find the latest installer for the desktop editor.
https://github.com/hex2bit/NetCore.EncryptedConfig/releases

The editor is pretty basic but allows you to edit files that get saved using AES 256 encryption, utilizing certificates installed on your machine.  Files with the extensions of **.ejson** and **.ejs** are mapped to the program, so it is recommended to save your files with one of those two extensions.  You can even edit those files in Visual Studio just by double clicking on them.

The editor will also remember the certificate that was last successfully used to edit a file, and tries to be smart about tracking that.  After you edited a file once, you can usually edit it again, even if your project folder moves due to branching/etc., without having to specify the certificate a second time.

# Work In Progress
This project is still a work in progress and I have yet to use it in any kind of production level projects, so expect some changes and bugs.  The Nuget package is currently published as pre-release to limit its exposure, which I know can cause problems with some build tools.
