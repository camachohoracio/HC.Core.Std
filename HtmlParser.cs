#region

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using HC.Core.Comunication.Web;
using HC.Core.Logging;
using HtmlAgilityPack;
using NUnit.Framework;

#endregion

namespace HC.Core
{
    public static class HtmlParser
    {
        public static List<List<HtmlNode>>[] GetDataTablesWithHeaders(
            string strUrl,
            string strRowSeparator = "tr")
        {
            return GetDataTablesHeaders(
                strUrl,
                "table",
                strRowSeparator);
        }

        public static List<List<HtmlNode>>[] GetDataTablesHeaders(
            string strUrl,
            string strLabel,
            string strRowSeparator = "tr")
        {
            string strWebPage = GetWebPage(strUrl);
            List<List<HtmlNode>>[] dts = ParseAllTablesHeaders(
                strWebPage,
                strLabel,
                strRowSeparator);
            return dts;
        }

        public static List<List<HtmlNode>>[] ParseAllTablesHeaders(
            string strWebPage,
            string strLabel,
            string strRowSeparator = "tr")
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(strWebPage);
                var result = new List<List<List<HtmlNode>>>();
                HtmlNode[] tabs = doc.DocumentNode.Descendants(strLabel).ToArray();
                for (int i = 0; i < tabs.Length; i++)
                {
                    HtmlNode table = tabs[i];
                    List<List<HtmlNode>> dt = ParseTableHeader(
                        table,
                        strRowSeparator);
                    if (dt != null && dt.Count > 0)
                    {
                        result.Add(dt);
                    }
                }
                return result.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<List<HtmlNode>>[0];
        }

        private static List<List<HtmlNode>> ParseTableHeader(
            HtmlNode table,
            string strRowSeparator = "tr")
        {
            try
            {
                HtmlNodeCollection rows = table.SelectNodes(strRowSeparator);
                if (rows == null || rows.Count == 0)
                {
                    return null;
                }

                //
                // load titles
                //
                List<string> colValidatorList = AddColumns(rows, new DataTable());

                if (colValidatorList.Count == 0)
                {
                    return null;
                }

                rows = table.SelectNodes("tr|thead");

                var result = new List<List<HtmlNode>>();

                foreach (HtmlNode row in rows)
                {
                    var data = new object[colValidatorList.Count];
                    if (row.SelectNodes("tr|th|td") == null)
                    {
                        continue;
                    }
                    var nodes = row.SelectNodes("tr|th|td").ToArray();

                    var listRow = new List<HtmlNode>();
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        HtmlNode cell = nodes[i];
                        listRow.Add(cell);
                    }
                    result.Add(listRow);
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static string ConvertToHtmlString(string p)
        {
            var sr = new StringReader(p);
            string strLine;
            var sb = new StringBuilder();
            while ((strLine = sr.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(strLine))
                {
                    sb.AppendLine(@"<br/>");
                }
                else
                {
                    bool blnIsBold = strLine.Trim().StartsWith("<IsaTitleFlag>");
                    strLine = strLine
                        .Replace(@"\t", @"&nbsp;")
                        .Replace(" ", "&nbsp;");
                    if (blnIsBold)
                    {
                        strLine = "<strong>" + strLine + "</strong>";
                    }
                    sb.AppendLine(@"<p class=MsoNormal style='mso-margin-top-alt:auto;text-align:justify'>" + strLine + @"</p>");

                }
            }
            return sb.ToString();
        }

        [Test]
        public static void TestWebsie()
        {
            using (var sr = new StreamReader(@"c:\htmlTest.txt"))
            {
                string strWebPage = sr.ReadToEnd();
                DataTable[] dts = ParseAllTables(strWebPage);
                var dt = dts[7];
                Console.WriteLine(dt);
            }
        }
        public static List<List<string>>[] GetDataLists(string strUrl)
        {
            var strWebPage = GetWebPage(strUrl);
            var dts = ParseTableAsList(strWebPage);
            return dts;
        }

        public static DataTable[] GetDataTables(string strUrl)
        {
            return GetDataTables(strUrl, "table");
        }

        public static DataTable[] GetDataTables(string strUrl, string strLabel)
        {
            string strWebPage = GetWebPage(strUrl);
            DataTable[] dts = ParseAllTables(strWebPage, strLabel);
            return dts;
        }
        public static string GetWebPage(
            string strUrl)
        {
            return GetWebPage(strUrl, 0);
        }

        public static string GetWebPage(
            string strUrl,
            int intTimeOutMills)
        {
            while (!WebHelper.IsConnectedToInternet())
            {
                const string strMessage = "Is connected to internet";
                Console.WriteLine(strMessage);
                Logger.Log(strMessage);
                Thread.Sleep(1000);
            }
            HttpWebResponse webResponse = null;
            Stream pageStream = null;
            StreamReader sr = null;
            try
            {
                var webRequest = (HttpWebRequest)WebRequest.Create(strUrl);
                if (intTimeOutMills > 0)
                {
                    webRequest.Timeout = intTimeOutMills;
                }
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                pageStream = webResponse.GetResponseStream();
                if (pageStream == null)
                {
                    return string.Empty;
                }
                sr = new StreamReader(pageStream);
                string strWebPage = sr.ReadToEnd();
                return strWebPage;
            }
            catch (Exception ex)
            {
                //
                // swallow the exception, this may breack when there is no internet connection
                //
                Console.WriteLine("Failed to download website [" + strUrl + "]");
            }
            finally
            {
                if (pageStream != null)
                {
                    pageStream.Close();
                }
                if (sr != null)
                {
                    sr.Close();
                }
                if (webResponse != null)
                {
                    webResponse.Close();
                }
            }
            return string.Empty;
        }

        public static List<List<string>>[] ParseTableAsList(
            string strWebPage)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(strWebPage);
            var result = new List<List<List<string>>>();
            foreach (HtmlNode table in doc.DocumentNode.Descendants("table"))
            {
                var dt = ParseList(table);
                if (dt.Count > 0)
                {
                    result.Add(dt);
                }
            }
            return result.ToArray();
        }

        public static DataTable[] ParseAllTables(
            string strWebPage)
        {
            return ParseAllTables(
                strWebPage,
                "table");
        }

        public static DataTable[] ParseAllTables(
            string strWebPage,
            string strLabel)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(strWebPage);
            var result = new List<DataTable>();
            HtmlNode[] tabs = doc.DocumentNode.Descendants(strLabel).ToArray();
            for (int i = 0; i < tabs.Length; i++)
            {
                HtmlNode table = tabs[i];
                DataTable dt = ParseTable(table);
                if (dt != null && dt.Rows.Count > 0)
                {
                    result.Add(dt);
                }
            }
            return result.ToArray();
        }

