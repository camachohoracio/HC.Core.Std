#region

using System;
using System.Collections.Generic;
using System.Reflection;
using HC.Core.Comunication;
using HC.Core.ConfigClasses;
using HC.Core.Io;
using HC.Core.Logging;

#endregion

namespace HC.Core
{
    public static class Config
    {
        #region Members

        private static readonly string m_strServerName;

        #endregion

        #region Constructors

        static Config()
        {
            try
            {
                m_strServerName = HCConfig.GetConstant<string>(
                    "TopicServerName",
                    typeof(Config));
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static List<string> GetEmailToList()
        {
            return HCConfig.GetConfigList(
                "EmailToList",
                typeof(Config));
        }

        #endregion

        #region Public

        public static string GetWebProxyNameNewsData()
        {
            return HCConfig.GetConstant<string>(
                "WebProxyNameNewsData",
                typeof(Config));
        }

        public static int GetWebProxyPortNewsData()
        {
            return HCConfig.GetConstant<int>(
                "WebProxyPortNewsData",
                typeof(Config));
        }

        public static int GetConnectionsAvailableNewsData()
        {
            return HCConfig.GetConstant<int>(
                "ConnectionsAvaiableNewsData",
                typeof(Config));
        }


        public static List<string> GetContinents()
        {
            return HCConfig.GetConfigList(
                "Continents",
                typeof(Config));
        }

        public static Dictionary<string, string> GetCountryLookup()
        {
            List<string> countryList =
                HCConfig.GetConfigList(
                    "CountryLookup",
                    typeof(Config));

            var countryLookup =
                new Dictionary<string, string>();
            foreach (string strCountry in countryList)
            {
                string[] tokens = strCountry.Split(',');
                countryLookup[
                    tokens[0].Trim()] = tokens[1].Trim();
            }
            return countryLookup;
        }

        public static List<string> GetKnownTypes()
        {
            return HCConfig.GetConfigList(
                "KnownTypes",
                typeof(Config));
        }

        public static Dictionary<string, List<string>> GetKnownAssembliesMap()
        {
            List<string> list = HCConfig.GetConfigList(
                "KnownAssemblies",
                typeof(Config));
            var mapAssemblyToLocations =
                new Dictionary<string, List<string>>();
            foreach (string strItem in list)
            {
                if(string.IsNullOrEmpty(strItem))
                {
                    continue;
                }
                string[] tokens = strItem.Split(',');
                var locationList = new List<string>();
                mapAssemblyToLocations[tokens[0].ToLower().Trim()] = locationList;
                if(tokens.Length > 1)
                {
                    for (int i = 1; i < tokens.Length; i++)
                    {
                        string strCurrLoc = tokens[i];
                        if(string.IsNullOrEmpty(strCurrLoc) ||
                            !DirectoryHelper.Exists(strCurrLoc))
                        {
                            if(DirectoryHelper.IsANetWorkDir(strCurrLoc))
                            {
                                if(!NetworkHelper.IsNetworkAvailable())
                                {
                                    strCurrLoc = DirectoryHelper.RemoveNetWorkDir(strCurrLoc);
                                }
                            }
                            if (string.IsNullOrEmpty(strCurrLoc) ||
                                !DirectoryHelper.Exists(strCurrLoc))
                            {
                                continue;
                            }
                        }

                        locationList.Add(strCurrLoc);
                    }
                }
            }
            return mapAssemblyToLocations;
        }

        public static string GetTopicInterface()
        {
            return HCConfig.GetConstant<string>(
                "TopicInterface",
                typeof(Config));
        }

        public static string GetEmailName()
        {
            return HCConfig.GetConstant<string>(
                "EmailName",
                typeof(Config));
        }

        public static string GetEmailPsw()
        {
            return HCConfig.GetConstant<string>(
                "EmailPsw",
                typeof(Config));
        }

        public static string GetEmailHost()
        {
            return HCConfig.GetConstant<string>(
                "EmailHost",
                typeof(Config));
        }

        public static int GetEmailPort()
        {
            return HCConfig.GetConstant<int>(
                "EmailPort",
                typeof(Config));
        }

        internal static string GetEmailAddr()
        {
            return HCConfig.GetConstant<string>(
                "DefaultCacheDataPath",
                typeof(Config));
        }

        public static List<string> GetLookupPaths()
        {
            return HCConfig.GetConfigList(
                "LookupPaths",
                typeof(Config));
        }
        

        public static string GetDefaultCacheDataPath()
        {
            return HCConfig.GetConstant<string>(
                "DefaultCacheDataPath",
                typeof(Config));
        }

        public static string[] GetServerNames()
        {
            return HCConfig.GetConfigList("Servers", typeof (Config)).ToArray();
        }

        public static List<string> GetReservedWords()
        {
            return HCConfig.GetConfigList(
                "ReservedWords",
                typeof(Config));
        }

        public static string GetDbSerializerName()
        {
            return HCConfig.GetConstant<string>(
                "DbSerializerName",
                typeof(Config));
        }

        public static string GetSelfDescribingClassSchemaDir()
        {
            string strDir = HCConfig.GetConstant<string>(
                "SelfDescribingClassSchemaDir",
                typeof(Config));

            if (!DirectoryHelper.Exists(strDir))
            {
                DirectoryHelper.CreateDirectory(strDir);
            }
            return strDir;
        }

        public static string GetUiParamsPath()
        {
            string strDir = HCConfig.GetConstant<string>(
                "UiParamsPath",
                typeof(Config));

            if (!DirectoryHelper.Exists(strDir))
            {
                DirectoryHelper.CreateDirectory(strDir);
            }
            return strDir;
        }

        public static string GetLiveGridServiceTopic()
        {
            return HCConfig.GetConstant<string>(
               "LiveGridServiceTopic",
               typeof(Config));
        }

        public static string GetLiveGridClientTopic()
        {
            return HCConfig.GetConstant<string>(
               "LiveGridClientTopic",
               typeof(Config));
        }

        public static string GetTopicServerAddr()
        {
            return HCConfig.GetConstant<string>(
                "TopicServerAddr",
                typeof(Config));
        }

        public static string GetTopicServerName()
        {
            return m_strServerName;
        }

        public static string GetTopicPublisher(string strServerName)
        {
            string strTopicPublisher =
                HCConfig.GetConstant<string>(
                    "TopicPublisher",
                    typeof(Config));
            return strTopicPublisher.Replace("[server]", strServerName);
        }

        public static string GetTopicSubscriber(string strServerName)
        {
            string strTopicPublisher =
                HCConfig.GetConstant<string>(
                    "TopicSubscriber",
                    typeof(Config));
            return strTopicPublisher.Replace("[server]", strServerName);
        }

        public static int GetCalcThreads()
        {
            return HCConfig.GetConstant<int>(
                "CalcThreads",
                typeof(Config));
        }

        public static Dictionary<string, string> GetDataProviderToPathMap()
        {
            List<string> list = HCConfig.GetConfigList(
                "DataProviderToPath",
                typeof(Config));
            var map = new Dictionary<string, string>();
            for (int i = 0; i < list.Count; i++)
            {
                string[] tokens = list[i].Split(',');
                map[tokens[0].Trim()] = tokens[1].Trim();
            }
            return map;
        }

        #endregion

        public static string GetCalcTopic(Assembly assembly)
        {
            return HCConfig.GetConstant<string>(
                "CalcTopic",
                assembly);
        }

        public static List<string> GetCalcTypes()
        {
            return HCConfig.GetConfigList(
                "CalcTypes",
                typeof(Config));
        }

        internal static string GetAssemblyCache()
        {
            return HCConfig.GetConstant<string>(
                "assembly_cache",
                typeof(Config));
        }

        public static int GetReqRespPort()
        {
            return HCConfig.GetConstant<int>(
                "ReqRespPort",
                typeof(Config));
        }

        public static int GetIntradayReqRespPort()
        {
            return HCConfig.GetConstant<int>(
                "IntradayReqRespPort",
                typeof(Config));
        }

        public static int GetReqRespConnections()
        {
            return HCConfig.GetConstant<int>(
                "ReqRespConnections",
                typeof(Config));
        }

        public static int GetGuiReqRespPort()
        {
            return HCConfig.GetConstant<int>(
                "GuiReqRespPort",
                typeof(Config));
        }

        public static int GetGuiReqRespConnections()
        {
            return HCConfig.GetConstant<int>(
                "GuiReqRespConnection",
                typeof(Config));
        }

        public static string GetGuiServerName()
        {
            return HCConfig.GetConstant<string>(
                "GuiServerName",
                typeof(Config));
        }

        public static string GetResultsDataPath()
        {
            return HCConfig.GetConstant<string>(
                "ResultsDataPath",
                typeof(Config));
        }

        public static string GetDataServerName()
        {
            return HCConfig.GetConstant<string>(
                "DataServerName",
                typeof(Config));
        }

        public static string GetIntradayDataServerName()
        {
            return HCConfig.GetConstant<string>(
                "IntradayDataServerName",
                typeof(Config));
        }

        public static int GetDbOpenConnections()
        {
            return HCConfig.GetConstant<int>(
                "DbOpenConnections",
                typeof(Config));
        }

        public static int GetDbReadThreadSize()
        {
            return HCConfig.GetConstant<int>(
                "DbReadThreadSize",
                typeof(Config));
        }

        public static string GetPriceServerName()
        {
            return HCConfig.GetConstant<string>(
                "PriceServerName",
                typeof(Config));
        }

        public static int GetPriceReqRespPort()
        {
            return HCConfig.GetConstant<int>(
                "PriceReqRespPort",
                typeof(Config));
        }

        public static int GetPriceReqRespConnections()
        {
            return HCConfig.GetConstant<int>(
                "PriceReqRespConnection",
                typeof(Config));
        }

        public static List<string> GetIntradayProviders()
        {
            return HCConfig.GetConfigList(
                "IntradayProviders",
                typeof(Config));
        }

        public static string GetDefaultIntradayCacheDataPath()
        {
            return HCConfig.GetConstant<string>(
                "DefaultIntradayCacheDataPath",
                typeof(Config));
        }
    }
}