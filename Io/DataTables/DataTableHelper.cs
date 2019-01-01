#region

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using HC.Core.Exceptions;

#endregion

namespace HC.Core.Io.DataTables
{
    public class DataTableHelper
    {
        #region Members

        private readonly bool m_blnContainsTitles;
        private readonly List<string> m_titlesList;
        private int mvHeaderRow = -1;

        #endregion

        #region Properties

        /// <summary>
        ///   FileInfo with the import/export file information
        /// </summary>
        public FileInfo FileInf { get; set; }

        /// <summary>
        ///   The row containing the column titles -1 = no titles, 0 = first row
        /// </summary>
        public int HeaderRow
        {
            get { return mvHeaderRow; }
            set { mvHeaderRow = value; }
        }

        /// <summary>
        ///   Zero based row containing the first data row
        /// </summary>
        public int DataRow1 { get; set; }

        /// <summary>
        ///   field delimiter
        /// </summary>
        public string Delimiter { get; set; }

        /// <summary>
        ///   Maximum rows ro read 0 = all rows
        /// </summary>
        public int MaxRows { get; set; }

        /// <summary>
        ///   Name of the file without the extension
        /// </summary>
        public string NameOnly //read only
        {
            get
            {
                return FileInf.Name.Substring(0,
                                              (FileInf.Name.Length - FileInf.Extension.Length));
            }
        }

        #endregion

        #region Constructors

        public DataTableHelper(
            string strFileName,
            List<string> titlesList) :
                this(
                strFileName,
                false)
        {
            m_titlesList = titlesList;
        }

        /// <summary>
        ///   Instantiate with the filename
        /// </summary>
        /// <param name = "sFilename">file name to be used to create the fileinfo object</param>
        public DataTableHelper(
            string strFileName,
            bool blnContainsTitles)
        {
            m_blnContainsTitles = blnContainsTitles;

            if (blnContainsTitles)
            {
                HeaderRow = 0;
            }
            else
            {
                HeaderRow = -1;
            }

            FileInf = new FileInfo(strFileName);

            var dataFileType =
                FileHelper.GetDataFileType(strFileName);

            //
            // Set delimiter type
            //
            if (dataFileType == DataFileType.Excel)
            {
                throw new NotImplementedException();
            }
            else if (dataFileType == DataFileType.Csv)
            {
                Delimiter = ",";
            }
            else if (dataFileType == DataFileType.Txt)
            {
                Delimiter = "\t";
            }
        }

        #endregion

        #region Public

        public static void SaveDataTableToTextFile(
            DataTable table,
            string filename)
        {
            SaveDataTableToTextFile(
                table,
                filename,
                "\t");
        }

        public static void SaveDataTableToCsvFile(
            DataTable table,
            string filename)
        {
            SaveDataTableToTextFile(
                table,
                filename,
                ",");
        }

        public static void SaveDataTableToTextFile(
            DataTable[] dataTableArray,
            DataTable[] titlesTableArray,
            DataTable[] headersTableArray,
            string filename,
            string sepChar)
        {
            using (var writer = new StreamWriter(filename))
            {
                for (var i = 0; i < dataTableArray.Length; i++)
                {
                    //
                    // write headers
                    //
                    if (headersTableArray != null)
                    {
                        WriteDataTable(
                            headersTableArray[i],
                            sepChar,
                            writer);
                    }

                    //
                    // write headers
                    //
                    if (titlesTableArray != null)
                    {
                        WriteDataTable(
                            titlesTableArray[i],
                            sepChar,
                            writer);
                    }

                    //
                    // write data tables
                    //
                    WriteDataTable(
                        dataTableArray[i],
                        sepChar,
                        writer);
                }
                writer.Close();
            }
        }

        public static void SaveDataTableToTextFile(
            DataTable table,
            string filename,
            string sepChar)
        {
            using (var writer = new StreamWriter(filename))
            {
                WriteDataTable(
                    table,
                    sepChar,
                    writer);
                writer.Close();
            }
        }

