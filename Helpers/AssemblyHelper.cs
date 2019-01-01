#region

using System.IO;

#endregion

namespace HC.Core.Helpers
{
    public static class AssemblyHelper
    {
        #region Public

        public static string GetAssemblyName(
            string strAssemblyFileName)
        {
            var fi = new FileInfo(
                strAssemblyFileName);
            var strAssemblyName =
                fi.Name.Replace(
                    fi.Extension,
                    string.Empty);
            return strAssemblyName;
        }

        #endregion
    }
}


