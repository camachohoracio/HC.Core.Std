#region

using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace HC.Core.Io.ArrayIo
{
    public static class ArrWriter
    {
        public static void WriteArrListStr(
            string strFileName,
            char chrDelimiter,
            List<string[,]> dblArrList,
            List<string[]> strTitlesList)
        {
            var intRowCount = int.MaxValue;
            for (var i = 0; i < dblArrList.Count; i++)
            {
                if (dblArrList[i].GetLength(0) > intRowCount)
                {
                    intRowCount = dblArrList[i].GetLength(0);
                }
            }

            var intColumnCount = dblArrList[0].GetLength(1);
            using (var sw = new StreamWriter(strFileName))
            {
                //
                // load titles
                //
                if (strTitlesList != null)
                {
                    var strTitlesRow = "";
                    for (var k = 0; k < strTitlesList.Count; k++)
                    {
                        var titles = strTitlesList[k];
                        if (k == 0)
                        {
                            strTitlesRow = titles[0];
                        }
                        for (var j = (k == 0 ? 1 : 0);
                             j < Math.Min(intColumnCount, titles.Length);
                             j++)
                        {
                            strTitlesRow += chrDelimiter + titles[j];
                        }
                    }
                    sw.WriteLine(strTitlesRow);
                }

                //
                // load data rows
                //
                for (var i = 0; i < intRowCount; i++)
                {
                    var strRow = "";
                    for (var k = 0; k < dblArrList.Count; k++)
                    {
                        var dblArr = dblArrList[k];
                        if (i < dblArrList[k].GetLength(0))
                        {
                            if (k == 0)
                            {
                                strRow = dblArr[i, 0];
                            }
                            for (var j = (k == 0 ? 1 : 0); j < intColumnCount; j++)
                            {
                                strRow += chrDelimiter + dblArr[i, j];
                            }
                        }
                    }
                    sw.WriteLine(strRow);
                }
                sw.Close();
            }
        }

        public static void WriteArrListDbl(
            string strFileName,
            char chrDelimiter,
            List<double[,]> dblArrList,
            List<string[]> strTitlesList)
        {
            var intRowCount = dblArrList[0].GetLength(0);
            var intColumnCount = dblArrList[0].GetLength(1);
            using (var sw = new StreamWriter(strFileName))
            {
                //
                // load titles
                //
                if (strTitlesList != null)
                {
                    var strTitlesRow = "";
                    for (var k = 0; k < strTitlesList.Count; k++)
                    {
                        var titles = strTitlesList[k];
                        if (k == 0)
                        {
                            strTitlesRow = titles[0];
                        }
                        for (var j = (k == 0 ? 1 : 0); j < intColumnCount; j++)
                        {
                            strTitlesRow += chrDelimiter + titles[j];
                        }
                    }
                    sw.WriteLine(strTitlesRow);
                }

                //
                // load data rows
                //
                for (var i = 0; i < intRowCount; i++)
                {
                    var strRow = "";
                    for (var k = 0; k < dblArrList.Count; k++)
                    {
                        var dblArr = dblArrList[k];
                        if (k == 0)
                        {
                            strRow = dblArr[i, 0].ToString();
                        }
                        for (var j = (k == 0 ? 1 : 0); j < intColumnCount; j++)
                        {
                            strRow += chrDelimiter + dblArr[i, j].ToString();
                        }
                    }
                    sw.WriteLine(strRow);
                }
                sw.Close();
            }
        }

        public static void WriteArr(
            string strFileName,
            char chrDelimiter,
            double[,] dblArr)
        {
            var dblArrList = new List<double[,]>();
            dblArrList.Add(dblArr);
            WriteArrListDbl(
                strFileName,
                chrDelimiter,
                dblArrList,
                null);
        }

        public static void WriteArrSpace(
            string strFileName,
            double[,] dblArr)
        {
            WriteArr(strFileName, ' ', dblArr);
        }
    }
}


