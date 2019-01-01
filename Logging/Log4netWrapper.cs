#region

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using HC.Core.ConfigClasses;
using log4net;
using log4net.Config;
using log4net.Repository;
using HC.Core.Exceptions;
using HC.Core.Io;
using HC.Core.DynamicCompilation;
using HC.Core.Events;
using System.Reflection;

#endregion

namespace HC.Core.Logging
{
    /// <summary>
    /// Log4Net implementation of the ILoggerService interface.
    /// </summary>
    public class Log4NetWrapper : ILoggerService
    {
        #region Events & delegates

        public delegate void OnLogDelegate(string strError);
        public static event OnLogDelegate OnLog;
        
        #endregion

        #region Members

        private readonly ILog m_log;
        private static DateTime m_logDate;
        private static readonly object m_fileNameLock = new object();
        /// <summary>
        /// Avoid having multiple appdomains using the same config file name
        /// at the same time
        /// </summary>
        private static readonly object m_lockObject = new object();
        private static readonly ConcurrentDictionary<string,int> m_exCountMap = 
            new ConcurrentDictionary<string, int>();

        #endregion

        #region Properties

        public static string LogFileName { get; private set; }

        #endregion

        #region Constants

        private const string CONFIG_NAME = "log4net.config";
        public const string LOG_PATH = @"c:\HC\Logs";

        #endregion

        #region Constructors