        public static void CreateDataTableFromArrListDbl(
            List<double[,]> dataList,
            List<string[]> dataTitles,
            out DataTable titlesDataTable,
            out DataTable dataTable)
        {
            titlesDataTable = new DataTable();
            dataTable = new DataTable();
            var intColCount = dataTitles[0].Length;
            var intColCounter = 0;
            for (var i = 0; i < intColCount; i++)
            {
                for (var j = 0; j < dataList.Count; j++)
                {
                    var strTitle = "col_" + intColCounter;
                    AddDataColumn(titlesDataTable,
                                  strTitle,
                                  typeof (string));
                    AddDataColumn(dataTable,
                                  strTitle,
                                  typeof (double));
                    intColCounter++;
                }
            }

            //
            // load titles into data tables
            //
            var newDataRow = titlesDataTable.NewRow();
            intColCounter = 0;
            for (var j = 0; j < dataList.Count; j++)
            {
                for (var i = 0; i < intColCount; i++)
                {
                    newDataRow[intColCounter] = dataTitles[j][i];
                    intColCounter++;
                }
            }
            titlesDataTable.Rows.Add(newDataRow);


            // get max row Length
            var intRowLength = -1;
            for (var j = 0; j < dataList.Count; j++)
            {
                if (intRowLength < dataList[j].GetLength(0))
                {
                    intRowLength = dataList[j].GetLength(0);
                }
            }

            //
            // load data into data tables
            //
            for (var intRowIndex = 0;
                 intRowIndex < intRowLength;
                 intRowIndex++)
            {
                newDataRow = dataTable.NewRow();
                intColCounter = 0;
                for (var j = 0; j < dataList.Count; j++)
                {
                    for (var i = 0; i < intColCount; i++)
                    {
                        if (dataList[j].GetLength(0) > intRowIndex)
                        {
                            newDataRow[intColCounter] = dataList[j][intRowIndex, i];
                        }
                        else
                        {
                            newDataRow[intColCounter] = 0;
                        }
                        intColCounter++;
                    }
                }
                dataTable.Rows.Add(newDataRow);
            }
        }

        public static void CreateDataTableFromArrListStr(
            List<string[,]> dataList,
            List<string[]> dataTitles,
            out DataTable titlesDataTable,
            out DataTable dataTable)
        {
            titlesDataTable = new DataTable();
            dataTable = new DataTable();
            var intColCount = dataTitles[0].Length;
            var intColCounter = 0;
            for (var i = 0; i < intColCount; i++)
            {
                for (var j = 0; j < dataList.Count; j++)
                {
                    var strTitle = "col_" + intColCounter;
                    AddDataColumn(titlesDataTable,
                                  strTitle,
                                  typeof (string));
                    AddDataColumn(dataTable,
                                  strTitle,
                                  typeof (string));
                    intColCounter++;
                }
            }

            //
            // load titles into data tables
            //
            var newDataRow = titlesDataTable.NewRow();
            intColCounter = 0;
            for (var j = 0; j < dataList.Count; j++)
            {
                for (var i = 0; i < intColCount; i++)
                {
                    newDataRow[intColCounter] = dataTitles[j][i];
                    intColCounter++;
                }
            }
            titlesDataTable.Rows.Add(newDataRow);


            // get max row Length
            var intRowLength = -1;
            for (var j = 0; j < dataList.Count; j++)
            {
                if (intRowLength < dataList[j].GetLength(0))
                {
                    intRowLength = dataList[j].GetLength(0);
                }
            }

            //
            // load data into data tables
            //
            for (var intRowIndex = 0;
                 intRowIndex < intRowLength;
                 intRowIndex++)
            {
                newDataRow = dataTable.NewRow();
                intColCounter = 0;
                for (var j = 0; j < dataList.Count; j++)
                {
                    for (var i = 0; i < intColCount; i++)
                    {
                        if (dataList[j].GetLength(0) > intRowIndex)
                        {
                            newDataRow[intColCounter] = dataList[j][intRowIndex, i];
                        }
                        else
                        {
                            newDataRow[intColCounter] = 0;
                        }
                        intColCounter++;
                    }
                }
                dataTable.Rows.Add(newDataRow);
            }
        }

        public static void AddColumn(
            DataTable resultsDatatable,
            DataTable titlesDataTable,
            string strTitle,
            Type type)
        {
            if (titlesDataTable != null)
            {
                AddDataColumn(titlesDataTable, strTitle, Type.GetType("System.String"));
            }
            AddDataColumn(resultsDatatable, strTitle, type);
        }

        public static void AddDataColumn(
            DataTable resultsDatatable,
            string strTitle,
            Type type)
        {
            var analysisIdCol2 = new DataColumn();
            analysisIdCol2.DataType = Type.GetType(type.ToString());
            analysisIdCol2.ColumnName = strTitle;
            resultsDatatable.Columns.Add(analysisIdCol2);
        }

