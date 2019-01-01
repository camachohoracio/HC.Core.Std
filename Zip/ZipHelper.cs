using System;
using System.IO;
using HC.Core.Helpers;
using HC.Core.Io;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace HC.Core.Zip
{
    public class ZipHelper
    {
        public static void ZipFolder(string sourceDir, string targetName)
        {
            // Simple sanity checks
            if (sourceDir.Length == 0)
            {
                PrintToScreen.WriteLine("Please specify a directory");
                return;
            }
            else
            {
                if (!DirectoryHelper.Exists(sourceDir))
                {
                    PrintToScreen.WriteLine(sourceDir, "Directory not found");
                    return;
                }
            }

            if (targetName.Length == 0)
            {
                PrintToScreen.WriteLine("No name specified");
                return;
            }

            string[] astrFileNames = Directory.GetFiles(sourceDir);
            ZipOutputStream strmZipOutputStream = new ZipOutputStream(File.Create(targetName));
            try
            {

                // Compression Level: 0 - 9
                // 0: no(Compression)
                // 9: maximum compression
                strmZipOutputStream.SetLevel(9);

                Byte[] abyBuffer = new byte[4096];

                foreach (string strFile in astrFileNames)
                {
                    ZipFile(strmZipOutputStream, strFile, abyBuffer);
                }
                strmZipOutputStream.Finish();
            }
            finally
            {
                strmZipOutputStream.Close();
            }
        }

        public static void ZipFile(
            string strFile,
            string strZipFileName)
        {
            ZipOutputStream strmZipOutputStream =
                new ZipOutputStream(File.Create(strZipFileName));
            try
            {

                // Compression Level: 0 - 9
                // 0: no(Compression)
                // 9: maximum compression
                strmZipOutputStream.SetLevel(9);
                Byte[] abyBuffer = new byte[4096];
                ZipFile(strmZipOutputStream, strFile, abyBuffer);
            }
            finally
            {
                strmZipOutputStream.Close();
            }
        }


        private static void ZipFile(
            ZipOutputStream strmZipOutputStream,
            string strFile,
            byte[] abyBuffer)
        {
            FileStream strmFile = File.OpenRead(strFile);
            try
            {
                FileInfo fi = new FileInfo(strFile);

                ZipEntry objZipEntry = new ZipEntry(fi.Name);

                objZipEntry.DateTime = DateTime.Now;
                objZipEntry.Size = strmFile.Length;

                strmZipOutputStream.PutNextEntry(objZipEntry);

                StreamUtils.Copy(strmFile, strmZipOutputStream, abyBuffer);
            }
            finally
            {
                strmFile.Close();
            }
        }

        public static void UnZipFile(
            string strZipFileName)
        {
            string strNewZipPath = new FileInfo(strZipFileName).DirectoryName;
            UnZipFile(strZipFileName,
                     strNewZipPath);
        }


        public static void UnZipFile(
            string strZipFileName,
            string strNewZipPath)
        {
            if (strZipFileName.Length == 0)
            {
                PrintToScreen.WriteLine("No name specified");
                return;
            }

            if (!DirectoryHelper.Exists(strNewZipPath))
            {
                DirectoryHelper.CreateDirectory(strNewZipPath);
            }

            ZipInputStream strmZipInputStream = new ZipInputStream(File.Open(
                strZipFileName,
                FileMode.Open));
            try
            {
                try
                {
                    ZipEntry zipEntry;
                    while ((zipEntry = strmZipInputStream.GetNextEntry()) != null)
                    {
                        string strFileName = Path.Combine(
                            strNewZipPath,
                            zipEntry.Name);

                        using (FileStream sw = new FileStream(strFileName, FileMode.Create))
                        {
                            int len = 0;
                            int bufferSize = 1024;
                            byte[] buffer = new byte[bufferSize];
                            while ((len = strmZipInputStream.Read(buffer, 0, bufferSize)) > 0)
                            {
                                sw.Write(buffer, 0, len);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    PrintToScreen.WriteLine(e.Message);
                }
            }
            finally
            {
                strmZipInputStream.Close();
            }

            //PrintToScreen.WriteLine("Operation complete");
        }
    }
}


