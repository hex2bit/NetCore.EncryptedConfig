using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;

namespace Hex2bit.AspNetCore.EncryptedConfig.Editor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string OutputFile { get; set; }
        public static string StoreLocation { get; set; }
        public static string StoreName { get; set; }
        public static string Thumbprint { get; set; }
        public static string InputFile { get; set; }
        public static bool Piped { get; set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ParseInputs(e);

            ProcessInputs();
        }

        private static void ParseInputs(StartupEventArgs e)
        {
            // if run from a ClickOnce published app
            if (AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null
                && AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData != null)
            {
                if (new FileInfo(AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData[0]).Exists)
                {
                    OutputFile = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData[0].ToLower();
                }
            }
            // if run from traditional file association or called with just a single parameter (assumed to be the file to open)
            else if (e.Args.Length == 1)
            {
                OutputFile = e.Args[0].ToLower();
            }
            else
            {
                // look for command line arguments
                for (int i = 0; i < e.Args.Length; ++i)
                {
                    if (e.Args[i].ToLower() == "-p")
                    {
                        Piped = true;
                    }
                    else if (e.Args.Length > i + 1)
                    {
                        if (e.Args[i].ToLower() == "-t")
                        {
                            Thumbprint = e.Args[++i].ToLower();
                        }
                        else if (e.Args[i].ToLower() == "-n")
                        {
                            StoreName = e.Args[++i].ToLower();
                        }
                        else if (e.Args[i].ToLower() == "-l")
                        {
                            StoreLocation = e.Args[++i].ToLower();
                        }
                        else if (e.Args[i].ToLower() == "-o")
                        {
                            OutputFile = e.Args[++i].ToLower();
                        }
                        else if (e.Args[i].ToLower() == "-i")
                        {
                            InputFile = e.Args[++i].ToLower();
                        }
                        // else unknown parameter
                    }
                    // else no more parameters to process
                }
            }
        }

        private static void ProcessInputs()
        {
            bool error = false;

            // if a source is provided, this should just encrypt the content without an app window and exit
            if (Piped || InputFile != null)
            {
                // check for required parameters
                if (string.IsNullOrWhiteSpace(Thumbprint))
                {
                    Console.WriteLine("When providing an input to encrypt, you must provide a thumbprint (using the -t or -thumbprint parameter)");
                    error = true;
                }
                if (string.IsNullOrWhiteSpace(OutputFile))
                {
                    Console.WriteLine("When providing an input to encrypt, you must provide the name of the output file (using the -o or -output or -outputfile parameter");
                    error = true;
                }

                // encrypt the input if there's no error
                if (!error)
                {
                    error = EncryptFile();
                }

                if (error)
                {
                    // bad inputs, return non-success exit code
                    Environment.ExitCode = 1;
                }
                System.Windows.Application.Current.Shutdown();
            }
            else
            {
                // show window
                MainWindow window = new MainWindow();
                Application.Current.MainWindow = window;
                window.Show();
            }
        }

        private static bool EncryptFile()
        {
            bool error = false;
            FileStream outputFile = null;
            StreamReader inputStream = null;

            // try opening the output file for writing
            try
            {
                using (EncryptionService encryptionService = new EncryptionService(Thumbprint, (StoreLocation)Enum.Parse(typeof(StoreLocation), StoreLocation, true), (StoreName)Enum.Parse(typeof(StoreName), StoreName, true)))
                {
                    if (!File.Exists(InputFile))
                    {
                        Console.WriteLine("The input file [" + InputFile + "] cannot be found.");
                        error = true;
                    }
                    else
                    {
                        if (Piped)
                        {
                            inputStream = new StreamReader(Console.OpenStandardInput());
                        }
                        else // input file
                        {
                            inputStream = new StreamReader(File.OpenRead(InputFile));
                        }

                        outputFile = File.OpenWrite(OutputFile);
                        if (!outputFile.CanWrite)
                        {
                            Console.WriteLine("Cannot write to output file [" + OutputFile + "].");
                            error = true;
                        }
                        else
                        {
                            Console.WriteLine("Encrypted from [" + (Piped ? "Piped Input" : InputFile) + "] to [" + OutputFile + "]");
                            byte[] bytes = encryptionService.Encrypt(inputStream.ReadToEnd());
                            outputFile.Write(bytes, 0, bytes.Length);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error: " + ex.ToString());
                error = true;
            }
            finally
            {
                try { if (inputStream != null) inputStream.Close(); } catch (Exception) { }
                try { if (outputFile != null) outputFile.Close(); } catch (Exception) { }
            }
            
            return error;
        }
    }
}