        public static DataTable CreateTitlesDataTable(
            DataTable dt)
        {
            var intColumnCount = dt.Columns.Count;

            var titlesDataTable = new DataTable();
            for (var i = 0; i < intColumnCount; i++)
            {
                var strTitle =
                    dt.Columns[i].ColumnName;

                var analysisIdCol1 = new DataColumn();
                analysisIdCol1.DataType = Type.GetType("System.String");
                analysisIdCol1.ColumnName = strTitle;
                titlesDataTable.Columns.Add(analysisIdCol1);
            }
            //
            // add titles row
            //
            var row = titlesDataTable.NewRow();
            for (var i = 0; i < intColumnCount; i++)
            {
                var strTitle =
                    dt.Columns[i].ColumnName;
                row[strTitle] = strTitle;
            }
            titlesDataTable.Rows.Add(row);

            return titlesDataTable;
        }

        public static DataTable MapDataTable(
            DataTable dt,
            List<string> columnList,
            List<string> selectedColumnList,
            bool blnIncludeTitles)
        {
            var intColumnMapping = new int[columnList.Count];
            for (var intColumnId = 0; intColumnId < columnList.Count; intColumnId++)
            {
                var strCurrentColumnName = columnList[intColumnId];
                for (var i = 0; i < selectedColumnList.Count; i++)
                {
                    var strSelectedCurrentColumnName = selectedColumnList[i];
                    if (strCurrentColumnName.Equals(strSelectedCurrentColumnName))
                    {
                        intColumnMapping[intColumnId] = i;
                        break;
                    }
                }
            }

            var mappedDt = new DataTable();
            // load new columns
            for (var intColumnId = 0; intColumnId < columnList.Count; intColumnId++)
            {
                var intMappedIndex = intColumnMapping[intColumnId];

                var dc = dt.Columns[intMappedIndex];

                var strColumnName = selectedColumnList[intMappedIndex]; //selectedColumnList[intColumnId];

                AddDataColumn(mappedDt, strColumnName, dc.DataType);
            }
            //
            // load data
            //
            for (var i = blnIncludeTitles ? 1 : 0; i < dt.Rows.Count; i++)
            {
                var row = dt.Rows[i];
                var newRow = mappedDt.NewRow();
                for (var intColumnId = 0; intColumnId < columnList.Count; intColumnId++)
                {
                    var intMappedIndex = intColumnMapping[intColumnId];
                    newRow[intColumnId] = row[intMappedIndex];
                }
                mappedDt.Rows.Add(newRow);
            }
            return mappedDt;
        }

        public static void SortDataTable(DataTable dt, string sort)
        {
            // Setup a copy of the DataTable
            var newDT = dt.Clone();
            var rowCount = dt.Rows.Count;

            // Populate the new DataTable from the old one, with ordering
            var foundRows = dt.Select(null, sort);
            for (var i = 0; i < rowCount; i++)
            {
                var arr = new object[dt.Columns.Count];
                for (var j = 0; j < dt.Columns.Count; j++)
                {
                    arr[j] = foundRows[i][j];
                }
                var data_row = newDT.NewRow();
                data_row.ItemArray = arr;
                newDT.Rows.Add(data_row);
            }

            dt.Rows.Clear();

            for (var i = 0; i < newDT.Rows.Count; i++)
            {
                var arr = new object[dt.Columns.Count];
                for (var j = 0; j < dt.Columns.Count; j++)
                {
                    arr[j] = newDT.Rows[i][j];
                }

                var data_row = dt.NewRow();
                data_row.ItemArray = arr;
                dt.Rows.Add(data_row);
            }
        }

