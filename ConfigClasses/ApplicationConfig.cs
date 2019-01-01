#region

using System;

#endregion

namespace HC.Core.ConfigClasses
{
    internal class ApplicationConfig
    {
        static ApplicationConfig()
        {
            ConfigDirectory = AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string ConfigDirectory { get; set; }
    }
}


