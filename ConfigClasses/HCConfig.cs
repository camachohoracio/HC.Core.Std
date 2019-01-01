#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Xml;
using HC.Core.Exceptions;
using HC.Core.Io;
using HC.Core.Logging;
using HC.Core.Threading;

#endregion

namespace HC.Core.ConfigClasses
{
    //
    // HCConfig to provide access to sql and resource registries.
    //
    public sealed class HCConfig : IDisposable
    {
        #region Constants

        private const string RESOURCE_CONFIG_FILE = "Config_Resources.xml";
        private const string SETUP_FILE_SUFFIX = "_setup.txt";
        public static bool UseDefaultConfigPath = true;
        public const string DEFAULT_CONFIG_PATH = @"Config\HC.Configs";
        public static readonly string ResultsPath = @"c:\" + CoreConstants.COMPANY_NAME + @"\Results";
        private Boolean m_blnDisposed;

        #endregion

        #region Properties

        public static string ClientUniqueName { get; private set; }
        public static string DnsName { get; private set; }
        public static string ConfigDir { get; private set; }
        public static string DllName { get; private set; }

        #endregion

        #region Members

        private static readonly object m_classLock = new object();
        private readonly object m_lock = new object();

        private ConcurrentDictionary<string, XmlDocument> m_registries =
            new ConcurrentDictionary<string, XmlDocument>();

        private static readonly HCConfig m_quantConfig = new HCConfig();

        #endregion

        #region Constructors

        static HCConfig()
        {
            string strConfigDir = Configs.Get("config_dir");
            if (!string.IsNullOrEmpty(strConfigDir))
            {
                if (!DirectoryHelper.Exists(
                    strConfigDir,
                    false))
                {
                    throw new HCException("Config dir not found: " + 
                    strConfigDir);
                }
                SetConfigDir(strConfigDir);
            }
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                string strLocation = entryAssembly.Location;
                DllName = Path.GetFileName(strLocation);
            }
            DnsName = Dns.GetHostName();

            ClientName = DnsName + "%" + DllName;
            ClientUniqueName = DnsName + "%" +
                DllName + "%" +
                Process.GetCurrentProcess().Id;
        }

        public static void AppendToClientUniqueName(string s)
        {
            ClientUniqueName += s;
        }

        /// <summary>
        /// Singleton
        /// </summary>
        private HCConfig()
        {
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            if (!m_blnDisposed)
            {
                m_registries.Clear();
                m_registries = null;
            }
            m_blnDisposed = true;
        }

        #endregion

        #region Public

        public static HCConfig GetRegistry()
        {
            return m_quantConfig;
        }

        public static void DeployConfigs(string strDestPath)
        {
            LoadDefaultConfigs();
            string strPath = GetDefaultConfigPath();
            List<string> fileList = FileHelper.GetFileList(strPath);
            foreach (string strSourceFileName in fileList)
            {
                string strDestFileName = Path.Combine(DEFAULT_CONFIG_PATH,
                             new FileInfo(strSourceFileName).Name);
                strDestFileName = Path.Combine(strDestPath,
                             strDestFileName);
                string strDestDir = new FileInfo(strDestFileName).DirectoryName;
                if (strDestDir != null && 
                    !DirectoryHelper.Exists(
                    strDestDir,
                    false))
                {
                    DirectoryHelper.CreateDirectory(strDestDir);
                }
                File.Copy(strSourceFileName, strDestFileName, true);
            }
        }

