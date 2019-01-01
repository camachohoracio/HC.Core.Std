#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using HC.Core.Comunication;
using HC.Core.Io.Serialization;
//using IWshRuntimeLibrary;
using HC.Core.Exceptions;
using HC.Core.Helpers;
using HC.Core.Logging;
using NUnit.Framework;
using File = System.IO.File;

#endregion

namespace HC.Core.Io
{
    public class FileHelper
    {
        public static DateTime GetDateLastModified(
            string strFileName,
            bool blnUseService)
        {
            try
            {
                if (blnUseService)
                {
                    return (DateTime) ProviderEvents.InvokeOnRunMethodDistributedViaService(
                        typeof (FileHelper),
                        "GetDateLastModifiedLocal",
                        new List<object>
                        {
                            strFileName
                        });
                }
                return GetDateLastModifiedLocal(
                    strFileName);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new DateTime();
        }

        public static DateTime GetDateLastModifiedLocal(
            string strFileName)
        {
            try
            {
                if (!File.Exists(strFileName))
                {
                    return new DateTime();
                }
                return File.GetLastWriteTime(strFileName);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new DateTime();
        }

        public static bool Equals(FileInfo f1, FileInfo f2)
        {
            int file1byte;
            int file2byte;

            if (!f1.Name.Equals(f2.Name))
            {
                return false;
            }

            if (f1.Length != f2.Length)
            {
                return false;
            }

            DateTime lastWriteTimeUtc = f1.LastWriteTimeUtc;
            DateTime time2 = f2.LastWriteTimeUtc;
            double dblSpanSeconds = Math.Abs((lastWriteTimeUtc - time2).TotalSeconds);
            if (dblSpanSeconds < 1)
            {
                return true;
            }

            // Open the two files.
            FileStream fs1 = f1.OpenRead();
            FileStream fs2 = f2.OpenRead();

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            int intCounter = 0;
            do
            {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
                intCounter++;
                if (intCounter > 1000)
                {
                    break;
                }
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is 
            // equal to "file2byte" at this point only if the files are 
            // the same.
            return ((file1byte - file2byte) == 0);
        }

        public static void WriteListToFile<T>(
            List<T> list,
            string strFileName)
        {
            using(var sw = new StreamWriter(strFileName))
            {
                foreach (T variable in list)
                {
                    sw.WriteLine(variable);
                }
            }
        }

        public static void ExploreFile(string strFileName)
        {
            var strPath = Path.GetDirectoryName(strFileName);
            if (Exists(
                strFileName,
                false))
            {
                var exploreTest =
                    new ProcessStartInfo();
                exploreTest.FileName = "explorer.exe";
                exploreTest.Arguments = strPath;
                Process.Start(exploreTest);
            }
            else
            {
                Console.WriteLine("File not found: " + strFileName);
            }
        }

        public static bool CheckLockedFile(string strFileName)
        {
            FileStream inStream = null;
            var blnIsLocked = false;
            try
            {
                lock (Serializer.GetLockObject(strFileName))
                {
                    inStream = File.OpenRead(strFileName);
                }
            }
            catch
            {
                blnIsLocked = true;
            }
            finally
            {
                if (inStream != null)
                {
                    inStream.Close();
                }
            }
            return blnIsLocked;
        }

        public static string GetExtension(string strFileName)
        {
            var fi = new FileInfo(strFileName);
            return fi.Extension;
        }

        public static void KillProcess(
            string strProcessName)
        {
            KillProcess(
                "taskkill",
                " /im " + strProcessName,
                true);
        }

        public static void KillProcess(
            string strServerName,
            string strProcessName)
        {
            KillProcess(
                "taskkill",
                "/s " + strServerName + " /im " + strProcessName,
                true);
        }

        public static void KillProcess(
            string strCommand,
            string strArgs,
            bool blnWaitForExit)
        {
            var processStartInfo = new ProcessStartInfo(
                strCommand,
                strArgs);

            var proc = new Process();

            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;

            proc.StartInfo = processStartInfo;
            proc.Start();
            if (blnWaitForExit)
            {
                proc.WaitForExit();
            }

            Thread.Sleep(5000);

            var streamReader = proc.StandardOutput;
            var err = proc.StandardError;

            var tempLine = string.Empty;

            while (!streamReader.EndOfStream)
            {
                tempLine = streamReader.ReadLine().Trim().ToLower();
                if (tempLine.Contains("success"))
                {
                    PrintToScreen.WriteLine(tempLine);
                }
            }
        }

        //public static void CreateShortCut(
        //    string strExeFileName,
        //    string strIcoFileName,
        //    string strShortcutFileName)
        //{
        //    // delete shortcut if it exists
        //    if (Exists(
        //        strShortcutFileName,
        //        false))
        //    {
        //        Delete(
        //            strShortcutFileName,
        //            false);
        //    }

        //    // Create a new instance of WshShellClass
        //    var WshShell = new WshShell();

        //    // Create the shortcut
        //    IWshShortcut MyShortcut;

        //    // Choose the path for the shortcut
        //    MyShortcut =
        //        (IWshShortcut)WshShell.CreateShortcut(
        //            strShortcutFileName);

        //    // Where the shortcut should point to
        //    MyShortcut.TargetPath = strExeFileName;

        //    // Description for the shortcut
        //    MyShortcut.Description = string.Empty;

        //    // Location for the shortcut's icon
        //    MyShortcut.IconLocation = strIcoFileName;
        //    try
        //    {
        //        // Create the shortcut at the given path
        //        MyShortcut.Save();
        //    }
        //    catch
        //    {
        //        //Debugger.Break();
        //    }
        //}

        public static bool CheckFileExists(string strFileName)
        {
            //
            // Check if file exiss
            //
            if (Exists(
                strFileName,
                false))
            {
                //var strMessage = "File: " +
                //                 new FileInfo(strFileName).Name +
                //                 " already exists. Do you wish to backup existing file?";
                //var strCaption = "File Check";
                //var buttons = MessageBoxButtons.YesNo;
                //DialogResult result;
                //result = HC.Utils.MessageBoxWrapper.Question(strMessage, strCaption, buttons);
                //if (result == DialogResult.No)
                //{
                //    HC.Utils.MessageBoxWrapper.Information("The process has been cancelled.");
                //    return false;
                //}
                //else
                {
                    PutAsideFile(
                        strFileName);
                }
            }
            return true;
        }

        /// <summary>
        ///   Reads a text file and returns a string.
        /// </summary>
        /// <param name = "strFileName">
        ///   Text file name
        /// </param>
        /// <returns></returns>
        public static string ReadFile(string strFileName)
        {
            var sb = new StringBuilder();
            using (var sr = new StreamReader(strFileName))
            {
                var strLine = string.Empty;
                while ((strLine = sr.ReadLine()) != null)
                {
                    sb.AppendLine(strLine);
                }
                sr.Close();
            }
            return sb.ToString();
        }

        public static void PutAsideFile(
            string strFileName)
        {
            lock (Serializer.GetLockObject(strFileName))
            {
                File.Move(strFileName,
                          strFileName + "_tmp");
            }
        }

        public static bool IsNetworkPath(string strNetworkPath)
        {
            if (strNetworkPath[0].Equals(@"\".ToCharArray()[0]) &&
                strNetworkPath[1].Equals(@"\".ToCharArray()[0]))
            {
                return true;
            }
            return false;
        }

        public static bool CheckIsDatabaseFile(string strFileName)
        {
            var fi = new FileInfo(strFileName);
            if (fi.Extension.ToLower().Equals(".mdf") ||
                fi.Extension.ToLower().Equals(".ldf"))
            {
                return true;
            }
            return false;
        }

        public static string[] GetAllFilesStartingWith(
            string strPath,
            string strStartingWith)
        {
            var di = new DirectoryInfo(strPath);
            var fi = new FileInfo(strStartingWith);
            strStartingWith = fi.Name;
            var fiArray = di.GetFiles();
            var strFileList = new List<string>();
            for (var i = 0; i < fiArray.Length; i++)
            {
                if (fiArray[i].Name.StartsWith(strStartingWith))
                {
                    strFileList.Add(fiArray[i].FullName);
                }
            }
            return strFileList.ToArray();
        }

        public static string[] GetAllFilesContains(
            string strPath,
            string strContains)
        {
            var di = new DirectoryInfo(strPath);
            var fiArray = di.GetFiles();
            var strFileList = new List<string>();
            for (var i = 0; i < fiArray.Length; i++)
            {
                if (fiArray[i].Name.ToLower().Contains(strContains.ToLower()))
                {
                    strFileList.Add(fiArray[i].FullName);
                }
            }
            return strFileList.ToArray();
        }

        public static string GetDriveName(string path)
        {
            var pathLength = path.Length;
            var outPath = string.Empty;
            for (var i = 0; i < pathLength; i++)
            {
                var currentChar = path[i];
                outPath = outPath + currentChar;
                if (currentChar == @"\".ToCharArray()[0])
                {
                    return outPath;
                }
            }
            return null;
        }

        public static string GetDriveLetter(string strPath)
        {
            try
            {
                string strDrive = GetDriveName(strPath);
                if (string.IsNullOrEmpty(strDrive))
                {
                    strDrive = "c";
                }
                return strDrive.Replace(@":\", string.Empty);
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return "c";
        }

        public static string GetCurrentAssamblyRootPath()
        {
            return GetDriveName(
                GetCurrentAssemblyPath());
        }

        public static bool CopyDirectory(
            string strSourceDir,
            string strDestDir,
            bool blnReplaceFiles,
            bool blnUseService,
            List<string> dirsToAvoid = null)
        {
            if (blnUseService)
            {
                return (bool) ProviderEvents.InvokeOnRunMethodDistributedViaService(
                    typeof (FileHelper),
                    "CopyDirectoryLocal",
                    new List<object>
                    {
                        strSourceDir,
                        strDestDir,
                        blnReplaceFiles,
                        dirsToAvoid
                    });
            }
            return CopyDirectoryLocal(
                        strSourceDir,
                        strDestDir,
                        blnReplaceFiles,
                        dirsToAvoid);
        }

        public static bool CopyDirectoryLocal(
            string strSourceDir,
            string strDestDir,
            bool blnReplaceFiles,
            List<string> dirsToAvoid = null)
        {
            try
            {
                lock (Serializer.GetLockObject(strSourceDir))
                {
                    lock (Serializer.GetLockObject(strDestDir))
                    {
                        if (strDestDir[strDestDir.Length - 1] != Path.DirectorySeparatorChar)
                        {
                            strDestDir += Path.DirectorySeparatorChar;
                        }
                        if (!DirectoryHelper.Exists(
                            strDestDir,
                            false))
                        {
                            DirectoryHelper.CreateDirectory(strDestDir);
                        }
                        string[] files =
                            Directory.GetFileSystemEntries(strSourceDir);
                        foreach (string strFileName in files)
                        {
                            if (DirectoryHelper.Exists(
                                strFileName,
                                false))
                            {
                                bool blnContainsFalseDir = false;
                                if (dirsToAvoid != null)
                                {

                                    foreach (string strDirToAvoid in dirsToAvoid)
                                    {
                                        if(strFileName.EndsWith(strDirToAvoid))
                                        {
                                            blnContainsFalseDir = true;
                                            break;
                                        }
                                        if (strFileName.Contains(
                                            @"\" + strDirToAvoid + @"\"))
                                        {
                                            blnContainsFalseDir = true;
                                            break;
                                        }
                                    }
                                }
                                if (!blnContainsFalseDir)
                                {
                                    CopyDirectoryLocal(
                                        strFileName,
                                        strDestDir + Path.GetFileName(strFileName),
                                        blnReplaceFiles);
                                }
                            }
                                // Files in directory
                            else
                            {
                                try
                                {
                                    if (Exists(
                                        strDestDir + Path.GetFileName(strFileName),
                                        false) &&
                                        blnReplaceFiles)
                                    {
                                        Delete(
                                            strDestDir + Path.GetFileName(strFileName),
                                            false);
                                    }
                                    File.Copy(strFileName, strDestDir + Path.GetFileName(strFileName), true);
                                }
                                catch (HCException e2)
                                {
                                    ////lc.Write(e2);
                                    Console.WriteLine(e2);
                                }
                            }
                        }
                    }
                }
                return true;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public static string ProcessDirectory(string strDirectory)
        {
            var strSymbol =
                strDirectory[strDirectory.Length - 1].ToString();
            if (strSymbol.Equals(@"\"))
            {
                return strDirectory.Substring(0,
                                              strDirectory.Length - 1);
            }
            return strDirectory;
        }

        public static void RunExecutable(string strFileName,
                                         bool blnWaitForExit)
        {
            RunExecutable(
                strFileName,
                string.Empty,
                blnWaitForExit);
        }

        public static void RunExecutable(
            string strFileName,
            string strArguments,
            bool blnWaitForExit)
        {
            RunExecutable(
                strFileName,
                strArguments,
                blnWaitForExit,
                false);
        }

        public static int RunExecutable(
            string strFileName,
            string strArguments,
            bool blnWaitForExit,
            bool blnIsHidden)
        {
            try
            {
                //Process process = LoadProcess(
                //    strFileName,
                //    strArguments,
                //    blnWaitForExit,
                //    blnIsHidden);

                if (!Exists(
                    strFileName,
                    false))
                {
                    Console.WriteLine("File not found: " + strFileName);
                    return -1;
                }

                var psi = new ProcessStartInfo(strFileName);
                if (blnIsHidden)
                {
                    //psi.CreateNoWindow = false;
                    //psi.WindowStyle = ProcessWindowStyle.Minimized;
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    // *** Redirect the output ***
                    psi.RedirectStandardError = true;
                    psi.RedirectStandardOutput = true;
                }

                if (!blnWaitForExit)
                {
                    Process.Start(
                        strFileName,
                        strArguments);
                    return 0;
                }
                psi.FileName = strFileName;
                if (!strArguments.Equals(string.Empty))
                {
                    psi.Arguments = strArguments;
                }
                psi.UseShellExecute = false;

                var process = Process.Start(psi);
                if (blnWaitForExit)
                {
                    process.WaitForExit();
                }
                var exitCode = process.ExitCode;
                process.Close();
                return exitCode;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return -1;
        }

        public static Process LoadProcess(
            string strFileName,
            string strArguments,
            bool blnWaitForExit,
            bool blnIsHidden)
        {
            if (!Exists(
                strFileName,
                false))
            {
                Console.WriteLine("File not found: " + strFileName);
                return null;
            }
            var psi = new ProcessStartInfo(strFileName);

            if (blnIsHidden)
            {
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Minimized;
            }

            psi.FileName = strFileName;
            if (!strArguments.Equals(string.Empty))
            {
                psi.Arguments = strArguments;
            }

            psi.UseShellExecute = false;
            var process = Process.Start(psi);
            return process;
        }

        private static readonly object m_lockObj = new object();
        public static void ExecuteDosCommand(string strCommand)
        {
            lock (m_lockObj)
            {
                const string strFileName = @"c:\" +
                                           Constants.STR_BAT_FILE_NAME;
                if (Exists(
                    strFileName,
                    false))
                {
                    Delete(
                        strFileName,
                        false);
                }
                using (var sw = new StreamWriter(
                    new FileStream(strFileName,
                                              FileMode.Create,
                                              FileAccess.Write,
                                              FileShare.Write),
                    Encoding.GetEncoding("ISO-8859-1")))
                {
                    sw.WriteLine(strCommand);
                }
                RunExecutable(strFileName, true);
                Delete(
                    strFileName,
                    false);
            }
        }

        public static long CountNumberOfRows(string strFileName)
        {
            long longRowCounter = 0;
            using (var sr = new StreamReader(strFileName))
            {
                string strLine;
                while ((strLine = sr.ReadLine()) != null)
                {
                    longRowCounter++;
                }
                sr.Close();
            }
            return longRowCounter;
        }

        public static int CountNumberOfColumns(
            string strFileName,
            char chrDelimiter)
        {
            var intColumnCount = 0;

            Stream stream = new FileStream(strFileName,
                                           FileMode.Open,
                                           FileAccess.Read,
                                           FileShare.Read);

            using (var sr = new StreamReader(stream))
            {
                string strLine;
                while ((strLine = sr.ReadLine()) != null)
                {
                    intColumnCount = strLine.Split(chrDelimiter).Length;
                    break;
                }
                sr.Close();
            }
            return intColumnCount;
        }

        public static List<string> GetFileList(
            string strDirName,
            bool blnSubDirectories,
            bool blnUseService)
        {
            if(blnUseService)
            {

                return (List<string>) ProviderEvents.InvokeOnRunMethodDistributedViaService(
                    typeof (FileHelper),
                    "GetFileListLocal",
                    new List<object>
                        {
                            strDirName,
                            blnSubDirectories
                        });
            }
            return GetFileListLocal(
                strDirName,
                blnSubDirectories);
        }

        public static List<string> GetFileListLocal(
            string strDirName,
            bool blnSubDirectories)
        {
            try
            {
                SearchOption searchOption = blnSubDirectories
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;
                return GetFileList(strDirName, searchOption);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<string>();
        }

        public static List<string> GetFileList(string strDirName)
        {
            return GetFileList(strDirName, SearchOption.AllDirectories);
        }

        public static void DeleteAllFiles(string strDirName,
            bool blnInvludeSubDirs)
        {
            try
            {
                if(!DirectoryHelper.Exists(
                    strDirName,
                    false))
                {
                    return;
                }
                List<string> fileList = GetFileList(strDirName,
                    blnInvludeSubDirs ?
                    SearchOption.AllDirectories :
                    SearchOption.TopDirectoryOnly);

                foreach (string strFileName in fileList)
                {
                    try
                    {

                        Delete(
                            strFileName,
                            false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        Console.WriteLine(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static List<string> GetFileList(string strDirName,
            SearchOption searchOption)
        {
            try
            {
                if (!Directory.Exists(strDirName))
                {
                    return new List<string>();
                }
                return new List<string>(
                    Directory.GetFiles(strDirName, "*.*",
                                       searchOption));
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<string>();
        }

        public static string GetCurrentAssemblyPath()
        {
            return IoHelper.GetCurrentAssemblyPath();
        }

        public static string GetAssemblyName(Type type)
        {
            return GetAssemblyName(Assembly.GetAssembly(type));
        }

        public static string GetCallingAssemblyName()
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            return GetAssemblyName(callingAssembly);
        }

        public static string GetAssemblyFullFileName(Type type)
        {
            string strLocation = Assembly.GetAssembly(type).Location;
            if(string.IsNullOrEmpty(strLocation))
            {
                return type.Name;
            }
            // Get the registry config file name.
            var assemblyFileInfo = new FileInfo(strLocation);
            return assemblyFileInfo.FullName;
        }

        public static string GetExecutingAssemblyDir()
        {
            // Get the registry config file name.
            var assemblyFileInfo = new FileInfo(
                Assembly.GetExecutingAssembly().Location);
            return assemblyFileInfo.DirectoryName;
        }

        public static string GetAssemblyName(Assembly assembly)
        {
            // Get calling assembly name without file suffix.
            var assemblyName = new AssemblyName(assembly.FullName);
            var strAssemblyName = assemblyName.Name;
            return strAssemblyName;
        }

        public static string GetAssemblyFullName()
        {
            return IoHelper.GetAssemblyFullName();
        }

        public static string GetCurrentPath()
        {
            return IoHelper.GetCurrentPath();
        }

        public static void OpenFile(
            string strFileName,
            bool blnWaitForExit)
        {
            Process objProcess = null;
            try
            {
                objProcess = new Process();
                objProcess.StartInfo.FileName = strFileName;
                objProcess.StartInfo.WindowStyle =
                    ProcessWindowStyle.Normal;
                objProcess.Start();
                if (blnWaitForExit)
                {
                    objProcess.WaitForExit();
                }
                objProcess.Close();
            }
            catch (HCException e2)
            {
                var strMessage = "Could not start process ";
                ////m_lc.Write(strMessage);
                ////lc.Write(e2);
                Console.WriteLine(strMessage + strFileName + ".", e2);
            }
            finally
            {
                if (objProcess != null)
                {
                    objProcess.Close();
                }
            }
        }

        public static DataFileType GetDataFileType(string strFileName)
        {
            var fi = new FileInfo(strFileName);
            var strFileExtension = fi.Extension.ToLower();
            if (strFileExtension.Equals(".xls"))
            {
                return DataFileType.Excel;
            }
            else if (strFileExtension.Equals(".csv"))
            {
                return DataFileType.Csv;
            }
            else if (strFileExtension.Equals(".txt"))
            {
                return DataFileType.Txt;
            }
            else
            {
                throw new HCException("Error. Data file type not defined");
            }
        }

        public static double GetFleSizeMb(string strFileName)
        {
            if (!Exists(
                strFileName,
                false))
            {
                throw new HCException("File not found: " +
                                             strFileName);
            }
            var dblFileSize = new FileInfo(strFileName).Length / 1000000.0;
            return dblFileSize;
        }

        [Test]
        public static void TestIllegalPath()
        {
            string strFileName = CleanFileName(@"C:\HC\bin\Apps\\Gu:i");
            Console.WriteLine(strFileName);

        }

        public static string CleanFileName(string strFileName)
        {
            try
            {
                strFileName = StringHelper.ReplaceCaseInsensitive(
                    strFileName,
                    "con",
                    "coen_");
                strFileName = StringHelper.ReplaceCaseInsensitive(
                    strFileName,
                    "prn",
                    "pren_");
                strFileName = StringHelper.ReplaceCaseInsensitive(
                    strFileName,
                    "nul",
                    "nuel_");

                strFileName = StringHelper.ReplaceCaseInsensitive(
                    strFileName,
                    "aux",
                    "aauuxx");

                strFileName = strFileName
                    .Replace("&", string.Empty)
                    .Replace("|", string.Empty)
                    .Replace("*", string.Empty)
                    .Replace("?", string.Empty)
                    .Replace("<", string.Empty)
                    .Replace(">", string.Empty)
                    .Replace(";", string.Empty)
                    .Replace(",", string.Empty)
                    .Replace("=", string.Empty)
                    .Replace("*", string.Empty)
                    .Replace('\a'.ToString(), string.Empty)
                    .Replace('\b'.ToString(), string.Empty)
                    .Replace('\t'.ToString(), string.Empty)
                    .Replace('\n'.ToString(), string.Empty)
                    .Replace('\v'.ToString(), string.Empty)
                    .Replace('\f'.ToString(), string.Empty)
                    .Replace('\r'.ToString(), string.Empty);
                string[] toks = strFileName.Split(@"\".ToCharArray()[0]);
                string strName1 = toks[toks.Length - 1];
                string strDir = StringHelper.ReplaceEndsWith(strFileName, strName1);
                strFileName = strDir + strName1.Replace(":", string.Empty);

                return strFileName;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return strFileName;
        }

        public static bool Exists(
            string strFileName, 
            bool blnUseService = false)
        {
            try
            {
                if (blnUseService)
                {
                    return (bool) ProviderEvents.InvokeOnRunMethodDistributedViaService(
                        typeof(FileHelper),
                        "ExistsFileLocal",
                        new List<object>
                            {
                                strFileName
                            });
                }
                return ExistsFileLocal(
                    strFileName);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public static bool ExistsFileLocal(
            string strFileName)
        {
            try
            {
                if(string.IsNullOrEmpty(strFileName))
                {
                    return false;
                }

                if (NetworkHelper.IsADistWorkerConnected)
                {
                    if (IsNetworkPath(strFileName))
                    {
                        Logger.Log(
                            new HCException(
                                "Worker should not access data! [" +
                                strFileName + "]" +
                                Environment.StackTrace));
                    }
                }

                lock (Serializer.GetLockObject(strFileName))
                {
                    return File.Exists(strFileName);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public static bool Delete(
            string strFileName,
            bool blnUseService = false)
        {
            try
            {
                if (blnUseService)
                {
                    return (bool)ProviderEvents.InvokeOnRunMethodDistributedViaService(
                        typeof(FileHelper),
                        "DeleteLocal",
                        new List<object>
                            {
                                strFileName
                            });
                }
                return DeleteLocal(
                    strFileName);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public static bool DeleteLocal(
            string strFileName)
        {
            try
            {
                lock (Serializer.GetLockObject(strFileName))
                {
                    File.Delete(strFileName);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return false;
        }

        public static bool Move(
            string strOldFileName, 
            string strNewFileName, 
            bool blnUseService)
        {
            try
            {
                if(blnUseService)
                {
                    return (bool) ProviderEvents.InvokeOnRunMethodDistributedViaService(
                        typeof (FileHelper),
                        "MoveLocal",
                        new List<object>
                            {
                                strOldFileName,
                                strNewFileName
                            });
                }
                return MoveLocal(strOldFileName,
                          strNewFileName);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        private static bool MoveLocal(
            string strOldFileName, 
            string strNewFileName)
        {
            try
            {
                lock (Serializer.GetLockObject(strOldFileName))
                {
                    lock (Serializer.GetLockObject(strNewFileName))
                    {
                        File.Move(
                            strOldFileName,
                            strNewFileName);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public static string GetOnlyNameWithoutExtention(
            string strFileName)
        {
            var fi = new FileInfo(strFileName);
            return fi.Name.Replace(fi.Extension, string.Empty);
        }

        public static Dictionary<string, DateTime> GetMapFileNameToDate(
            List<string> fileNames, 
            bool blnUseService)
        {
            try
            {
                if (blnUseService)
                {
                    return (Dictionary<string, DateTime>) ProviderEvents.InvokeOnRunMethodDistributedViaService(
                        typeof (FileHelper),
                        "GetMapFileNameToDateLocal",
                        new List<object>
                        {
                            fileNames
                        });
                }
                return GetMapFileNameToDateLocal(fileNames);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new Dictionary<string, DateTime>();
        }

        public static Dictionary<string, DateTime> GetMapFileNameToDateLocal(
            List<string> fileNames)
        {
            try
            {
                var result = new Dictionary<string, DateTime>();
                for (int i = 0; i < fileNames.Count; i++)
                {
                    string strFileName = fileNames[i];
                    result[strFileName] =
                        GetDateLastModifiedLocal(
                            strFileName);
                }
                return result;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new Dictionary<string, DateTime>();
        }

        public static DateTime GetLastWriteTime(
            string strFileName, 
            bool blnUseService)
        {
            try
            {
                if (blnUseService)
                {
                    return (DateTime)ProviderEvents.InvokeOnRunMethodDistributedViaService(
                        typeof(FileHelper),
                        "GetLastWriteTimeLocal",
                        new List<object>
                        {
                            strFileName
                        });
                }
                return GetLastWriteTimeLocal(strFileName);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new DateTime();
        }

        public static DateTime GetLastWriteTimeLocal(
            string strFileName)
        {
            return File.GetLastWriteTime(strFileName);
        }
    }
}


