#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using HC.Core.Exceptions;
using HC.Core.Helpers;
using HC.Core.Logging;
using HC.Core.Text;

#endregion

namespace HC.Core.Io.ArrayIo
{
    [Serializable]
    public class ArrReader
    {
        #region Events

        #region Delegates

        public delegate void ReadLine(string[] strTokens);

        #endregion

        public event ReadLine OnReadLine;

        #endregion

        #region Members

        private readonly bool m_blnContainsTitles;
        private readonly char m_chrDelimiter;
        private readonly List<string[]> m_strDataArr;
        private readonly string m_strFileName;
        private readonly string[] m_strTitles;
        private Dictionary<string, int> m_titlesDict;

        #endregion

        #region Properties

        public int Count
        {
            get { return m_strDataArr.Count; }
        }

        #endregion

        #region Constructors

        public ArrReader(
            string strFileName,
            char chrDelimiter,
            string[] strTitlesArr)
        {
            m_strTitles = strTitlesArr;
            m_strFileName = strFileName;
            m_chrDelimiter = chrDelimiter;
            LoadTitlesLookup();
        }

        public ArrReader(
            string strFileName,
            char chrDelimiter,
            bool blnContainsTitles)
        {
            if (blnContainsTitles)
            {
                using (var sr = new StreamReader(strFileName))
                {
                    var strLine = sr.ReadLine();
                    m_strTitles = strLine.Split(chrDelimiter);
                }
            }
            m_strFileName = strFileName;
            m_chrDelimiter = chrDelimiter;
            m_blnContainsTitles = blnContainsTitles;
            LoadTitlesLookup();
        }

        #endregion

        #region Private

        private void LoadTitlesLookup()
        {
            if (m_strTitles != null)
            {
                m_titlesDict = new Dictionary<string, int>();
                for (var i = 0; i < m_strTitles.Length; i++)
                {
                    m_titlesDict.Add(
                        m_strTitles[i].ToLower().Trim(),
                        i);
                }
            }
        }

        #endregion

        #region Public

        public static TokenWrapper[][] TokenizeLine(
            int[] intColumns,
            string strLine,
            char delimiter,
            int intColumnCount,
            string[] strStopWordsArr)
        {
            var strTmpToks = strLine.Split(delimiter);
            var strColArr = new TokenWrapper[strTmpToks.Length];
            for (var i = 0; i < strTmpToks.Length; i++)
            {
                strColArr[i] = new TokenWrapper(strTmpToks[i]);
            }
            var intCurrentColumnCount = strColArr.Length;
            //
            // add untokenized id row
            //
            var newRowArr = new TokenWrapper[intColumnCount][];
            newRowArr[0] = new TokenWrapper[1];
            newRowArr[0][0] = strColArr[
                intColumns[0]];
            //
            // tokenize the rest of the column
            //
            for (var intColumn = 0;
                 intColumn < Math.Min(
                     intColumnCount,
                     intColumns.Length);
                 intColumn++)
            {
                // 
                // get column id
                //
                var intColumnId = intColumns[intColumn];

                // tokenize current field
                var tokenArr =
                    Tokeniser.TokeniseAndWrap(
                        strColArr[intColumnId].Token,
                        strStopWordsArr);

                newRowArr[intColumn] = tokenArr;
            }

            for (var i = intCurrentColumnCount; i < intColumnCount; i++)
            {
                newRowArr[i] = new TokenWrapper[1];
                newRowArr[i][0] = new TokenWrapper("");
            }
            return newRowArr;
        }

        public void StartReading()
        {
            var intColumnCount = FileHelper.CountNumberOfColumns(
                m_strFileName,
                m_chrDelimiter);
            using (var sr = new StreamReader(m_strFileName))
            {
                string strLine;
                var intRowCounter = 0;
                //
                // go to next line if file contains titles
                //
                if (m_blnContainsTitles)
                {
                    sr.ReadLine();
                }
                while ((strLine = sr.ReadLine()) != null)
                {
                    var strTokenArray = strLine.Split(m_chrDelimiter);
                    InvokeOnReadLine(strTokenArray);
                    intRowCounter++;
                }
                sr.Close();
            }
        }

        public void SortItemsBy(string strColumnName)
        {
            m_strDataArr.Sort(
                new ArrReaderComparator(m_titlesDict[strColumnName]));
        }

        public string GetItem(
            string[] strTokens,
            string strColumnName)
        {
            var intCol = m_titlesDict[strColumnName.ToLower().Trim()];
            return strTokens[intCol];
        }

        public string GetItem(
            string[] strTokens,
            int intCol)
        {
            return strTokens[intCol];
        }

