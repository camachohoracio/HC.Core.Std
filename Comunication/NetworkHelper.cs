using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using HC.Core.ConfigClasses;
using HC.Core.Logging;
using NUnit.Framework;

namespace HC.Core.Comunication
{
    public static class NetworkHelper
    {
        public static bool IsADistWorkerConnected { get; set; }

        public const string LOOP_BACK_IP = "127.0.0.1";

        public static string CurrentIp { get; private set; }
        
        static NetworkHelper()
        {
            CurrentIp = GetIpAddr(HCConfig.DnsName);
        }

        public static bool IsNetworkAvailable()
        {
            try
            {
                return NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
                Logger.Log("Network is not available");
            }
            return false;
        }

        public static string GetIpAddr(string strDns)
        {
            try
            {
                if (string.IsNullOrEmpty(strDns))
                {
                    return string.Empty;
                }
                if(!IsNetworkAvailable())
                {
                    return LOOP_BACK_IP;
                }
                const int TRIALS = 2;
                string strIp;
                int intTrials = 0;
                while (!TryGetIp(strDns, out strIp))
                {
                    string strMessage = "Failed generating ip for [" +
                                        strDns + "] IsNetworkAvailable [" +
                                        IsNetworkAvailable() + "]. Trials [" +
                                        intTrials + "]/[" +
                                        TRIALS + "]";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);
                    Thread.Sleep(1000);
                    if (intTrials > TRIALS)
                    {
                        strIp = LOOP_BACK_IP;
                        return strIp;
                    }
                    intTrials++;
                }

                return strIp;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Logger.Log(ex);
            }
            return strDns;
        }

        [Test]
        public static void Test()
        {
            string strIpAddr = GetIpAddr("horacio-fuji");
            Console.WriteLine(strIpAddr);
        }



        [DebuggerHidden]
        private static bool TryGetIp(string strDns,
                                     out string strIp)
        {
            try
            {
                if(strDns.ToLower().Equals("localhost"))
                {
                    strDns = Dns.GetHostName();
                }
                IPHostEntry host = Dns.GetHostEntry(strDns);
                List<IPAddress> ipList = (from n in host.AddressList
                              where n.AddressFamily == AddressFamily.InterNetwork
                              select n).ToList();
                strIp = ipList.Last().ToString();
                return true;
            }
            catch
            {
                Console.WriteLine("Failed to generate IP for DNS [" +
                    strDns + "]");
                //
                // swallow exception!
                //
                //Logger.Log(ex);
            }
            strIp = null;
            return false;
        }

        public static bool IsLoopBackIp(string strIp)
        {
            return strIp.Equals(LOOP_BACK_IP);
        }
    }
}