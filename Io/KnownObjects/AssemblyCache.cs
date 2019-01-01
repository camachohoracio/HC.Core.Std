#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HC.Core.Logging;
using System.IO;
using HC.Core.Exceptions;
using System.Reflection;

#endregion

namespace HC.Core.Io.KnownObjects
{
    public static class AssemblyCache
    {
        public static readonly object AssemblyLock = new object();
        public static List<Assembly> LoadedAssemblies { get; private set; }
 
        #region Members

        private static bool m_blnIsInitialized;
        private static readonly object m_lockObject = new object();
        private static string[] m_strDirArr;
        private static readonly string m_strDefaultAssemblyLocation;

        #endregion

        static AssemblyCache()
        {
            try
            {
                m_strDefaultAssemblyLocation =
                    new FileInfo("dummy").DirectoryName + @"\Default";
                if (DirectoryHelper.Exists(m_strDefaultAssemblyLocation))
                {
                    File.Delete(m_strDefaultAssemblyLocation);
                    DirectoryHelper.Delete(
                        m_strDefaultAssemblyLocation,
                        false);
                }
                DirectoryHelper.CreateDirectory(m_strDefaultAssemblyLocation);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        #region Public

        public static List<Assembly> GetLoadedAssemblies(Assembly callingAssembly)
        {
            if (LoadedAssemblies != null)
            {
                return LoadedAssemblies;
            }
            lock (AssemblyLock)
            {
                if (LoadedAssemblies != null)
                {
                    return LoadedAssemblies;
                }
                Dictionary<string, List<string>> knownAssembliesMap = Config.GetKnownAssembliesMap();
                AssemblyName[] referencedAssemblies = callingAssembly.GetReferencedAssemblies();
                var loadedAssemblies = (from n in AppDomain.CurrentDomain.GetAssemblies()
                                        where !n.IsDynamic &&
                                              !String.IsNullOrEmpty(n.Location) &&
                                              knownAssembliesMap.ContainsKey(
                                                  new FileInfo(n.Location).Name.ToLower())
                                        select n).ToList();
                string strAssemblyLocation = new FileInfo(Assembly.GetCallingAssembly().Location).DirectoryName;
                strAssemblyLocation = string.IsNullOrEmpty(strAssemblyLocation) ? string.Empty : strAssemblyLocation;
                string strLocalAssemblyLocation = new FileInfo("dummy").DirectoryName;
                string strMessage = "Default assembly from location: " + strAssemblyLocation;
                Logger.Log(strMessage);
                Console.WriteLine(strMessage);
                strMessage = "Local assembly from location: " + strLocalAssemblyLocation;
                Logger.Log(strMessage);
                Console.WriteLine(strMessage);
                
                foreach (AssemblyName referencedAssembly in referencedAssemblies)
                {
                    string strAssemblyName = referencedAssembly.Name + ".dll";
                    strAssemblyName = strAssemblyName.ToLower();
                    if (knownAssembliesMap.ContainsKey(strAssemblyName))
                    {
                        loadedAssemblies.Add(AppDomain.CurrentDomain.Load(referencedAssembly));
                        knownAssembliesMap.Remove(strAssemblyName);
                    }
                }

                LoadMissingAsemblies(
                    knownAssembliesMap, 
                    loadedAssemblies, 
                    strAssemblyLocation, 
                    strLocalAssemblyLocation);
                LoadedAssemblies = loadedAssemblies.Distinct().ToList();
            }
            return LoadedAssemblies;
        }

        private static void LoadMissingAsemblies(
            Dictionary<string, List<string>> knownAssembliesMap, 
            List<Assembly> loadedAssemblies, 
            string strAssemblyLocation, 
            string strLocalAssemblyLocation)
        {
            foreach (var kvp in knownAssembliesMap)
            {
                try
                {
                    string strAssemly = kvp.Key;
                    string strAssemblyLocalFileName =
                        Path.Combine(strAssemblyLocation,
                                     strAssemly);

                    if (!File.Exists(strAssemblyLocalFileName))
                    {
                        strAssemblyLocalFileName =
                            Path.Combine(strLocalAssemblyLocation,
                                         strAssemly);
                    }

                    string strRemoteFileName = GetRemoteAssemblyFileName(
                        knownAssembliesMap,
                        strAssemly);

                    if (File.Exists(strAssemblyLocalFileName))
                    {
                        FileInfo fiRemote = null;
                        if (!string.IsNullOrEmpty(strRemoteFileName))
                        {
                            fiRemote = new FileInfo(strRemoteFileName);
                        }
                        if (fiRemote != null)
                        {
                            var fiLocal = new FileInfo(strAssemblyLocalFileName);
                            //
                            // compare files
                            //
                            if (!FileHelper.Equals(
                                fiLocal,
                                fiRemote) &&
                                fiRemote.LastWriteTimeUtc >
                                fiLocal.LastWriteTimeUtc)
                            {
                                try
                                {
                                    //
                                    // remote is newer than local. Then copy over
                                    //
                                    CopyAssembly(strAssemblyLocalFileName, strRemoteFileName);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex);
                                }
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(strRemoteFileName))
                    {
                        try
                        {
                            CopyAssembly(
                                strAssemblyLocalFileName,
                                strRemoteFileName);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }

                    string strMessage;
                    if (File.Exists(strAssemblyLocalFileName))
                    {
                        loadedAssemblies.Add(Assembly.LoadFrom(strAssemblyLocalFileName));
                        strMessage = "Loaded known assembly from [" +
                                            strAssemblyLocalFileName + "]";
                        Logger.Log(strMessage);
                        Console.WriteLine(strMessage);
                    }
                    else
                    {
                        strMessage = "assembly file not found [" +
                                            strAssemblyLocalFileName + "]";
                        Logger.Log(strMessage);
                        Console.WriteLine(strMessage);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
        }

        private static void CopyAssembly(
            string strAssemblyLocalFileName, 
            string strRemoteFileName)
        {
            try
            {
                File.Copy(strRemoteFileName,
                         strAssemblyLocalFileName,
                         true);

                var fiRemote = new FileInfo(strRemoteFileName);
                string strPdbFileNameRemote =
                    Path.Combine(
                    fiRemote.DirectoryName,
                    fiRemote.Name.Replace(
                    fiRemote.Extension,
                    ".pdb"));

                var fiLocal = new FileInfo(strAssemblyLocalFileName);
                string strPdbFileNameLocal =
                    Path.Combine(
                    fiLocal.DirectoryName,
                    fiLocal.Name.Replace(
                    fiLocal.Extension,
                    ".pdb"));
                if (File.Exists(strPdbFileNameRemote))
                {
                    File.Copy(strPdbFileNameRemote,
                             strPdbFileNameLocal,
                             true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static string GetRemoteAssemblyFileName(
            Dictionary<string, List<string>> knownAssemblies,
            string strAssemly)
        {
            List<string> fileList;
            var foundFileMap = new Dictionary<string,DateTime>();
            if (knownAssemblies.TryGetValue(strAssemly, out fileList))
            {
                if (fileList != null)
                {
                    foreach (string strPath in fileList)
                    {
                        var strFileName = Path.Combine(
                            strPath,
                            strAssemly);
                        if (File.Exists(strFileName))
                        {
                            foundFileMap[strFileName] = new FileInfo(strFileName).LastWriteTimeUtc;
                        }
                    }
                }
            }
            if(foundFileMap.Count > 0)
            {
                var foundFileList = foundFileMap.ToList();
                //
                // sort by  last write date
                //
                foundFileList.Sort((a,b) => -a.Value.CompareTo(b.Value));
                return foundFileMap.First().Key;
            }
            return string.Empty;
        }

        public static void Initialize()
        {
            if (!m_blnIsInitialized)
            {
                lock (m_lockObject)
                {
                    if (!m_blnIsInitialized)
                    {
                        //LoadCulture();
                        Assembly callingAssembly = Assembly.GetCallingAssembly();
                        GetLoadedAssemblies(callingAssembly);
                        //
                        // get config dirs
                        //
                        string strConfigDirs = Config.GetAssemblyCache();
                        var directories = new List<string>();
                        if (!string.IsNullOrEmpty(strConfigDirs))
                        {
                            var strDirArr = GetList(strConfigDirs).ToArray();
                            foreach (string strDir in strDirArr)
                            {
                                if (!DirectoryHelper.Exists(strDir))
                                {
                                    string strMessage = "Assembly dir not found: " +
                                        strDir;
                                    Console.WriteLine(strMessage);
                                }
                                else
                                {
                                    directories.Add(strDir);
                                }
                            }
                        }
                        strConfigDirs = Configs.Get("assembly_cache");
                        if (!string.IsNullOrEmpty(strConfigDirs))
                        {
                            var strDirArr = GetList(strConfigDirs).ToArray();
                            foreach (string strDir in strDirArr)
                            {
                                if (!DirectoryHelper.Exists(strDir))
                                {
                                    string strMessage = "Assembly dir not found: " +
                                        strDir;
                                    Console.WriteLine(strMessage);
                                }
                                else
                                {
                                    directories.Add(strDir);
                                }
                            }
                        }
                        directories.Add(m_strDefaultAssemblyLocation);
                        m_strDirArr = directories.Distinct().ToArray();
                        //
                        // subscribe to loaded assemblies
                        //
                        if (m_strDirArr != null &&
                            m_strDirArr.Length > 0)
                        {
                            AppDomain.CurrentDomain.AssemblyResolve +=
                                CurrentDomainAssemblyResolve;
                        }
                        m_blnIsInitialized = true;
                    }
                }
            }
        }

        private static void LoadCulture()
        {
            try
            {
                var field = typeof(CultureInfo).GetField("s_userDefaultCulture",
                                                          BindingFlags.Static |
                                                          BindingFlags.NonPublic);
                field.SetValue(null, new CultureInfo("en-US"));
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }


        #endregion

        #region Private

        private static List<string> GetList(string strList)
        {
            string[] strTokens = strList.Split("\n,".ToCharArray());
            for (int i = 0; i < strTokens.Length; i++)
            {
                strTokens[i] = strTokens[i].
                    Replace('\r', ' ').
                    Replace(" ", "").
                    Trim();
            }
            return new List<string>(strTokens);
        }

        private static Assembly CurrentDomainAssemblyResolve(
            object sender,
            ResolveEventArgs args)
        {
            try
            {
                string strAssemblyName = (args.Name.Split(',')[0] + ".dll").ToLower();
                if (strAssemblyName.EndsWith(".resources.dll"))
                {
                    return null;
                }
                Dictionary<string, DateTime> foundFilesList = 
                    FindFiles(strAssemblyName);

                string strCurrAssemblyFileName = string.Empty;
                if (foundFilesList.Count > 0)
                {
                    var foundFilesArr = foundFilesList.ToList();
                    foundFilesArr.Sort((a, b) => -a.Value.CompareTo(b.Value));
                    strCurrAssemblyFileName = foundFilesArr.First().Key;
                }

                if (string.IsNullOrEmpty(strCurrAssemblyFileName))
                {
                    if (!strAssemblyName.ToLower().Contains("lz4"))
                    {
                        Logger.Log(new HCException("Assembly file not found: " +
                                                   strAssemblyName,
                                                   false));
                    }
                    return null;
                }

                var fiRemoteAssembly = new FileInfo(strCurrAssemblyFileName);
                string strLocalAssemblyFileName = Path.Combine(
                    m_strDefaultAssemblyLocation,
                    fiRemoteAssembly.Name);
                CopyAssembly(strLocalAssemblyFileName, strCurrAssemblyFileName);
                return Assembly.LoadFrom(strLocalAssemblyFileName);
            }
            catch (Exception ex)
            {
                if(ex.Message.Contains("lz4"))
                {
                    return null;
                }
                Logger.Log(ex, false);
            }
            return null;
        }

        private static Dictionary<string, DateTime> FindFiles(string strAssemblyName)
        {
            var foundFilesList = new Dictionary<string, DateTime>();
            foreach (string strDir in m_strDirArr)
            {
                var strAssemblyFileName = Path.Combine(
                    strDir,
                    strAssemblyName);
                if (File.Exists(strAssemblyFileName))
                {
                    foundFilesList[strAssemblyFileName] = new FileInfo(strAssemblyFileName).LastWriteTimeUtc;
                }
            }

            return foundFilesList;
        }

        #endregion
    }
}