        public static DataTable StrToTable(
            string strInput,
            bool blnHasTitles,
            string strDelimiter,
            Type[] typeArr)
        {
            if (string.IsNullOrEmpty(strInput))
            {
                return null;
            }

            var dtData = new DataTable();
            TextReader oTR = new StringReader(strInput);
            string strLine = null;
            string[] arData; //array of strings to load the data into for each line read in
            DataRow drData;
            var iRows = 0;

            //get the header row
            if (!blnHasTitles)
            {
                var tokens = oTR.ReadLine().Split(
                    strDelimiter.ToCharArray());
                strLine = "col_0";
                for (var i = 1; i < tokens.Length; i++)
                {
                    strLine += strDelimiter + "col_" + i;
                }
            }
            else
            {
                strLine = oTR.ReadLine();
            }
            var titleTokens = strLine.Split(strDelimiter.ToCharArray());

            for (var i = 0; i < titleTokens.Length; i++)
            {
                var strTitle = titleTokens[i];
                AddDataColumn(
                    dtData,
                    strTitle,
                    typeArr[i]);
            }

            //bail if the table failed
            if (dtData.Columns.Count == 0)
            {
                return null;
            }

            //reset the text reader
            oTR.Close();
            oTR = new StringReader(strInput);

            //get the first data line
            strLine = CleanString(oTR.ReadLine());
            if (blnHasTitles)
            {
                strLine = CleanString(oTR.ReadLine());
            }

            while (true && strLine != null)
            {
                //populate the string array with the line data
                arData = strLine.Split(new[] {strDelimiter}, StringSplitOptions.None);
                //load thedatarow
                drData = dtData.NewRow();
                for (var i = 0; i < dtData.Columns.Count; i++)
                {
                    //test for additional fields - this can happen if there are stray commas
                    if (i < arData.Length)
                    {
                        drData[i] = arData[i];
                    }
                }
                //only get the top N rows if there is a max rows value > 0
                iRows++;

                //add the row to the table
                dtData.Rows.Add(drData);

                //read in the next line
                strLine = CleanString(oTR.ReadLine());
                if (strLine == null)
                {
                    break;
                }
            }
            oTR.Close();
            oTR.Dispose();
            dtData.AcceptChanges();
            return dtData;
        }


        /// <summary>
        ///   Read in the CSV file and move the data to a table
        /// </summary>
        /// <returns>Datatable with the CSV data loaded</returns>
        public DataTable CsvToTable()
        {
            try
            {
                // trap if the fileinfo has not been added to the object
                if (FileInf == null)
                {
                    return null;
                }

                var dtData = new DataTable();
                TextReader oTR = File.OpenText(FileInf.FullName);
                string sLine = null;
                string[] arData; //array of strings to load the data into for each line read in
                DataRow drData;
                var iRows = 0;

                //get the header row
                if (mvHeaderRow > -1)
                {
                    for (var i = 0; i < (mvHeaderRow + 1); i++)
                    {
                        sLine = CleanString(oTR.ReadLine());
                    }
                }
                else if (m_titlesList != null)
                {
                    //
                    // load titles from provided list
                    //
                    var blnFirstTitle = true;
                    foreach (string strTitle in m_titlesList)
                    {
                        sLine += blnFirstTitle ? strTitle : "," + strTitle;
                        blnFirstTitle = false;
                    }
                    mvHeaderRow = 0;
                }
                else //get the first row to count the columns
                {
                    sLine = CleanString(oTR.ReadLine());
                }
                //create the columns in the table
                CreateColumns(dtData, sLine);

                //bail if the table failed
                if (dtData.Columns.Count == 0)
                {
                    return null;
                }

                //reset the text reader
                oTR.Close();
                oTR = File.OpenText(FileInf.FullName);

                //get the first data line
                sLine = CleanString(oTR.ReadLine());
                if (m_blnContainsTitles)
                {
                    sLine = CleanString(oTR.ReadLine());
                }

                while (true && sLine != null)
                {
                    //populate the string array with the line data
                    arData = sLine.Split(new[] {Delimiter}, StringSplitOptions.None);
                    //load thedatarow
                    drData = dtData.NewRow();
                    for (var i = 0; i < dtData.Columns.Count; i++)
                    {
                        //test for additional fields - this can happen if there are stray commas
                        if (i < arData.Length)
                        {
                            drData[i] = arData[i];
                        }
                    }
                    //only get the top N rows if there is a max rows value > 0
                    iRows++;
                    if (MaxRows > 0 && iRows > MaxRows)
                    {
                        break;
                    }

                    //add the row to the table
                    dtData.Rows.Add(drData);

                    //read in the next line
                    sLine = CleanString(oTR.ReadLine());
                    if (sLine == null)
                    {
                        break;
                    }
                }
                oTR.Close();
                oTR.Dispose();
                dtData.AcceptChanges();
                return dtData;
            }
            catch (HCException Exc)
            {
                throw;
            }
        }

        public static DataTable CreateDataTable(
            string[] strTitlesArr,
            List<object[]> data)
        {
            var dt = CreateDataTable(strTitlesArr);
            int intCols = strTitlesArr.Length;
            foreach (object[] objects in data)
            {
                var row = dt.NewRow();
                for(int i = 0; i<intCols; i++)
                {
                    object o = objects[i];
                    row[i] = o == null ? string.Empty : o.ToString();
                }
                dt.Rows.Add(row);
            }
            return dt;
        }

