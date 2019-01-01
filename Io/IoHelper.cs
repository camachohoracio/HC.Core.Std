#region

using System;
using System.IO;
using System.Reflection;

#endregion

namespace HC.Core.Io
{
    public static class IoHelper
    {
        public static string GetAssemblyPath(Type type)
        {
            try
            {
                string strPath = GetAssemblyFullName(type);
                return strPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return string.Empty;
        }

        private static string ParseAssemblyName(string strAssemblyName)
        {
            string strPath = Path.GetDirectoryName(
                strAssemblyName);
            if (DirectoryHelper.Exists(strPath))
            {
                return strPath;
            }
            strPath = strPath.Substring(6, strPath.Length - 6);
            return strPath;
        }

        public static string GetCurrentAssemblyPath()
        {
            try
            {
                string strAssemblyName = GetAssemblyFullName();
                string strPath = ParseAssemblyName(strAssemblyName);
                return strPath;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            return string.Empty;
        }

        public static string GetAssemblyName()
        {
            var assemblyFileInfo = new FileInfo(
                Assembly.GetExecutingAssembly().Location);
            return assemblyFileInfo.Name;
        }

        public static string GetAssemblyFullName()
        {
            return GetAssemblyFullName(Assembly.GetExecutingAssembly());
        }

        public static string GetAssemblyFullName(Assembly assembly)
        {
            return assembly.CodeBase.Replace(@"file:///", string.Empty);
        }

        public static string GetAssemblyFullName(Type type)
        {
            return GetAssemblyFullName(Assembly.GetAssembly(type));
        }

        public static string GetCurrentPath()
        {
            return GetCurrentAssemblyPath();
        }
    }
}


