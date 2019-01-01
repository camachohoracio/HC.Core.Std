#region

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using HC.Core.Helpers;
using HC.Core.Logging;

#endregion

namespace HC.Core.Comunication.Web
{
    public class WebClientWrapper : WebClient
    {
        #region Members

        private readonly int m_intTimeoutMills;

        #endregion

        #region Constructors

        public WebClientWrapper() : this(0)
        {
        }

        public WebClientWrapper(int intTimeoutMills)
        { 
            m_intTimeoutMills = intTimeoutMills;
        }

        #endregion

        public static string DownloadWebsite(
            string strUrl)
        {
            return DownloadWebsite(strUrl, 0, 0);
        }

        public static string DownloadWebsite(
            string strUrl,
            int intTimeoutMills,
            int intMaxTrials)
        {
            int intTrials = 0;
            return DownloadWebsite(strUrl, intTimeoutMills, intMaxTrials, ref intTrials);
        }
        
        public static string DownloadWebsite(
            string strUrl,
            int intTimeoutMills,
            int intMaxTrials,
            ref int intTrials)
        {
            while (!WebHelper.IsConnectedToInternet())
            {
                const string strMessage = "No internet connection";
                Console.WriteLine(strMessage);
                Logger.Log(strMessage);
                Thread.Sleep(3000);
            }
            string strHistory = string.Empty;

            WebClientWrapper webClient;

            if (intTimeoutMills > 0)
            {
                webClient = new WebClientWrapper(intTimeoutMills);
            }
            else
            {
                webClient = new WebClientWrapper();
            }
            try
            {
                strHistory = webClient.DownloadString(strUrl);
            }
            catch (Exception ex)
            {
                //
                // no need to log it
                //
                Console.WriteLine(
                    "Failed download [" + strUrl + 
                    "] Trials [" + intTrials + "]/[" +
                    intMaxTrials + "]");
                Console.WriteLine(ex);

                intTrials++;
                if (intTrials < intMaxTrials)
                {
                    return DownloadWebsite(strUrl,
                        intTimeoutMills,
                        intMaxTrials,
                        ref intTrials);
                }
                else
                {
                    //
                    // too many trials, give it up
                    //
                    return string.Empty;
                }
            }
            finally
            {
                webClient.Dispose();
            }
            return strHistory;
        }
        
        protected override WebRequest GetWebRequest(Uri address)
        {
            var result = base.GetWebRequest(address);
            if (result == null)
            {
                return null;
            }            
            if (m_intTimeoutMills > 0)
            {
                result.Timeout = m_intTimeoutMills;
            }
            return result;
        }

        public new string DownloadString(
            string strUrl)
        {
            try
            {
                while (!WebHelper.IsConnectedToInternet())
                {
                    string strMessage = GetType().Name + " is not connected to internet";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);
                    Thread.Sleep(1000);
                }
                return base.DownloadString(strUrl);
            }
            catch
            {
                Console.WriteLine("Faild to download website [" +
                    strUrl + "]");
            }
            return string.Empty;
        }

        public string DownloadString(
            string strUrl,
            int intTimeOutMills)
        {
            HttpWebRequest webRequest = null;
            HttpWebResponse webResponse = null;
            StreamReader responseStream = null;

            try
            {
                webRequest = (HttpWebRequest) WebRequest.Create(strUrl);
                webRequest.UseDefaultCredentials = true;
                if (intTimeOutMills > 0)
                {
                    webRequest.Timeout = intTimeOutMills;
                }
                var encode = Encoding.GetEncoding("utf-8");
                try
                {
                    using(webResponse = (HttpWebResponse) webRequest.GetResponse())
                    {
                        Stream stream = webResponse.GetResponseStream();
                        if (stream == null)
                        {
                            return string.Empty;
                        }
                        responseStream = new StreamReader(stream, encode);
                        string strHistory = responseStream.ReadToEnd();

                        if (string.IsNullOrEmpty(strHistory))
                        {
                            return string.Empty;
                        }
                        return strHistory;
                    }
                }
                catch
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                PrintToScreen.WriteLine(ex);
                Logger.Log(ex, false);
            }
            finally
            {
                if (webRequest != null)
                {
                    webRequest.Abort();
                }
                if (responseStream != null)
                {
                    responseStream.Close();
                    responseStream.Dispose();
                }

                if (webResponse != null)
                {
                    webResponse.Close();
                }
                Dispose();
            }
            return string.Empty;
        }
    }
}



