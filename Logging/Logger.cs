#region

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using HC.Core.ConfigClasses;
using HC.Core.Io;
using HC.Core.Comunication;
using HC.Core.Threading.ProducerConsumerQueues;

#endregion

namespace HC.Core.Logging
{
    public static class Logger
    {
        private static readonly ProducerConsumerQueue<LogQueueItem> m_exceptionQueue;

        static Logger()
        {
            try
            {
                m_exceptionQueue = new ProducerConsumerQueue<LogQueueItem>(10, 10000, false, false);
                m_exceptionQueue.SetAutoDisposeTasks(true);
                m_exceptionQueue.DoNotLogExceptions = true;
                m_exceptionQueue.DoLogging();
                m_exceptionQueue.OnWork += logQueueItem => Log01(
                    logQueueItem.ex,
                    logQueueItem.blnSendMail,
                    logQueueItem.blnPublishGui,
                    logQueueItem.blnPublishTopic);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void Log(string strMessage)
        {
            try
            {
                Assembly callingAssembly = Assembly.GetCallingAssembly();
                Log(strMessage,
                    false,
                    false,
                    callingAssembly);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void Log(string strMessage, bool blnSendEmail)
        {
            try
            {
                Log(strMessage, blnSendEmail, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void Log(
            string strMessage,
            bool blnSendEmail,
            bool blnPublishGui)
        {
            try
            {
                Log(strMessage,
                    blnSendEmail,
                    blnPublishGui,
                    false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void Log(
            string strMessage, 
            bool blnSendEmail,
            bool blnPublishGui,
            bool blnPublishTopic)
        {
            try
            {
                Assembly callingAssembly = Assembly.GetCallingAssembly();
                Log(strMessage,
                    blnSendEmail,
                    blnPublishGui,
                    blnPublishTopic,
                    callingAssembly);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void Log(
            string strMessage,
            bool blnSendEmail,
            bool blnPublishGui,
            Assembly callingAssembly)
        {
            try
            {
                Log(strMessage,
                    blnSendEmail,
                    blnPublishGui,
                    false,
                    callingAssembly);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void Log(
            string strMessage,
            bool blnSendEmail,
            bool blnPublishGui,
            bool blnPublishTopic,
            Assembly callingAssembly)
        {
            try
            {
                if (callingAssembly == null)
                {
                    return;
                }
                StackFrame frame = new StackTrace().GetFrame(3);
                Log(strMessage, 
                    blnSendEmail, 
                    blnPublishGui, 
                    callingAssembly,
                    frame);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void Log(
            string strMessage,
            bool blnSendEmail,
            bool blnPublishGui,
            Assembly callingAssembly,
            StackFrame stackFrame)
        {
            try
            {
                Log(
                    strMessage,
                    blnSendEmail,
                    blnPublishGui,
                    false,
                    callingAssembly,
                    stackFrame);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void Log(
            string strMessage,
            bool blnSendEmail,
            bool blnPublishGui,
            bool blnPublishTopic,
            Assembly callingAssembly,
            StackFrame stackFrame)
        {
            try
            {
                if(callingAssembly == null)
                {
                    return;
                }
                string strName = callingAssembly.GetName().Name;
                string strTitle = string.Empty;
                if (stackFrame != null)
                {
                    MethodBase method = stackFrame.GetMethod();
                    strTitle = method.DeclaringType + "." + method.Name;
                }
                strTitle = HCConfig.ClientUniqueName + "---" + strTitle;

                string strMsgBoddy = "{" + strName + "." + strTitle + " } - " +
                    strMessage;
                GetLogger(strName).Write(strMsgBoddy);

                if (blnSendEmail)
                {
                    MailImpl.SendEmail(
                        strTitle + " [" + callingAssembly.GetName().Name + "]", 
                        strMessage,
                        "HCExceptions");
                }
                if (blnPublishGui)
                {
                    Log4NetWrapper.PublishGui(
                        "Logger",
                        callingAssembly.GetName().Name,
                        strMessage,
                        "{" + strName + "." + strTitle + " } - ",
                        string.Empty);
                }
                if (blnPublishTopic)
                {
                    LoggerPublisher.PublishLog(strMsgBoddy);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void Log(Exception ex)
        {
            try
            {
                if(ex == null)
                {
                    return;
                }
                Log(ex, false); // at this point I dont need emails, roll this back in the future
            }
            catch (Exception ex2)
            {
                Console.WriteLine(ex2);
            }
        }

        public static void Log(Exception ex, bool blnSendMail)
        {
            try
            {
                Log(ex, blnSendMail, true);
            }
            catch (Exception ex2)
            {
                Console.WriteLine(ex2);
            }
        }

        public static void Log(
            Exception ex, 
            bool blnSendMail,
            bool blnPublishGui)
        {
            try
            {
                Log0(ex, blnSendMail, blnPublishGui, true);
            }
            catch (Exception ex2)
            {
                Console.WriteLine(ex2);
            }
        }

        private static void Log0(
            Exception ex,
            bool blnSendMail,
            bool blnPublishGui,
            bool blnPublishTopic)
        {
            if (ex is ThreadAbortException)
            {
                // ignore it for the time being
                return;
            }

            m_exceptionQueue.EnqueueTask(new LogQueueItem
                {
                    ex = ex,
                    blnSendMail = blnSendMail,
                    blnPublishGui = blnPublishGui,
                    blnPublishTopic = blnPublishTopic
                });
        }

        private static void Log01(
            Exception ex,
            bool blnSendMail,
            bool blnPublishGui,
            bool blnPublishTopic)
        {
            try
            {
                if (ex == null)
                {
                    return;
                }
                //string strTitle = HCException.GetMessageTitle(ex);
                //if (blnSendMail)
                //{
                string strInnerException = string.Empty;
                if (ex.InnerException != null)
                {
                    strInnerException = "InnerException: " +
                                               ex.InnerException.Message +
                    Environment.NewLine +
                    ex.InnerException.StackTrace;
                }
                string strMessage = 
                    HCConfig.ClientUniqueName +
                Environment.NewLine +
                    " ----- Exception: " +
                ex +
                Environment.NewLine +
                "InnerException:" +
                strInnerException +
                Environment.NewLine +
                "Trace:" +
                Environment.NewLine +
                ex.StackTrace +
                Environment.NewLine +
                Environment.NewLine;
                //    MailImpl.SendMessage(
                //        strTitle,
                //        strMessage);
                //}

                Assembly callingAssembly = Assembly.GetCallingAssembly();
                string strName = FileHelper.GetAssemblyName(callingAssembly);
                GetLogger(strName).Write(ex, blnPublishGui);

                if (blnPublishTopic)
                {
                    //string strInnerException = string.Empty;
                    //if (ex.InnerException != null)
                    //{
                    //    strInnerException = "InnerException: " +
                    //                               ex.InnerException.Message +
                    //    Environment.NewLine +
                    //    ex.InnerException.StackTrace;
                    //}
                    //string strErrorMessage =
                    //    HCConfig.ClientUniqueName + " ----- Exception: " +
                    //    ex.Message +
                    //    Environment.NewLine +
                    //    strInnerException +
                    //    Environment.NewLine +
                    //    "Trace:" +
                    //    Environment.NewLine +
                    //    ex.StackTrace +
                    //    Environment.NewLine +
                    //    Environment.NewLine;
                    LoggerPublisher.PublishLog(strMessage);
                }
            }
            catch (Exception ex2)
            {
                Console.WriteLine(ex2);
            }
        }

        public static ILoggerService GetLogger()
        {
            try
            {
                Assembly callingAssembly = Assembly.GetCallingAssembly();
                string strName = FileHelper.GetAssemblyName(callingAssembly);
                return GetLogger(strName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }

        public static ILoggerService GetLogger(string strName)
        {
            try
            {
                return new Log4NetWrapper(strName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }
    }
}