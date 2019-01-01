#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HC.Core.Exceptions;

#endregion

namespace HC.Core.ConfigClasses
{
    public class ConfigConstants : IEnumerable<string>
    {
        #region Properties

        public Dictionary<string, object> ConstantDict { get; private set; }

        #endregion

        #region Constructors

        public ConfigConstants() :
            this(new Dictionary<string, object>())
        {
        }

        public ConfigConstants(
            Dictionary<string, object> constantDict)
        {
            ConstantDict = constantDict;
        }

        #endregion

        #region Public

        public object this[string key]
        {
            get { return ConstantDict[key]; }
        }

        public IEnumerator<string> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Private

        #endregion

        public List<string> GetAllPropertyNames()
        {
            return ConstantDict.Keys.ToList();
        }

        public bool TryGetStrValue(string strPropName, out string strNewVal)
        {
            strNewVal = string.Empty;
            object objVal;
            bool blnResult = ConstantDict.TryGetValue(strPropName, out objVal);
            if (blnResult &&
                objVal != null)
            {
                strNewVal = objVal.ToString();
            }
            return blnResult;
        }

        public void SetStrValue(string strPropName, string strNewVal)
        {
            ConstantDict[strPropName] = strNewVal;
        }

        public void SaveToXml(string strXmlFileName)
        {
            var sb =
                new StringBuilder("<?xml version=" + '"' + "1.0" + '"' + " encoding=" + '"' + "utf-8" + '"' +
                                  "?>")
                    .AppendLine();
            sb.AppendLine("<constants>");
            foreach (KeyValuePair<string, object> kvp in ConstantDict)
            {
                string strPropertyName = kvp.Key.Trim();
                string strPropertyValue = kvp.Value.ToString().Trim();
                sb.AppendLine("<" + strPropertyName + ">");
                sb.AppendLine(strPropertyValue);
                sb.AppendLine("</" + strPropertyName + ">");
            }
            sb.AppendLine("</constants>");
            var strDescr = sb.ToString().Trim();
            if (string.IsNullOrEmpty(strDescr))
            {
                throw new HCException("Null description");
            }
            using (var sw = new StreamWriter(strXmlFileName))
            {
                sw.WriteLine(strDescr);
            }
        }
    }
}


