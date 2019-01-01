using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HC.Core.Logging;
using NUnit.Framework;

namespace HC.Core.Comunication.Web
{
    public static class WebHelper
    {
        [DllImport("wininet.dll")]
        private extern static bool InternetCheckConnection(String url , int flag, int ReservedValue);

        private static DateTime m_lastTimechecked;
        private static bool m_blnIsConnected;
        private static readonly object m_checkLock = new object();
        private const string WEBSITE = "http://www.google.com";

        private static readonly string strRegexPattern =
            "<\\b(https?|ftp|file)://[-a-zA-Z0-9+&@#/%?=~_|!:,.;]*[-a-zA-Z0-9+&@#/%=~_|]>";
            //@"(?i)\b((?:[a-z][\w-]+:(?:/{1,3}|[a-z0-9%])|www\d{0,3}[.]|[a-z0-9.\-]+[.][a-z]{2,4}/)(?:[^\s()<>]+|\(([^\s()<>]+|(\([^\s()<>]+\)))*\))+(?:\(([^\s()<>]+|(\([^\s()<>]+\)))*\)|[^\s`!()\[\]{};:'" +
            //'"' + ".,<>?«»“”‘’]))";

        public static string[] ExtractUrls(string str0)
        {
            try
            {
                if(string.IsNullOrEmpty(str0))
                {
                    return new string[0];
                }

                string[] toks = str0.Split(' ');
                var matchList = new List<string>();
                foreach (string str in toks)
                {
                    MatchCollection matches = Regex.Matches(
                        str,
                        strRegexPattern,
                        RegexOptions.IgnoreCase);

                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            if (match.Success)
                            {
                                foreach (var capture in match.Captures)
                                {
                                    string strUrl = capture.ToString();
                                    matchList.Add(strUrl);
                                }
                            }
                        }
                    }
                }
                return matchList.ToArray();
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new string[0];
        }


        [Test]
        public static bool IsConnectedToInternet()
        {
            if ((DateTime.Now - m_lastTimechecked).TotalSeconds > 5)
            {
                lock (m_checkLock)
                {
                    if ((DateTime.Now - m_lastTimechecked).TotalSeconds > 5)
                    {
                        m_blnIsConnected = InternetCheckConnection(WEBSITE, 1, 0);

                        if(m_blnIsConnected)
                        {
                            using (var ww = new WebClient())
                            {
                                try
                                {
                                    var strHistory = ww.DownloadString(WEBSITE);
                                    var lowerHistory = strHistory.ToLower();
                                    m_blnIsConnected = lowerHistory.Contains("google") &&
                                                       !lowerHistory.Contains("this webpage is not available") &&
                                                       !lowerHistory.Contains("unable to access the network");
                                }
                                catch(Exception ex)
                                {
                                    //
                                    // do not log the exception
                                    //
                                    Console.WriteLine(ex);
                                    m_blnIsConnected = false;
                                }
                            }
                        }

                        m_lastTimechecked = DateTime.Now;
                    }
                }
            }
            return m_blnIsConnected;
        }
    }
}