        public static void LoadDefaultConfigs()
        {
            try
            {
                string strMessage = "Loading config files. Please wait...";
                Logger.Log(strMessage);
                Console.WriteLine(strMessage);

                string strPath = GetDefaultConfigPath();
                if (!DirectoryHelper.Exists(strPath))
                {
                    DirectoryHelper.CreateDirectory(strPath);
                }

                List<string> lookupPaths = Config.GetLookupPaths();

                foreach (string strLookupPath in lookupPaths)
                {
                    if (DirectoryHelper.Exists(strLookupPath))
                    {
                        List<string> strFileList = GetFilteredFileList(strLookupPath);

                        foreach (string strFileName in strFileList)
                        {
                            string strCurrFileName = new FileInfo(strFileName).Name;
                            string strDestFileName = Path.Combine(
                                strPath,
                                strCurrFileName);

                            bool blnCopyFile = CheckCopyFile(strFileName, strDestFileName);

                            if (blnCopyFile)
                            {
                                var fiDest = new FileInfo(strDestFileName);
                                string strDirName = fiDest.DirectoryName;
                                if (strDirName != null &&
                                    !DirectoryHelper.Exists(strDirName))
                                {
                                    DirectoryHelper.CreateDirectory(strDirName);
                                }

                                try
                                {
                                    File.Copy(strFileName,
                                        strDestFileName,
                                        true);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log(ex);
                                }
                                Console.WriteLine("loaded config = " + strDestFileName);
                            }
                        }
                    }
                }
                strMessage = "Finish loading config files";
                Logger.Log(strMessage);
                Console.WriteLine(strMessage);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Thread.Sleep(2000);
                LoadDefaultConfigs(); // do this recursively utill we succeed or die
            }
        }

