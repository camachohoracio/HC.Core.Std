using System;
using System.Configuration;

namespace HC.Core.Logging
{
    public static class Configs
    {
        public static string Get(string strConfig)
        {
            try
            {
                return ConfigurationManager.AppSettings.Get(strConfig);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return string.Empty;
        }
    }
}



