#region

using System.Collections.Generic;
using System.Xml;

#endregion

namespace HC.Core.Io
{
    public static class XmlHelper
    {
        public static List<List<string>> GetNodeList(
            string strCommandCode,
            string strXmlFileName)
        {
            var resultNodeList = new List<List<string>>();
            var doc = new XmlDocument();
            doc.Load(strXmlFileName);
            var exlement = doc.DocumentElement;
            var nodeList = doc.DocumentElement.ChildNodes;
            string strCurrentCommandName;
            foreach (object xmlement_ in nodeList)
            {
                if (xmlement_ is XmlElement)
                {
                    var xmlement = (XmlElement) xmlement_;
                    foreach (XmlAttribute attribute in xmlement.Attributes)
                    {
                        strCurrentCommandName = attribute.Value;

                        if (strCurrentCommandName.Equals(strCommandCode))
                        {
                            foreach (XmlElement childNodes in xmlement.ChildNodes)
                            {
                                var currentNodes = new List<string>();
                                for (var i = 0; i < childNodes.ChildNodes.Count; i++)
                                {
                                    currentNodes.Add(
                                        childNodes.ChildNodes[i].ChildNodes[0].Value);
                                }
                                resultNodeList.Add(currentNodes);
                            }
                        }
                    }
                }
            }
            return resultNodeList;
        }

        public static List<string> GetOneLevelNodeList(
            string strCommandCode,
            string strXmlFileName)
        {
            var resultNodeList = new List<string>();
            var doc = new XmlDocument();
            doc.Load(strXmlFileName);
            var exlement = doc.DocumentElement;
            var nodeList = doc.DocumentElement.ChildNodes;
            foreach (object xmlement_ in nodeList)
            {
                if (xmlement_ is XmlElement)
                {
                    var xmlement = (XmlElement) xmlement_;
                    var strInnerText = xmlement.InnerText;
                    resultNodeList.Add(strInnerText);
                }
            }
            return resultNodeList;
        }
    }
}


