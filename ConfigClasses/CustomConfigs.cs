#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using HC.Core.Io;
using HC.Core.Logging;
using HC.Core.Threading;

#endregion

namespace HC.Core.ConfigClasses
{
    public static class CustomConfigs
    {
        #region Members

        private static readonly ConcurrentDictionary<string, object> m_filesChecked =
            new ConcurrentDictionary<string, object>();

        #endregion

        public static void LoadCustomConfigs(
            string strXmlFileName)
        {
            object dummyVal;
            if (m_filesChecked.TryGetValue(strXmlFileName, out dummyVal))
            {
                return;
            }
            lock (LockObjectHelper.GetLockObject(strXmlFileName + "_" +
                typeof(CustomConfigs).Name))
            {
                try
                {
                    if (m_filesChecked.TryGetValue(strXmlFileName, out dummyVal))
                    {
                        return;
                    }

                    if (!FileHelper.Exists(strXmlFileName))
                    {
                        return;
                    }

                    var fi = new FileInfo(strXmlFileName);

                    string strDirName = fi.DirectoryName;
                    if (strDirName == null)
                    {
                        return;
                    }
                    string strCustomXmlFileName =
                        Path.Combine(
                            strDirName,
                            HCConfig.DnsName);
                    strCustomXmlFileName =
                        Path.Combine(
                            strCustomXmlFileName,
                            fi.Name);

                    if (!FileHelper.Exists(strCustomXmlFileName))
                    {
                        return;
                    }

                    var paramsClass = HCConfig.GetConfigConstants(
                        strXmlFileName,
                        false);

                    var paramsClassCustom = HCConfig.GetConfigConstants(
                        strCustomXmlFileName,
                        false);
                    List<string> propNames = paramsClassCustom.GetAllPropertyNames();
                    bool blnAddValue = false;
                    for (int i = 0; i < propNames.Count; i++)
                    {
                        string strPropName = propNames[i];

                        string strNewVal;
                        if (paramsClassCustom.TryGetStrValue(strPropName, out strNewVal))
                        {
                            string strCurrVal;
                            if (!paramsClass.TryGetStrValue(strPropName, out strCurrVal) ||
                                string.IsNullOrEmpty(strCurrVal) ||
                                !strNewVal.Equals(strCurrVal))
                            {
                                paramsClass.SetStrValue(strPropName, strNewVal);
                                blnAddValue = true;
                            }
                        }
                    }
                    if (blnAddValue)
                    {
                        paramsClass.SaveToXml(strXmlFileName);
                    }
                    Logger.Log("Config [" + 
                        strXmlFileName + "] has been updated by config[" +
                        strCustomXmlFileName + "]");
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
                finally
                {
                    m_filesChecked[strXmlFileName] = null;
                }
            }
        }
    }
}