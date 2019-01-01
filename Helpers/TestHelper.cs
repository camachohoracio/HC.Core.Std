#region

using System.IO;
using HC.Core.Io;

#endregion

namespace HC.Core.Helpers
{
    public static class TestHelper
    {
        public static void CopyFiles(
            string strSourcePath,
            string strDestinationPath)
        {
            var fi = new FileInfo(strDestinationPath);
            if (!DirectoryHelper.Exists(fi.DirectoryName))
            {
                DirectoryHelper.CreateDirectory(fi.DirectoryName);
            }
            FileHelper.CopyDirectoryLocal(
                strSourcePath,
                strDestinationPath,
                false);
        }
    }
}