        public string GetItem(int intRow, string strColumnName)
        {
            var intCol = m_titlesDict[strColumnName];
            return m_strDataArr[intRow][intCol];
        }

        public string GetItem(int intRow, int intColumn)
        {
            return m_strDataArr[intRow][intColumn];
        }

        /// <summary>
        ///   Read file and fill an array of double
        /// </summary>
        /// <param name = "strFileName"></param>
        /// <param name = "chrDelimiter"></param>
        /// <returns></returns>
        public static double[,] LoadFileDbl(
            string strFileName,
            char chrDelimiter)
        {
            var intColumnCount = FileHelper.CountNumberOfColumns(
                strFileName,
                chrDelimiter);
            var intRowCount = (int) FileHelper.CountNumberOfRows(strFileName);
            var dblDataArray = new double[intRowCount,intColumnCount];
            using (var sr = new StreamReader(strFileName))
            {
                string strLine;
                var intRowCounter = 0;
                while ((strLine = sr.ReadLine()) != null)
                {
                    var strTokenArray = strLine.Split(chrDelimiter);
                    for (var i = 0; i < intColumnCount; i++)
                    {
                        dblDataArray[intRowCounter, i] = Convert.ToDouble(strTokenArray[i]);
                    }
                    intRowCounter++;
                }
                sr.Close();
            }
            return dblDataArray;
        }

        public static string[,] LoadFileStr(
            string strFileName,
            char chrDelimiter)
        {
            return LoadFileStr(
                strFileName,
                chrDelimiter,
                false);
        }

        public static List<string[]> LoadStrArrList(
            string strFileName,
            char chrDelimiter,
            bool blnContainsTitles)
        {
            var intColumnCount = FileHelper.CountNumberOfColumns(
                strFileName,
                chrDelimiter);
            var list = new List<string[]>();
            using (var sr = new StreamReader(strFileName))
            {
                string strLine;
                var intRowCounter = 0;
                //
                // go to next line if file contains titles
                //
                if (blnContainsTitles)
                {
                    sr.ReadLine();
                }
                while ((strLine = sr.ReadLine()) != null)
                {
                    var strTokenArray = strLine.Split(chrDelimiter);
                    list.Add(strTokenArray);
                    intRowCounter++;
                }
                sr.Close();
            }
            return list;
        }

        public static string[,] LoadFileStr(
            string strFileName,
            char chrDelimiter,
            bool blnContainsTitles)
        {
            var intColumnCount = FileHelper.CountNumberOfColumns(
                strFileName,
                chrDelimiter);
            var intRowCount = (int) FileHelper.CountNumberOfRows(strFileName);
            var dblDataArray = new string[intRowCount,intColumnCount];
            using (var sr = new StreamReader(strFileName))
            {
                string strLine;
                var intRowCounter = 0;
                //
                // go to next line if file contains titles
                //
                if (blnContainsTitles)
                {
                    sr.ReadLine();
                }
                while ((strLine = sr.ReadLine()) != null)
                {
                    var strTokenArray = strLine.Split(chrDelimiter);
                    for (var i = 0; i < intColumnCount; i++)
                    {
                        dblDataArray[intRowCounter, i] = strTokenArray[i];
                    }
                    intRowCounter++;
                }
                sr.Close();
            }
            return dblDataArray;
        }

        public static string[,] LoadSpaceFileStr(string strFileName)
        {
            return LoadFileStr(strFileName, ' ');
        }

        public static double[,] LoadSpaceFileDbl(string strFileName)
        {
            return LoadFileDbl(strFileName, ' ');
        }


        public static double[,] LoadCommaFile(string strFileName)
        {
            return LoadFileDbl(strFileName, ',');
        }


        public static string[][] LoadTabDelimDataArray(string strFileName)
        {
            string line;
            var columnCount = 0;
            var stringList = new List<string[]>();
            try
            {
                //Pass the file path and file name to the StreamReader constructor.
                var sr = new StreamReader(strFileName);

                //Read the first line of text.
                line = sr.ReadLine();
                //Continue to read until you reach end of file.
                while (line != null)
                {
                    var arr2 = Regex.Split(line, "\t");
                    if (columnCount == 0)
                    {
                        columnCount = arr2.GetLength(0);
                    }
                    var newRowArray = new string[columnCount];
                    var actualColumnCount = arr2.GetLength(0);
                    for (var column = 0; column < actualColumnCount; column++)
                    {
                        newRowArray[column] = arr2[column];
                    }
                    for (var i = actualColumnCount; i < columnCount; i++)
                    {
                        newRowArray[i] = "";
                    }
                    stringList.Add(newRowArray);
                    line = sr.ReadLine();
                }
                //Close the file.
                sr.Close();
            }
            catch (HCException e)
            {
                ////lc.Write(e);
                //Debugger.Break();
                PrintToScreen.WriteLine(e.Message);
            }

            var x = new string[stringList.Count][];
            var j = 0;
            foreach (string[] a in stringList)
            {
                if (a == null)
                {
                    throw (new HCException("null array"));
                }
                x[j] = a;
                j++;
            }
            //string[][][] y = (string[][][])(stringList.ToArray(typeof(String)));
            return x;
        }