        public static string ParseTable(
            string[,] table,
            List<string> titles,
            string[,] colors)
        {
            try
            {
                //<table id="t01">
                //  <tr>
                //    <th>First Name</th>
                //    <th>Last Name</th>		
                //    <th>Points</th>
                //  </tr>
                //  <tr>
                //    <td>Jill</td>
                //    <td>Smith</td>		
                //    <td>50</td>
                //  </tr>
                //  <tr>
                //    <td>Eve</td>
                //    <td>Jackson</td>		
                //    <td>94</td>
                //  </tr>
                //  <tr>
                //    <td>John</td>
                //    <td>Doe</td>		
                //    <td>80</td>
                //  </tr>
                //</table>
                var sb = new StringBuilder();
                //sb.AppendLine("<table border=" + '"' + 1 + '"' + ">");

                sb.AppendLine(
                    "<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0 style=" + "'" +
                    "border-collapse:collapse;border:none;mso-border-alt:solid #D9D9D9 .5pt;mso-border-themecolor:background1;mso-border-themeshade:217;mso-yfti-tbllook:1184;mso-padding-alt:0cm 5.4pt 0cm 5.4pt;mso-border-insideh:.5pt solid #D9D9D9;mso-border-insideh-themecolor:background1;mso-border-insideh-themeshade:217;mso-border-insidev:.5pt solid #D9D9D9;mso-border-insidev-themecolor:background1;mso-border-insidev-themeshade:217" +
                    "'" + ">");

                //"<table class=MsoTableGrid border=1 cellspacing=0 cellpadding=0 style=" + "'" + 
                //"border-collapse:collapse;border:none;mso-border-alt:solid windowtext .5pt; mso-yfti-tbllook:1184;mso-padding-alt:0cm 5.4pt 0cm 5.4pt" +
                //"'" + ">");
                //
                // add titles
                //
                sb.AppendLine("<tr>");
                foreach (var strTitle in titles)
                {
                    sb.AppendLine("<th>" + strTitle + "</th>");
                }
                sb.AppendLine("</tr>");

                //
                // add rows
                //
                for (int i = 0; i < table.GetLength(0); i++)
                {
                    sb.AppendLine("<tr>");
                    for (int j = 0; j < table.GetLength(1); j++)
                    {
                        if (colors != null && !string.IsNullOrEmpty(colors[i, j]))
                        {
                            sb.AppendLine("<td bgcolor=" + '"' + colors[i, j] + '"' + ">" + CleanStringToHtml(table[i, j]) + "</td>");
                        }
                        else
                        {
                            sb.AppendLine("<td>" + table[i, j] + "</td>");
                        }
                    }
                    sb.AppendLine("</tr>");
                }
                sb.AppendLine("</table>");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return string.Empty;
        }

        private static string CleanStringToHtml(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }
            return s.Replace("|", ",");
        }


