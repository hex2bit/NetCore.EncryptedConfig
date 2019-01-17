using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hex2bit.NetCore.EncryptedConfig.Editor
{
    public class SettingsManager
    {
        // list of successfully opened files and options used
        List<EncryptedFileDetails> KnownFiles = new List<EncryptedFileDetails>();

        public SettingsManager()
        {
            Properties.Settings.Default.Upgrade();
            string knownFilesJson = Properties.Settings.Default.KnownFiles;
            if (!string.IsNullOrWhiteSpace(knownFilesJson))
            {
                KnownFiles = JsonConvert.DeserializeObject<List<EncryptedFileDetails>>(knownFilesJson);
            }
        }

        public void UpdateSettings(string fullPath, string storeLocation, string storeName, string thumbprint)
        {
            EncryptedFileDetails file = null;
            EncryptedFileDetails existingFile = KnownFiles.FirstOrDefault(x => string.Compare(x.FullPath, fullPath, true) == 0);

            // check if file is already known
            if (existingFile != null)
            {
                // check for updates
                file = GetFileSystemDetails(fullPath);
                file.CertificateStoreLocation = storeLocation;
                file.CertificateStoreName = storeName;
                file.CertificateThumbprint = thumbprint;

                if (!file.Equals(existingFile))
                {
                    existingFile.FileName = file.FileName;
                    existingFile.FullPath = file.FullPath;
                    existingFile.ParentFolderName = file.ParentFolderName;
                    existingFile.SecondParentFolderName = file.SecondParentFolderName;
                    existingFile.VisualStudioProjectFileName = file.VisualStudioProjectFileName;
                    existingFile.CertificateStoreLocation = storeLocation;
                    existingFile.CertificateStoreName = storeName;
                    existingFile.CertificateThumbprint = thumbprint;

                    // save changes
                    Properties.Settings.Default.KnownFiles = JsonConvert.SerializeObject(KnownFiles);
                    Properties.Settings.Default.Save();
                }
            }
            else
            {
                // add new file to list
                file = GetFileSystemDetails(fullPath);
                file.CertificateStoreLocation = storeLocation;
                file.CertificateStoreName = storeName;
                file.CertificateThumbprint = thumbprint;

                if (file != null)
                {
                    KnownFiles.Add(file);

                    // save changes
                    Properties.Settings.Default.KnownFiles = JsonConvert.SerializeObject(KnownFiles);
                    Properties.Settings.Default.Save();
                }
            }
        }

        public EncryptedFileDetails FindBestMatch(string fullPath)
        {
            // get file details
            EncryptedFileDetails file = GetFileSystemDetails(fullPath);

            // find best match
            return KnownFiles
                .Where( // select any with matching properties
                    x => string.Compare(x.FileName, file.FileName, true) == 0
                    || string.Compare(x.FullPath, file.FullPath, true) == 0 
                    || string.Compare(x.ParentFolderName, file.ParentFolderName, true) == 0
                    || string.Compare(x.SecondParentFolderName, file.SecondParentFolderName, true) == 0
                    || string.Compare(x.VisualStudioProjectFileName, file.VisualStudioProjectFileName, true) == 0)
                .Select( // select a new object with a match score and the encrypted file details
                    x => new
                    {
                        score = string.Compare(x.FullPath, file.FullPath, true) == 0 ? 100 : 0 // exact file ranks very high
                                + string.Compare(x.FileName, file.FileName, true) == 0 ? 40 : 0 // file name weighted higher than folder and project details
                                + string.Compare(x.ParentFolderName, file.ParentFolderName, true) == 0 ? 10 :0
                                + string.Compare(x.SecondParentFolderName, file.SecondParentFolderName, true) == 0 ? 5 : 0
                                + string.Compare(x.VisualStudioProjectFileName, file.VisualStudioProjectFileName, true) == 0 ? 20 : 0, // this best defines the specific project if set
                        file = x
                    })
                 .OrderByDescending(x => x.score) // order by the calculated score se we can grab the highest
                 .Select(x => x.file) // grab the encrypted file details now that we no longer need the score to sore by
                 .FirstOrDefault(); // select the first result or default (null) if there are no results
        }

        private EncryptedFileDetails GetFileSystemDetails(string fullPath)
        {
            try
            {
                EncryptedFileDetails fileDetails = new EncryptedFileDetails();
                fileDetails.FullPath = fullPath;
                fileDetails.FileName = new FileInfo(fullPath).Name;

                // check for visual studio project file in given folder
                fileDetails.VisualStudioProjectFileName = FindVisualStudioProject(new FileInfo(fullPath).Directory.FullName);

                try
                {
                    // get parent
                    fileDetails.ParentFolderName = Directory.GetParent(fullPath).Name;

                    // check for visual studio project file in first parent foler if it wasn't found earlier
                    if (fileDetails.VisualStudioProjectFileName == null)
                    {
                        fileDetails.VisualStudioProjectFileName = FindVisualStudioProject(new FileInfo(fullPath).Directory.Parent.FullName);
                        if (fileDetails.VisualStudioProjectFileName != null)
                        {
                            fileDetails.VisualStudioProjectFileName = "..\\" + fileDetails.VisualStudioProjectFileName;
                        }
                    }

                    try
                    {
                        // get second parent
                        fileDetails.SecondParentFolderName = Directory.GetParent(fullPath).Parent.Name;

                        // check for visual studio project file in second parent folder if it wasn't found earlier
                        if (fileDetails.VisualStudioProjectFileName == null)
                        {
                            fileDetails.VisualStudioProjectFileName = FindVisualStudioProject(new FileInfo(fullPath).Directory.Parent.Parent.FullName);
                            if (fileDetails.VisualStudioProjectFileName != null)
                            {
                                fileDetails.VisualStudioProjectFileName = "..\\..\\" + fileDetails.VisualStudioProjectFileName;
                            }
                        }
                    }
                    catch (Exception) { } // no worries if there's no second parent
                }
                catch (Exception) { } // no worries if there's no parent

                // return file details
                return fileDetails;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string FindVisualStudioProject(string path)
        {
            try
            {
                DirectoryInfo folder = new DirectoryInfo(path);
                foreach (FileInfo file in folder.EnumerateFiles())
                {
                    if (file.Extension == ".csproj" // C#
                        || file.Extension == ".vbproj") // VB
                    {
                        return file.Name;
                    }
                }
            }
            catch (Exception) { } // bad path, do nothing

            return null;
        }
    }
}
