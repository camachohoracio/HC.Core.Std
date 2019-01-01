#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Management;
using HC.Core.Comunication;
using HC.Core.Exceptions;
using HC.Core.Io.Serialization;
using HC.Core.Logging;

#endregion

namespace HC.Core.Io
{
    public static class DirectoryHelper
    {
        public static string CombineDirectory(
            string strPath1,
            string strPath2,
            string strPath3)
        {
            var strPath = Path.Combine(
                strPath1,
                strPath2);
            strPath = Path.Combine(
                strPath,
                strPath3);
            return strPath;
        }

        public static string CombineDirectory(List<string> dirList)
        {
            var strPath = dirList[0];
            for (var i = 1; i < dirList.Count; i++)
            {
                strPath = Path.Combine(
                    strPath,
                    dirList[i]);
            }
            return strPath;
        }

        public static string GetOneLevelUpDirectory(
            string strPath)
        {
            var i = strPath.Length - 1;
            for (; i >= 0; i--)
            {
                if (strPath[i].Equals(@"\".ToCharArray()[0]))
                {
                    break;
                }
            }
            return strPath.Substring(0, i);
        }

        /// <summary>
        ///   Universal naming convention. Specifies a common syntax 
        ///   to describe the location of a network resource
        /// </summary>
        /// <returns></returns>
        //private static string GetUncPath(string strFileName)
        //{
        //    var fileInf = new FileInfo(strFileName);
        //    var sPath = new StringBuilder();
        //    string sNetLtr;
        //    var sLtr = fileInf.FullName.Substring(0, 2);
        //    var query = new SelectQuery(
        //        "select name, ProviderName from win32_logicaldisk where drivetype=4");
        //    var searcher = new ManagementObjectSearcher(query);

        //    foreach (ManagementObject mo in searcher.Get())
        //    {
        //        sNetLtr = Convert.ToString(mo["name"]);
        //        if (sNetLtr == sLtr)
        //        {
        //            sPath.AppendFormat("{0}{1}", mo["ProviderName"], fileInf.DirectoryName.Substring(2));
        //        }
        //    }
        //    return sPath.ToString();
        //}

        public static void CheckDirectory(string name)
        {
            lock (Serializer.GetLockObject(name))
            {
                var fi = new FileInfo(name);
                if (!Directory.Exists(fi.DirectoryName))
                {
                    Directory.CreateDirectory(fi.DirectoryName);
                }
            }
        }

        public static bool IsANetWorkDir(string strCurrLoc)
        {
            try
            {
                if (string.IsNullOrEmpty(strCurrLoc))
                {
                    return false;
                }
                return strCurrLoc.StartsWith(@"\\");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            return false;
        }

        public static string RemoveNetWorkDir(string strCurrLoc)
        {
            try
            {
                strCurrLoc = strCurrLoc.Replace(@"\\", string.Empty);
                var toks = strCurrLoc.Split(@"\".ToCharArray()[0]).ToList();
                toks.RemoveAt(0);
                strCurrLoc = string.Join(@"\", toks).Trim();
                strCurrLoc = strCurrLoc.Replace("$", ":");
                return strCurrLoc;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return string.Empty;
        }

        public static List<string> GetSubdirs(string strPath)
        {
            lock (Serializer.GetLockObject(strPath))
            {
                string[] dir = Directory.GetDirectories(strPath);
                return dir.ToList();
            }
        }

        public static bool Exists(
            string strFileName,
            bool blnUseService = false)
        {
            try
            {
                if (blnUseService)
                {
                    return (bool)ProviderEvents.InvokeOnRunMethodDistributedViaService(
                        typeof(DirectoryHelper),
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
                lock (Serializer.GetLockObject(strFileName))
                {
                    return Directory.Exists(strFileName);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }


        public static bool Delete(
            string strFileName,
            bool blnIncludeSubDirs = false)
        {
            return Delete(strFileName, blnIncludeSubDirs, false);
        }

        public static bool Delete(
            string strFileName,
            bool blnIncludeSubDirs = false,
            bool blnUseService = false)
        {
            try
            {
                if (blnUseService)
                {
                    return (bool)ProviderEvents.InvokeOnRunMethodDistributedViaService(
                        typeof(DirectoryHelper),
                        "DeleteLocal",
                        new List<object>
                            {
                                strFileName,
                                blnIncludeSubDirs
                            });
                }
                return DeleteLocal(
                    strFileName,
                    blnIncludeSubDirs);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public static bool DeleteLocal(
            string strFileName,
            bool blnIncludeSubDirs)
        {
            try
            {
                lock (Serializer.GetLockObject(strFileName))
                {
                    Directory.Delete(strFileName, blnIncludeSubDirs);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Logger.Log(new HCException("Could not delete dir [" +
                    strFileName + "]"));
            }
            return false;
        }


        public static bool CreateDirectory(
            string strFileName,
            bool blnUseService = false)
        {
            try
            {
                if (blnUseService)
                {
                    return (bool)ProviderEvents.InvokeOnRunMethodDistributedViaService(
                        typeof(DirectoryHelper),
                        "CreateDirectoryLocal",
                        new List<object>
                            {
                                strFileName
                            });
                }
                return CreateDirectoryLocal(
                    strFileName);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public static bool CreateDirectoryLocal(
            string strFileName)
        {
            try
            {
                if (NetworkHelper.IsADistWorkerConnected)
                {
                    Logger.Log(
                        new HCException(
                        "Worker should not access data! " +
                        Environment.StackTrace));
                }

                lock (Serializer.GetLockObject(strFileName))
                {
                    Directory.CreateDirectory(strFileName);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public static bool Move(
            string strDestDir, 
            string strBakDir, 
            bool blnUseService)
        {
            if (blnUseService)
            {
                return (bool) ProviderEvents.InvokeOnRunMethodDistributedViaService(
                    typeof (DirectoryHelper),
                    "MoveLocal",
                    new List<object>
                    {
                        strDestDir,
                        strBakDir
                    });
            }
            return MoveLocal(
                strDestDir,
                strBakDir);
        }

        public static bool MoveLocal(
            string strDestDir,
            string strBakDir)
        {
            try
            {
                Directory.Move(
                    strDestDir,
                    strBakDir);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

    }
}