        public static string ParseTable(
            List<string[]> table,
            List<string> titles,
            string[,] colors)
        {
            try
            {
                //<table id="t01">
                //  <tr>
                //    <th>First Name</th>
                //    <th>Last Name</th>		
                //    <th>Points</th>
                //  </tr>
                //  <tr>
                //    <td>Jill</td>
                //    <td>Smith</td>		
                //    <td>50</td>
                //  </tr>
                //  <tr>
                //    <td>Eve</td>
                //    <td>Jackson</td>		
                //    <td>94</td>
                //  </tr>
                //  <tr>
                //    <td>John</td>
                //    <td>Doe</td>		
                //    <td>80</td>
                //  </tr>
                //</table>
                var sb = new StringBuilder();
                sb.AppendLine("<table border=" + '"' + 3 + '"' + ">");
                //
                // add titles
                //
                sb.AppendLine("<tr>");
                foreach (var strTitle in titles)
                {
                    sb.AppendLine("<th>" + strTitle + "</th>");
                }
                sb.AppendLine("</tr>");

                //
                // add rows
                //
                for (int i = 0; i < table.Count; i++)
                {
                    sb.AppendLine("<tr>");
                    for (int j = 0; j < table[i].Length; j++)
                    {
                        if (colors != null && !string.IsNullOrEmpty(colors[i, j]))
                        {
                            sb.AppendLine("<td bgcolor=" + '"' + colors[i, j] + '"' + ">" + table[i][j] + "</td>");
                        }
                        else
                        {
                            sb.AppendLine("<td>" + table[i][j] + "</td>");
                        }
                    }
                    sb.AppendLine("</tr>");
                }
                sb.AppendLine("</table>");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return string.Empty;
        }

        public static DataTable ParseTable(HtmlNode table)
        {
            var result = new DataTable();

            var rows = table.SelectNodes("tbody/tr");
            if (rows == null)
            {
                rows = table.SelectNodes("tr");
            }
            if (rows == null || rows.Count == 0)
            {
                return null;
            }

            //
            // load titles
            //
            List<string> colValidatorList = AddColumns(rows, result);

            if (colValidatorList.Count == 0)
            {
                return null;
            }

            foreach (HtmlNode row in rows)
            {
                var data = new object[colValidatorList.Count];
                var nodes = row.SelectNodes("th|td").ToArray();
                for (int i = 0; i < nodes.Length; i++)
                {
                    HtmlNode cell = nodes[i];
                    data[i] = cell.InnerText
                        .Replace("&nbsp;", string.Empty);
                }
                result.Rows.Add(data);
            }

            return result;
        }

        private static List<string> AddColumns(HtmlNodeCollection rows, DataTable result)
        {
            var colValidatorList = new List<string>();
            var intColCounter = 0;
            var nodes = rows[0].SelectNodes("th|td");
            if (nodes == null)
            {
                return new List<string>();
            }
            foreach (HtmlNode cell in nodes)
            {
                var strCol = cell.InnerText;
                if (string.IsNullOrEmpty(strCol) ||
                    colValidatorList.Contains(strCol))
                {
                    strCol = strCol + "_" + intColCounter;
                }
                strCol = strCol.Replace("&nbsp;", string.Empty);

                colValidatorList.Add(strCol);
                intColCounter++;
            }

            int intMaxCellCount = (from cell in rows select cell.SelectNodes("th|td").Count).Max();

            for (int i = colValidatorList.Count; i < intMaxCellCount; i++)
            {
                string strCol = "col_" + i;
                colValidatorList.Add(strCol);
            }

            foreach (string strCol in colValidatorList)
            {
                result.Columns.Add(new DataColumn(strCol, typeof(string)));
            }
            return colValidatorList;
        }

        private static List<List<string>> ParseList(HtmlNode table)
        {
            try
            {
                if (table == null)
                {
                    return new List<List<string>>();
                }
                var resultList = new List<List<string>>();
                var rows = table.SelectNodes("tbody/tr");
                if (rows == null)
                {
                    rows = table.SelectNodes("tr");
                }

                if (rows == null)
                {
                    return new List<List<string>>();
                }

                foreach (HtmlNode row in rows)
                {
                    var data = new List<string>();
                    HtmlNodeCollection nodes = row.SelectNodes("th|td");
                    if (nodes == null)
                    {
                        return new List<List<string>>();
                    }
                    foreach (HtmlNode cell in nodes)
                    {
                        data.Add(cell.InnerText);
                    }
                    resultList.Add(data);
                }
                return resultList;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<List<string>>();
        }
    }
}


