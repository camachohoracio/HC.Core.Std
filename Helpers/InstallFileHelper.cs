#region

using System.IO;

#endregion

namespace HC.Core.Helpers
{
    public static class InstallFileHelper
    {
        public static bool CheckFirstTimeInstaller(
            string strAppFileName)
        {
            var blnFlag = false;
            using (var sr =
                new StreamReader(
                    strAppFileName))
                //"InstallFlag.txt"))
            {
                var strLine = sr.ReadLine();
                if (strLine.Equals("true"))
                {
                    blnFlag = true;
                }
                sr.Close();
            }
            return blnFlag;
        }

        public static void SetFirstTimeInstaller(string strSetupAppFileName)
        {
            using (var sw =
                new StreamWriter(strSetupAppFileName))
            {
                sw.WriteLine("false");
                sw.Close();
            }
        }
    }
}