        public static DataTable CreateDataTable(
            string[] strTitlesArr)
        {
            return CreateDataTable(
                strTitlesArr,
                (from n in strTitlesArr select n.GetType()).ToArray());
        }

        public static DataTable CreateDataTable(
            string[] strTitlesArr,
            Type[] typeArr)
        {
            var dt = new DataTable();
            if (strTitlesArr.Length != typeArr.Length)
            {
                throw new HCException("Column size does not match.");
            }

            for (var i = 0; i < strTitlesArr.Length; i++)
            {
                dt.Columns.Add(
                    strTitlesArr[i],
                    typeArr[i]);
            }
            return dt;
        }

        /// <summary>
        ///   Export the table to the CSV filename
        /// </summary>
        /// <param name = "dvData">Dataview to be exported</param>
        /// <param name = "bExcludeTitles"></param>
        /// <returns>success</returns>
        public bool TableToCSV(DataView dvData, bool bExcludeTitles)
        {
            try
            {
                if (dvData == null)
                {
                    return false;
                }
                var sLine = new StringBuilder();

                //Delete the existing file
                if (FileInf.Exists)
                {
                    FileInf.Delete();
                }
                var oSW = new StreamWriter(new FileStream(FileInf.FullName, FileMode.Create));

                if (!bExcludeTitles)
                {
                    foreach (DataColumn oCol in dvData.Table.Columns)
                    {
                        sLine.AppendFormat("{0}{1}", oCol.Caption, Delimiter);
                    }
                    //strip the trailing comma
                    sLine.Length = sLine.Length - 1;

                    //write the line
                    oSW.WriteLine(sLine.ToString());
                }

                for (var i = 0; i < dvData.Count; i++)
                {
                    sLine.Length = 0;
                    for (var j = 0; j < dvData.Table.Columns.Count; j++)
                    {
                        sLine.AppendFormat("{0}{1}", Convert.ToString(dvData[i][j]), Delimiter);
                    }
                    oSW.WriteLine(sLine.ToString());
                }
                oSW.Flush();
                oSW.Close();
                return true;
            }
            catch (HCException Exc)
            {
                throw;
            }
        }

        #endregion

        #region Private

        private static void WriteDataTable(
            DataTable dataTable,
            string sepChar,
            StreamWriter writer)
        {
            // first write a line with the columns name
            var sep = "";
            var builder = new StringBuilder();
            foreach (DataColumn col in dataTable.Columns)
            {
                builder.Append(sep).Append(col.ColumnName);
                sep = sepChar;
            }
            writer.WriteLine(builder.ToString());

            // then write all the rows
            foreach (DataRow row in dataTable.Rows)
            {
                sep = "";
                builder = new StringBuilder();

                foreach (DataColumn col in dataTable.Columns)
                {
                    builder.Append(sep).Append(row[col.ColumnName]);
                    sep = sepChar;
                }
                writer.WriteLine(builder.ToString());
            }
        }

        /// <summary>
        ///   Deal with single quiotes and backslash in the data
        /// </summary>
        /// <param name = "sLine">String to check</param>
        /// <returns>Checked string</returns>
        private static string CleanString(string sLine)
        {
            if (sLine == null)
            {
                return null;
            }
            sLine = sLine.Replace("'", "''");
            sLine = sLine.Replace("\"", "");
            return sLine;
        }

        /// <summary>
        ///   Create the datatable based on the title row of the number of elements in the 1st datarow array
        /// </summary>
        /// <param name = "oTable">Table to add the columns to</param>
        /// <param name = "sLine">title or first data row</param>
        private void CreateColumns(
            DataTable oTable,
            string sLine)
        {
            DataColumn oCol;
            string sTemp;
            int iCol;
            var arData = sLine.Split(new[] {Delimiter}, StringSplitOptions.None);
            for (var i = 0; i < arData.Length; i++)
            {
                //get the header labels from the row
                sTemp = string.Empty;
                if (mvHeaderRow != -1)
                {
                    sTemp = arData[i];
                }

                //deal with the empty string (may be missing from the row)
                if ((sTemp.Trim()).Length == 0)
                {
                    sTemp = string.Format("ColName_{0}", i);
                }

                //Deal with duplicate column names in the title row
                iCol = oTable.Columns.Count + 100;
                while (oTable.Columns.Contains(sTemp))
                {
                    sTemp = string.Format("ColName_{0}", iCol);
                }

                oCol = new DataColumn(sTemp, Type.GetType("System.String"));
                oTable.Columns.Add(oCol);
            }
        }

        #endregion
    }
}


