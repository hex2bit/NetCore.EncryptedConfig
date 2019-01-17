using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hex2bit.NetCore.EncryptedConfig.Editor
{
    public class EncryptedFileDetails : IEquatable<object>
    {
        // name of the encrypted file
        public string FileName { get; set; }
        // parent folder name
        public string ParentFolderName { get; set; }
        // second level parent folder name
        public string SecondParentFolderName { get; set; }
        // full path to the encrypted file
        public string FullPath { get; set; }
        // name of the visual studio project file (if found in the same folder), .csproj, .vbproj, or .fsproj files
        public string VisualStudioProjectFileName { get; set; }
        // the certificate store name
        public string CertificateStoreName { get; set; }
        // the certificate store location
        public string CertificateStoreLocation { get; set; }
        // the certificate thumbprint
        public string CertificateThumbprint { get; set; }

        public override bool Equals(object obj)
        {
            EncryptedFileDetails other;

            if (obj is EncryptedFileDetails)
            {
                other = (EncryptedFileDetails)obj;

                return string.Compare(FileName, other.FileName, true) == 0 
                    && string.Compare(ParentFolderName, other.ParentFolderName, true) == 0
                    && string.Compare(SecondParentFolderName, other.SecondParentFolderName, true) == 0
                    && string.Compare(FullPath, other.FullPath, true) == 0
                    && string.Compare(VisualStudioProjectFileName, other.VisualStudioProjectFileName, true) == 0
                    && string.Compare(CertificateStoreLocation, other.CertificateStoreLocation, true) == 0
                    && string.Compare(CertificateStoreName, other.CertificateStoreName, true) == 0
                    && string.Compare(CertificateThumbprint, other.CertificateThumbprint, true) == 0;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return (FileName ?? "").ToLower().GetHashCode()
                ^ (ParentFolderName ?? "").ToLower().GetHashCode()
                ^ (SecondParentFolderName ?? "").ToLower().GetHashCode()
                ^ (FullPath ?? "").ToLower().GetHashCode()
                ^ (VisualStudioProjectFileName ?? "").ToLower().GetHashCode()
                ^ (CertificateStoreLocation ?? "").ToLower().GetHashCode()
                ^ (CertificateStoreName ?? "").ToLower().GetHashCode()
                ^ (CertificateThumbprint ?? "").ToLower().GetHashCode();
        }
    }
}