        private static bool CheckCopyFile(string strFileName, string strDestFileName)
        {
            try
            {
                bool blnCopyFile = true;
                if (FileHelper.Exists(strDestFileName))
                {
                    var fiSource = new FileInfo(
                        strFileName);
                    var fiDest = new FileInfo(
                        strDestFileName);
                    DateTime sourceTime = fiSource.LastWriteTime;
                    DateTime destTime = fiDest.LastWriteTime;

                    //
                    // do not copy an old file
                    //
                    if (sourceTime <= destTime)
                    {
                        blnCopyFile = false;
                    }
                }
                return blnCopyFile;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        private static List<string> GetFilteredFileList(string strLookupPath)
        {
            List<string> strFileList = FileHelper.GetFileList(strLookupPath);
            var dateMap = new Dictionary<string, FileInfo>();
            foreach (string strFileName in strFileList)
            {
                string strCurrFileName = new FileInfo(strFileName).Name;
                if (strFileName.ToLower().EndsWith(".xml") &&
                    !strCurrFileName.ToLower().Equals("config_app.xml") &&
                    !strFileName.Contains(@"\bin\Debug\") &&
                    !strFileName.Contains(@"\obj\release\"))
                {
                    var fiSource = new FileInfo(strFileName);
                    FileInfo oldFi;
                    if(dateMap.TryGetValue(fiSource.Name, out oldFi))
                    {
                        if(oldFi.LastWriteTime < fiSource.LastWriteTime)
                        {
                            dateMap[fiSource.Name] = fiSource;
                        }
                    }
                    else
                    {
                        dateMap[fiSource.Name] = fiSource;
                    }
                }
            }
            return (from n in dateMap.Values select n.FullName).ToList();
        }

        public static string GetDefaultConfigPath()
        {
            string strPath = DEFAULT_CONFIG_PATH;
            strPath = Path.Combine(FileHelper.GetExecutingAssemblyDir(),
                                  strPath);
            return strPath;
        }

        public static List<string> GetList(string strList)
        {
            string[] strTokens = strList.Split('\n');
            for (int i = 0; i < strTokens.Length; i++)
            {
                strTokens[i] = strTokens[i].
                    Replace('\r', ' ').
                    Replace(" ", "").
                    Trim();
            }
            return new List<string>(strTokens);
        }


        //
        // Set the config dir if necessary.
        //
        public static void SetConfigDir(string strConfigDir)
        {
            lock (m_classLock)
            {
                if (!DirectoryHelper.Exists(strConfigDir))
                {
                    throw new HCException("Config dir not found: " + 
                    strConfigDir);
                }
                ConfigDir = strConfigDir;
                Logger.Log("Loaded config dir: " + strConfigDir);
            }
        }

        //
        // Get a sql query string.
        //
        public static string GetSqlConfigValue(string strName)
        {
            XmlDocument doc = m_quantConfig.GetSqlRegistry(Assembly.GetCallingAssembly());
            // Fetch the command node using XPath.
            string strXpath =
                "/commands/command[@name = \"" + strName + "\"]";
            return GetNodeValue(strXpath, doc);
        }

        public string GetConfigFullPath()
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            return GetConfigFullPath(callingAssembly);
        }

        public static string GetConfigFullPath(Type type)
        {
            return GetConfigFullPath(Assembly.GetAssembly(type));
        }

        public static string GetConfigFullPath(Assembly assembly)
        {
            string strConfigDirectory = GetExecutingAssembyDir();
            if (UseDefaultConfigPath)
            {
                return Path.Combine(
                    strConfigDirectory,
                    FileHelper.GetAssemblyName(assembly));
            }
            return strConfigDirectory;
        }

        public static string GetConfigSubPath()
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            return GetConfigSubPath(callingAssembly);
        }

        public string GetConfigSubPath(Type type)
        {
            return GetConfigSubPath(Assembly.GetAssembly(type));
        }

        public static string GetConfigSubPath(Assembly assembly)
        {
            if (UseDefaultConfigPath)
            {
                return FileHelper.GetAssemblyName(assembly);
            }
            return FileHelper.GetAssemblyName(assembly);
        }

        public static string GetAppPath()
        {
            return DirectoryHelper.CombineDirectory(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                CoreConstants.COMPANY_NAME,
                FileHelper.GetCallingAssemblyName());
        }

        public static string GetAppConfigValue(string strName)
        {
            XmlDocument doc = m_quantConfig.GetXmlDocument(Assembly.GetCallingAssembly());
            // Fetch the command node using XPath.
            string strXpath =
                "/commands/command[@name = \"" + strName + "\"]";
            return GetNodeValue(strXpath, doc);
        }

        public static ConfigConstants GetConfigConstants(
            string strFileName,
            bool blnLoadtoMemory)
        {
            Dictionary<string, object> constantDict = GetConstantDict(
                strFileName,
                blnLoadtoMemory);
            var configConstants = new ConfigConstants(constantDict);
            return configConstants;
        }

        public static ConfigConstants GetConfigConstantsFromString(
            string strString)
        {
            Dictionary<string, object> constantDict = GetConstantDictFromString(
                strString);
            var configConstants = new ConfigConstants(constantDict);
            return configConstants;
        }


        public static Dictionary<string, object> GetConstantDictFromString(
            string strDescr)
        {
            try
            {
                using (var reader =
                    new XmlTextReader(
                        new StringReader(strDescr)))
                {
                    var doc = new XmlDocument();
                    doc.Load(reader);
                    Dictionary<string, object> constantDict = GetConstantDict(doc);
                    return constantDict;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while loading registry value. " +
                                        e +
                                        Environment.NewLine +
                                        e.StackTrace);
                throw;
            }
        }


        public static Dictionary<string, object> GetConstantDict(
            string strFileName,
            bool blnLoadtoMemory)
        {
            try
            {
                XmlDocument doc = m_quantConfig.GetXmlDoc0(strFileName, blnLoadtoMemory);
                Dictionary<string, object> constantDict = GetConstantDict(doc);
                return constantDict;
            }
            catch (HCException e)
            {
                Console.WriteLine("Error while loading registry value. " +
                    e +
                    Environment.NewLine +
                    e.StackTrace);
                throw;
            }
        }

        private static Dictionary<string, object> GetConstantDict(XmlDocument doc)
        {
            try
            {
                var constantDict = new Dictionary<string, object>();
                XmlNodeList nodeList = doc.SelectNodes("constants");

                if (nodeList != null && nodeList.Count > 0)
                {
                    foreach (XmlNode node in nodeList[0].ChildNodes)
                    {
                        if (node != null && !node.ToString().Equals("#comment") &&
                            node.ChildNodes.Count > 0)
                        {
                            string strDescr = node.ChildNodes[0].OuterXml.Trim();
                            string[] tokens = strDescr.Split('#');
                            if (tokens.Length > 1)
                            {
                                string strType = tokens[1].ToLower().Trim();
                                string strValue = tokens[0].Trim();
                                object objValue;
                                if (strType.Equals("int"))
                                {
                                    objValue = int.Parse(strValue);
                                }
                                else if (strType.Equals("double"))
                                {
                                    objValue = double.Parse(strValue);
                                }
                                else if (strType.Equals("bool"))
                                {
                                    objValue = bool.Parse(strValue);
                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }
                                constantDict[
                                    node.Name] =
                                    objValue;

                            }
                            else
                            {
                                constantDict[
                                    node.Name] =
                                    strDescr;
                            }
                        }
                    }
                }
                return constantDict;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static List<string> GetConfigList(
            string strInput,
            Type type)
        {
            var strRawLine = GetConstant<string>(
                strInput,
                type);

            string[] strTokens = strRawLine.Split('\n');
            var tokenList = new List<string>();

            foreach (string strToken in strTokens)
            {
                if (!string.IsNullOrEmpty(strToken.Trim()))
                {
                    tokenList.Add(
                        strToken.Trim());
                }
            }

            return tokenList;
        }

        public static T GetConstant<T>(
            string strXmlFileName,
            string strName)
        {
            XmlDocument doc = m_quantConfig.GetXmlDocument(
                strXmlFileName);

            string strXPath =
                "/constants/" + strName;
            return (T)Convert.ChangeType(
                GetNodeValue(strXPath,
                             doc),
                typeof(T));
        }

        public static T GetConstant<T>(
            string strName,
            Type type)
        {
            Assembly assembly = Assembly.GetAssembly(type);
            return GetConstant<T>(
                strName,
                assembly);
        }

        public static T GetConstant<T>(
            string strName,
            Assembly assembly)
        {
            try
            {
                XmlDocument doc = m_quantConfig.GetXmlDocument(
                    assembly);
                // Fetch the command node using XPath.
                string strXpath =
                    "/constants/" + strName;
                return (T)Convert.ChangeType(
                               GetNodeValue(strXpath, doc),
                               typeof(T));
            }
            catch (HCException e)
            {
                Logger.Log("Error while loading registry value: " + 
                    strName);
                Logger.Log(e);
                throw;
            }
        }

        private XmlDocument GetXmlDocument(Assembly callingAssembly)
        {
            string strFilename = string.Empty;
            try
            {
                var assemblyName = new AssemblyName(callingAssembly.FullName);
                string strAssemblyName = assemblyName.Name;
                strFilename = GetRegistryFilename(strAssemblyName + "_config.xml");
                XmlDocument registry = GetXmlDocument(strFilename);

                return registry;
            }
            catch (Exception ex)
            {
                Logger.Log("Exception in file: " + strFilename);
                Logger.Log(ex);
            }
            return null;
        }

        private XmlDocument GetXmlDocument(string strFilename)
        {
            XmlDocument registry = GetXmlDoc(strFilename);

            if (registry == null)
            {
                throw new HCException("Could not load registry:" + strFilename);
            }
            return registry;
        }


        //
        // Get the value of a specified text node from the
        // resource registry.
        //
        public string GetNodeValue(string strXpath)
        {
            XmlDocument doc = m_quantConfig.GetResourceRegistry();
            return GetNodeValue(strXpath, doc);
        }

        //
        // Get the resource registry XML document.
        //
        public XmlDocument GetDoc()
        {
            return m_quantConfig.GetResourceRegistry();
        }

        public static string GetAppSetupFullFileName(Type type)
        {
            return Path.Combine(
                GetConfigFullPath(type),
                GetAppSetupFileName(type));
        }

        #endregion

        #region Private

        private static string GetAppSetupFileName(Type type)
        {
            string strAsspemblyName = FileHelper.GetAssemblyName(
                Assembly.GetAssembly(type));
            return strAsspemblyName + SETUP_FILE_SUFFIX;
        }

        //
        // Get the config directory.
        //
        public static string GetExecutingAssembyDir()
        {
            string strConfigDirectory;

            lock (m_classLock)
            {
                if (string.IsNullOrEmpty(ConfigDir))
                {
                    strConfigDirectory = FileHelper.GetExecutingAssemblyDir();
                }
                else
                {
                    strConfigDirectory = ConfigDir;
                }
            }

            return strConfigDirectory;
        }

        //
        // Get the sql registry for the specified calling assembly.
        //
        private XmlDocument GetSqlRegistry(Assembly callingAssembly)
        {
            var assemblyName = new AssemblyName(callingAssembly.FullName);
            string strAssemblyName = assemblyName.Name;
            string strFilename = GetRegistryFilename(strAssemblyName + "_sql.xml");
            XmlDocument registry = GetXmlDoc(strFilename);

            if (registry == null)
            {
                throw new HCException("Could not load registry:" + strFilename);
            }

            return registry;
        }

        //
        // Get the resource registry for the specified calling assembly.
        //
        private XmlDocument GetResourceRegistry()
        {
            string strFilename = GetRegistryFilename(RESOURCE_CONFIG_FILE);
            XmlDocument registry = GetXmlDoc(strFilename);

            if (registry == null)
            {
                throw new HCException("Could not load registry:" + strFilename);
            }

            return registry;
        }

        /// <summary>
        /// Get the registry filename for the specified calling assembly. 
        /// </summary>
        /// <param name="strConfigFile"></param>
        /// <returns></returns>
        private static string GetRegistryFilename(
            string strConfigFile)
        {
            string strConfigDirectory = GetExecutingAssembyDir();
            string strRegistryFilename;
            if (UseDefaultConfigPath)
            {
                strRegistryFilename = Path.Combine(
                    strConfigDirectory,
                    strConfigFile);
            }
            else
            {
                strRegistryFilename = Path.Combine(strConfigDirectory, strConfigFile);
            }
            return strRegistryFilename;
        }

        //
        // Get the registry for the filename.
        //
        private XmlDocument GetXmlDoc(string strFilename)
        {
            CustomConfigs.LoadCustomConfigs(strFilename);
            return GetXmlDoc0(strFilename, true);
        }

        private XmlDocument GetXmlDoc0(
            string strFilename,
            bool blnLoadtoMemory)
        {
            XmlDocument doc;
            if (!m_registries.TryGetValue(strFilename, out doc))
            {
                lock (m_lock)
                {
                    lock (LockObjectHelper.GetLockObject(strFilename))
                    {
                        if (!m_registries.TryGetValue(strFilename, out doc))
                        {
                            if (!FileHelper.Exists(strFilename))
                            {
                                throw new HCException("Could not open resource file " + strFilename);
                            }

                            var file = new FileInfo(strFilename);
                            doc = new XmlDocument();
                            using (var stream = file.OpenRead())
                            {
                                doc.Load(stream);
                                if (blnLoadtoMemory)
                                {
                                    m_registries[strFilename] = doc;
                                }
                                stream.Close();
                            }
                            Logger.Log("Loaded config file name = " +
                                       strFilename);
                        }
                    }
                }
            }
            return doc;
        }

        //
        // Get the value of a specified text node.
        //
        private static string GetNodeValue(string strXpath, XmlDocument doc)
        {
            string strNodeValue = "";

            XmlNode node = doc.SelectSingleNode(strXpath);
            if (node == null)
            {
                Logger.Log("Failed to get node: " + strXpath);
                return string.Empty;
            }

            node = doc.SelectSingleNode(strXpath + "/text()");

            if (node != null)
            {
                strNodeValue = node.Value;
            }

            // Remove any whitespace.
            strNodeValue = strNodeValue.Trim();

            return strNodeValue;
        }

        #endregion

        public static List<string> ReadListFromFile(
            string strFileName)
        {
            try
            {
                using (var sr = new StreamReader(strFileName))
                {
                    string strLine;
                    var list = new List<string>();
                    while ((strLine = sr.ReadLine()) != null)
                    {
                        list.Add(strLine.Trim());
                    }
                    return list;
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<string>();
        }

        public static string ClientName { get; private set; }
    }
}