        static Log4NetWrapper()
        {
            try
            {
                LoadLogConfig();
                SetLogFileName();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public Log4NetWrapper(string strName)
        {
            bool blnError = false;
            try
            {
                //
                // always use today's date
                //
                //lock (m_fileNameLock)
                //{
                //    if (m_logDate < DateTime.Today)
                //    {
                //        blnError = SetLogFileName();
                //    }
                //}
                m_log = LogManager.GetLogger(Assembly.GetEntryAssembly(), strName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                blnError = true;
            }
            if (blnError)
            {
                try
                {
                    LoadLogConfig();
                    SetLogFileName();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        #endregion

        #region Private

        private static void LoadLogConfig()
        {
            lock (m_lockObject)
            {
                string strConfigDir = Configs.Get("config_dir");
                if (string.IsNullOrEmpty(strConfigDir))
                {
                    strConfigDir = FileHelper.GetCurrentAssemblyPath();
                }
                string strConfigName =
                    Path.Combine(
                        strConfigDir,
                        CONFIG_NAME);
                if (!FileHelper.Exists(strConfigName))
                {
                    strConfigName =
                    Path.Combine(
                        HCConfig.ConfigDir,
                        CONFIG_NAME);
                    if (!FileHelper.Exists(strConfigName))
                    {
                        throw new HCException("File not found: " + strConfigName);
                    }
                }
                var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
                XmlConfigurator.Configure(
                    logRepository,
                    new FileInfo(strConfigName));
            }
        }

        public static bool SetLogFileName(
            string strName = "")
        {
            try
            {
                var logDate = DateTime.Today;
                string strPath = Path.Combine(
                    LOG_PATH,
                    logDate.ToString("yyyMMdd"));
                bool blnDirExists;
                while (!(blnDirExists = DirectoryHelper.Exists(strPath)))
                {
                    try
                    {
                        DirectoryHelper.CreateDirectory(strPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        Thread.Sleep(5000);
                    }
                }

                LogFileName = Path.Combine(strPath,
                                           (string.IsNullOrEmpty(strName) ?
                                           Configs.Get("product_name") :
                                           strName) + ".log");
                bool blnError = false;
                if (blnDirExists && FileHelper.Exists(LogFileName))
                {
                    try
                    {
                        var fi = new FileInfo(LogFileName);
                        int intCounter = 1;
                        string strDirName = fi.DirectoryName;

                        if (!string.IsNullOrEmpty(strDirName))
                        {
                            string strNewFileName =
                                Path.Combine(
                                    strDirName,
                                    intCounter + "_" +
                                    fi.Name);
                            while (FileHelper.Exists(strNewFileName))
                            {
                                strNewFileName =
                                    Path.Combine(
                                        strDirName,
                                        intCounter + "_" +
                                        fi.Name);
                                intCounter++;
                            }

                            File.Move(LogFileName, strNewFileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        Console.WriteLine(ex.StackTrace);
                    }
                }

                if (!ChangeLogFileName(LogFileName))
                {
                    Console.WriteLine("Log appender not found.");
                    return false;
                }

                m_logDate = logDate;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return false;
        }

        private static bool ChangeLogFileName(string strNewFilename)
        {
            try
            {
                bool blnResult = ChangeLogFileName("LogFileAppender", strNewFilename);

                if (!blnResult)
                {
                    var fi = new FileInfo(strNewFilename);
                    if (fi.DirectoryName != null)
                    {
                        string strFile =
                            Path.Combine(fi.DirectoryName,
                                         fi.Name.Replace(fi.Extension, string.Empty) + "_" +
                                         Process.GetCurrentProcess().Id + fi.Extension);
                        blnResult = ChangeLogFileName("LogFileAppender", strFile);
                    }
                }
                return blnResult;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        public static bool ChangeLogFileName(string appenderName, string newFilename)
        {
            try
            {
                ILoggerRepository rootRep = LogManager.GetRepository(Assembly.GetEntryAssembly());


                foreach (log4net.Appender.IAppender iApp in rootRep.GetAppenders())
                {

                    if (iApp.Name.CompareTo(appenderName) == 0 &&
                        iApp is log4net.Appender.FileAppender)
                    {
                        var fApp = (log4net.Appender.FileAppender) iApp;
                        fApp.File = newFilename;

                        bool blnIsError = false;
                        fApp.ErrorHandler = new ErrHandler((a, b) => { blnIsError = true; });
                        fApp.ActivateOptions();
                        if (blnIsError)
                        {
                            return false;
                        }
                        return true; // Appender found and name changed to NewFilename
                    }
                }

                return false; // appender not found
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }


        #endregion

        #region ILoggerService Members

        public void Write(string strMessage)
        {
            try
            {
                strMessage =
                    "Message: " +
                    strMessage +
                    Environment.NewLine +
                    Environment.NewLine;

                if (m_log.IsInfoEnabled)
                {
                    m_log.Info(strMessage);
                }
                if (OnLog != null &&
                    OnLog.GetInvocationList().Length > 0)
                {
                    OnLog.Invoke(strMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Write(Exception ex)
        {
            try
            {
                Write(ex, true);
            }
            catch (Exception ex2)
            {
                Console.WriteLine(ex2);
            }
        }

        public void Write(
            Exception ex,
            bool blnPublishGui)
        {
            try
            {
                string strErrorMessage =
                    "Exception: " +
                    ex.Message +
                    Environment.NewLine +
                    "Trace:" +
                    Environment.NewLine +
                    ex.StackTrace +
                    Environment.NewLine +
                    Environment.NewLine;

                //
                // log in gui
                //
                if (blnPublishGui)
                {
                    PublishGui(ex);
                }
                if (m_log.IsErrorEnabled)
                {
                    m_log.Error(strErrorMessage);
                }
                if (OnLog != null &&
                    OnLog.GetInvocationList().Length > 0)
                {
                    OnLog.Invoke(strErrorMessage);
                }
            }
            catch (Exception ex2)
            {
                Console.WriteLine(ex2);
            }
        }

        private static void PublishGui(Exception ex)
        {
            try
            {
                string strAssembly;
                if (ex.TargetSite != null &&
                    ex.TargetSite.DeclaringType != null)
                {
                    strAssembly = ex.TargetSite.DeclaringType.Assembly.GetName().Name;
                }
                else
                {
                    strAssembly = "UnknownAssembly";
                }
                string strStackTrace = ex.StackTrace;

                PublishGui(
                    "Exceptions",
                    strAssembly,
                    ex.Message,
                    strStackTrace,
                    ex.GetType().Name);
            }
            catch (Exception ex2)
            {
                Console.WriteLine(ex2);
            }
        }

        public static void PublishGui(
            string strGuiNode,
            string strAssembly, 
            string strMessage,
            string strStackTrace,
            string strExceptionType)
        {
            try
            {
                var exceptionClass = new SelfDescribingClass();
                exceptionClass.SetClassName("ExceptionClass");
                exceptionClass.SetStrValue("Message", strMessage);
                exceptionClass.SetStrValue("StackTrace", strStackTrace);
                exceptionClass.SetDateValue("Time", DateTime.Now);
                exceptionClass.SetStrValue("ClientName", HCConfig.ClientUniqueName);
                exceptionClass.SetStrValue("ExceptionType", strExceptionType);

                string strMessageKey = HCConfig.ClientUniqueName + strMessage;
                strMessageKey = strMessageKey.GetHashCode().ToString() +
                                strMessageKey.Length;
                int intCounter;
                m_exCountMap.TryGetValue(strMessageKey, out intCounter);
                intCounter++;
                m_exCountMap[strMessageKey] = intCounter;
                exceptionClass.SetIntValue(
                    "NumMessages",
                    intCounter);

                LiveGuiPublisherEvent.PublishGrid(
                    "Admin",
                    strGuiNode,
                    strAssembly,
                    strMessageKey,
                    exceptionClass,
                    2,
                    false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #endregion
    }
}


