#region

using System.Collections.Generic;
using System.Data;
//using System.Data.OleDb;
using System.IO;

#endregion

namespace HC.Core.Io
{
    public static class DataImportHelper
    {
        public static List<string> GetExcelSheetNames(string filePath)
        {
            return null;
            //OleDbConnection objConn = null;
            //DataTable dt = null;
            //var sheetNames = new List<string>();

            //try
            //{
            //    var connections = false;

            //    connections = WillFileOpen(filePath);

            //    if (connections)
            //    {
            //        // Connection String. Change the excel file to the file you
            //        // will search.
            //        //String conn = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filePath + ";Extended Properties=Excel 8.0;";

            //        var conn = "Provider=Microsoft.Jet.OLEDB.4.0;" +
            //                   "Data Source=" + filePath + ";" +
            //                   "Extended Properties=" + '"' + "Excel 8.0;HDR=Yes;IMEX=1" + '"' + ";";

            //        // Create connection object by using the preceding connection string.
            //        objConn = new OleDbConnection(conn);
            //        // Open connection with the database. 

            //        objConn.Open();
            //        // Get the data table containg the schema guid.
            //        dt = objConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

            //        if (dt == null)
            //        {
            //            return null;
            //        }

            //        // Add the sheet name to the string list.
            //        foreach (DataRow row in dt.Rows)
            //        {
            //            sheetNames.Add(row["TABLE_NAME"].ToString());
            //        }

            //        return sheetNames;
            //    }
            //    else
            //    {
            //        Console.WriteLine("Already a concurrent connection to the file " + filePath);
            //        return null;
            //    }
            //}
            //catch (HCException e)
            //{
            //    ////lc.Write(e);
            //}
            //finally
            //{
            //    // Clean up.
            //    if (objConn != null)
            //    {
            //        objConn.Close();
            //        objConn.Dispose();
            //    }
            //    if (dt != null)
            //    {
            //        dt.Dispose();
            //    }
            //}
            //return null;
        }

        /// <summary>
        ///   Method designed to fail if a file won't allow another connection to it
        ///   opening a filestream to the file will fail if it can't be opened, this means
        ///   we can guarantee the system won't fall apart.
        /// </summary>
        /// <param name = "filePath"></param>
        /// <returns></returns>
        private static bool WillFileOpen(string filePath)
        {
            try
            {
                var fs = new FileStream(filePath, FileMode.Open);
                fs.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static List<string> GetAccessTableNames(string filePath)
        {
            return null;
            //OleDbConnection objConn = null;
            //DataTable dt = null;
            //var tableNames = new List<string>();

            //try
            //{
            //    var strConn = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filePath + ";";
            //    // Create connection object by using the preceding connection string.
            //    objConn = new OleDbConnection(strConn);

            //    // We only want user tables, not system tables
            //    var restrictions = new string[4];
            //    restrictions[3] = "Table";

            //    // Open connection with the database.
            //    objConn.Open();

            //    // Get list of user tables
            //    dt = objConn.GetSchema("Tables", restrictions);

            //    // Add the sheet name to the string list.
            //    foreach (DataRow row in dt.Rows)
            //    {
            //        tableNames.Add(row["TABLE_NAME"].ToString());
            //    }

            //    return tableNames;
            //}
            //finally
            //{
            //    // Clean up.
            //    if (objConn != null)
            //    {
            //        objConn.Close();
            //        objConn.Dispose();
            //    }
            //    if (dt != null)
            //    {
            //        dt.Dispose();
            //    }
            //}
        }

        public static DataTable ImportExcelData(
            string FilePath,
            string WorksheetName,
            bool ColumnHeader)
        {
            return null;

            //string conn;

            //if (ColumnHeader)
            //{
            //    conn = "Provider=Microsoft.Jet.OLEDB.4.0;" + "Data Source=" + @FilePath +
            //           ";Extended Properties=Excel 8.0;";
            //}
            //else
            //{
            //    conn = "Provider=Microsoft.Jet.OLEDB.4.0;" + "Data Source=" + @FilePath +
            //           ";Extended Properties=\"Excel 8.0;HDR=NO;\"";
            //}

            //var dt = new DataTable("Excel");

            ////'You must use the $ after the object you reference in the spreadsheet
            //var adExcel = new OleDbDataAdapter("SELECT * FROM [" + WorksheetName + "]", conn);
            //adExcel.TableMappings.Add("Table", "Excel");
            //adExcel.SelectCommand.CommandTimeout = 0;
            //adExcel.Fill(dt);
            //return dt;
        }

        public static DataTable ImportAccessData(string filePath, string tableName)
        {
            return null;
            //var conn = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filePath + ";";
            //var dt = new DataTable("Access");
            //var adAccess = new OleDbDataAdapter("SELECT * FROM [" + tableName + "]", conn);
            //adAccess.TableMappings.Add("Table", "Access");
            //adAccess.SelectCommand.CommandTimeout = 0;
            //adAccess.Fill(dt);

            //return dt;
        }

        public static DataTable ImportCSVData(string filePath, string fileName, bool columnHeader)
        {
            var conn = string.Empty;

            if (columnHeader)
            {
                conn = "Provider=Microsoft.Jet.OLEDB.4.0;" + "Data Source=" + filePath +
                       ";Extended Properties=\"text;HDR=Yes;FMT=Delimited\"";
            }
            else
            {
                conn = "Provider=Microsoft.Jet.OLEDB.4.0;" + "Data Source=" + filePath +
                       ";Extended Properties=\"text;HDR=No;FMT=Delimited\"";
            }

            var dt = new DataTable("Csv");

            //var adCSV = new OleDbDataAdapter("SELECT * FROM [" + fileName + "]", conn);
            //adCSV.TableMappings.Add("Table", "Csv");
            //adCSV.SelectCommand.CommandTimeout = 0;
            //adCSV.Fill(dt);

            return dt;
        }
    }
}


