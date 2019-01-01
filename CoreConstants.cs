#region

using System;
using System.IO;
using HC.Core.Io;
using HC.Core.Resources;

#endregion

namespace HC.Core
{
    public static class CoreConstants
    {
        public const string RED = "#FFBBBB"; //"#FAEACB";//;
        public const string GREEN = "#BDF4CB"; //"#E1F8EE";//;

        public const string COMPANY_NAME = "HC";

        public const string DATA_PATH = "Data";

        /// <summary>
        /// Error allowed by the binary search precision.
        /// The smaller the error, the greather the number of iterations 
        /// required by the binary serch algorithm
        /// </summary>
        public const double DBL_BINARY_SEARCH_PRECISION = 1E-3;

        /// <summary>
        /// Number of iterations carried out by binary search
        /// </summary>
        public const int INT_BINARY_SEARCH_ITERATIONS = 100;

        public const IDataRequest SHARED = null;
        public const ulong HWM = 5000; // keep it low, so that the process blocks

        public static string ApplicationDataPath
        {
            get
            {
                string strDefaultPath =
                    Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.ApplicationData),
                        FileHelper.GetCallingAssemblyName());
                if (!DirectoryHelper.Exists(
                    strDefaultPath,
                    false))
                {
                    DirectoryHelper.CreateDirectory(strDefaultPath);
                }
                return strDefaultPath;
            }
        }
    }
}