        public static DataWrapper LoadTextMinningTestData(
            string strFileName)
        {
            var defaultDelimiter = '\t';
            //
            // get defailt columns
            //
            var intColumnCount =
                FileHelper.CountNumberOfColumns(
                    strFileName,
                    defaultDelimiter);

            var intColumnMap = new int[intColumnCount];

            for (var i = 0; i < intColumnCount; i++)
            {
                intColumnMap[i] = i;
            }

            return LoadTextMinningTestData(
                strFileName,
                defaultDelimiter,
                intColumnMap,
                true,
                new[] {""});
        }

        public static DataWrapper LoadTextMinningTestData(
            string strFileName,
            char delimiter,
            int[] intColumns,
            bool blnIncludeTitles,
            string[] strStopWordsArr)
        {
            string line;
            var columnCount = intColumns.Length;
            var intCounter = 0;
            var stringList = new List<TokenWrapper[][]>();

            try
            {
                //Pass the file path and file name to the StreamReader constructor.
                using (var sr = new StreamReader(strFileName))
                {
                    if (blnIncludeTitles)
                    {
                        //Read the first line of text.
                        sr.ReadLine();
                    }

                    //Continue to read until you reach end of file.
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!line.Equals(string.Empty))
                        {
                            //
                            // split into columns according to the delimitter
                            //
                            var strColArr = line.Split(delimiter);
                            var intCurrentColumnCount = strColArr.Length;

                            //
                            // add the untokenised id row
                            //
                            var newRowArray = new TokenWrapper[columnCount][];
                            newRowArray[0] = new TokenWrapper[1];
                            newRowArray[0][0] = new TokenWrapper(strColArr[
                                intColumns[0]]);

                            // tokenize the rest of the column
                            for (var intColumn = 0;
                                 intColumn < Math.Min(
                                     intCurrentColumnCount,
                                     intColumns.Length);
                                 intColumn++)
                            {
                                //
                                // get collumn id
                                //
                                var intColumnId = intColumns[intColumn];

                                // tokenise current field
                                var tokenArr =
                                    Tokeniser.TokeniseAndWrap(
                                        strColArr[intColumnId],
                                        strStopWordsArr);


                                newRowArray[intColumn] = tokenArr;
                            }
                            for (var i = intCurrentColumnCount; i < columnCount; i++)
                            {
                                newRowArray[i] = new TokenWrapper[1];
                                newRowArray[i][0] = new TokenWrapper("");
                            }
                            stringList.Add(newRowArray);
                            intCounter++;
                        }
                    }
                    //Close the file.
                    sr.Close();
                }
            }
            catch (HCException e)
            {
                Logger.Log(e);
                //Debugger.Break();
                PrintToScreen.WriteLine(e.Message);
            }

            var x = new RowWrapper[stringList.Count];
            var j = 0;
            foreach (TokenWrapper[][] a in stringList)
            {
                if (a == null)
                {
                    //Debugger.Break();
                    throw (new HCException("Null array."));
                }
                x[j].Columns = a;
                j++;
            }
            //string[][][] y = (string[][][])(stringList.ToArray(typeof(String)));
            return new DataWrapper(x);
        }

        #endregion

        #region ComparatorClass

        private void InvokeOnReadLine(string[] lineArr)
        {
            if (OnReadLine != null)
            {
                if (OnReadLine.GetInvocationList().Length > 0)
                {
                    OnReadLine.Invoke(lineArr);
                }
            }
        }

        private class ArrReaderComparator : IComparer<string[]>
        {
            #region Members

            private readonly int m_intColIndex;

            #endregion

            #region Constructor

            public ArrReaderComparator(int intColIndex)
            {
                m_intColIndex = intColIndex;
            }

            #endregion

            #region IComparer<string[]> Members

            public int Compare(string[] x, string[] y)
            {
                return x[m_intColIndex].CompareTo(y[m_intColIndex]);
            }

            #endregion
        }

        #endregion
    }
}


