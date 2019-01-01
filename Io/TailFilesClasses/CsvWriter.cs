#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HC.Core.Logging;
using HC.Core.Reflection;
using HC.Core.Time;

#endregion

namespace HC.Core.Io.TailFilesClasses
{
    public class CsvWriter : IDisposable
    {
        #region Events

        public delegate void AddNewLine(string strLine);
        public event AddNewLine OnAddNewLine;

        #endregion

        #region Members

        private FileStream m_rafData;
        private FileStream m_rafTitles;
        private readonly Encoding uniEncoding = new ASCIIEncoding();
        private const int BYTE_LENGTH = 2048;
        private const string DELIMITER = ",";
        private readonly string m_strTitlesFileName;
        private readonly Dictionary<String, String> m_currentVals = new Dictionary<String, String>();
        private DateTime m_currentTime;
        private readonly List<String> m_headers = new List<String>();
        private readonly List<String> m_currentHeadersUnescaped = new List<String>();
        private int m_intHeaderSize;

        #endregion

        protected CsvWriter()
        {
            m_headers.Add("millis");
            m_headers.Add("datetime");
            m_currentHeadersUnescaped.AddRange(m_headers);
            m_intHeaderSize = 0;
        }


        #region Constructor

        public CsvWriter(string strFilenameData)
        {
            FileInfo fi = new FileInfo(strFilenameData);

            string strTitleFileName =
                GetHeadersFileName(strFilenameData);

            m_strTitlesFileName = strTitleFileName;


            if (!DirectoryHelper.Exists(fi.DirectoryName))
            {
                DirectoryHelper.CreateDirectory(fi.DirectoryName);
            }

            try
            {

                using (StreamWriter sw = new StreamWriter(strFilenameData)) { }
                using (StreamWriter sw = new StreamWriter(strTitleFileName)) { }

                m_rafData = new FileStream(
                    strFilenameData,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite);
                CreateHeaderStream(strTitleFileName);
            }
            catch (FileNotFoundException e)
            {
                //Debugger.Break();
                throw new Exception("Error opening log file: [" + strFilenameData + "]", e);
            }
        }

        private void CreateDataStream(string strFileName)
        {
            try
            {
                if (m_rafData != null)
                {
                    m_rafData.Close();
                }
            }
            catch { }

            m_rafData = new FileStream(
                strFileName,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.ReadWrite);
        }

        private void CreateHeaderStream(string strTitleFileName)
        {
            try
            {
                if (m_rafTitles != null)
                {
                    m_rafTitles.Close();
                }
            }
            catch { }

            m_rafTitles = new FileStream(
                strTitleFileName,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.ReadWrite);
        }

        #endregion

        public static void Save<T>(List<T> list, string strFileName)
        {
            try
            {
                IReflector reflector = ReflectorCache.GetReflector(list[0].GetType());
                using (var sw = new StreamWriter(strFileName))
                {
                    sw.WriteLine(string.Join(";", reflector.GetPropertyNames()));
                    for (int i = 0; i < list.Count; i++)
                    {
                        sw.WriteLine(reflector.ToStringObject(list[i], ";"));
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static StreamWriter createStreamWriter(string filename)
        {
            FileInfo info = new FileInfo(filename);

            if (!DirectoryHelper.Exists(info.DirectoryName))
                DirectoryHelper.CreateDirectory(info.DirectoryName);

            return new StreamWriter(new FileStream(
                                        filename,
                                        FileMode.Create,
                                        FileAccess.Write,
                                        FileShare.Read));
        }

        public void Close()
        {
            try
            {
                m_rafData.Close();
                m_rafTitles.Close();
            }
            catch (IOException e)
            {
            }
        }

        protected void WriteLine()
        {
            try
            {
                //MemoryStream lineBuffer = new MemoryStream();
                StringBuilder sb = new StringBuilder();

                string strLine = m_currentTime + DELIMITER +
                                 DateHelper.ToDateTimeString(m_currentTime);
                sb.Append(strLine);
                //WriteStingIntoBuffer(strLine, lineBuffer);

                for (int i = 2; i < m_currentHeadersUnescaped.Count; i++)
                {
                    //
                    // write line in the buffer
                    //
                    string header = m_currentHeadersUnescaped[i];
                    string val = m_currentVals[header];
                    strLine = DELIMITER + (val != null ? val : "");
                    //WriteStingIntoBuffer(strLine, lineBuffer);
                    sb.Append(strLine);
                }
                strLine = Environment.NewLine;
                //WriteStingIntoBuffer(strLine, lineBuffer);
                sb.Append(strLine);
                //lineBuffer.Position = 0;
                //LoadBuffer(lineBuffer, m_rafData);

                WriteStingIntoBuffer(sb.ToString(), m_rafData);
                m_rafData.Flush();
                //
                // invoke new line
                //
                InvokeOnAddNewLine(sb.ToString());
            }
            catch (IOException e)
            {
                throw;
            }
        }

        protected void Relayout()
        {
            //
            // get position where the titles end
            //
            try
            {
                CreateHeaderStream(m_strTitlesFileName);
                bool first = true;
                StringBuilder sb = new StringBuilder();
                foreach (string header in m_headers)
                {
                    string strLine = first ? header : DELIMITER + header;
                    sb.Append(strLine);
                    first = false;
                }
                sb.Append(Environment.NewLine);
                WriteStingIntoBuffer(sb.ToString(), m_rafTitles);
                m_rafTitles.Flush();
            }
            catch (Exception e)
            {
                //Debugger.Break();
                throw;
            }
        }

        public void ConsolidateFile(string strFileName)
        {
            using (FileStream rafDataTmp = new FileStream(
                strFileName,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.ReadWrite))
            {
                LoadBuffer(
                    m_rafTitles,
                    rafDataTmp);
                LoadBuffer(
                    m_rafData,
                    rafDataTmp);
            }
        }

        private void WriteStingIntoBuffer(
            string strLine,
            Stream memoryStream)
        {
            byte[] bytes = uniEncoding.GetBytes(strLine);
            memoryStream.Write(bytes, 0, bytes.Length);
        }

        private void LoadBuffer(
            Stream readStream,
            Stream writeStream)
        {
            int intByteLength = BYTE_LENGTH;

            int n;
            byte[] buffer = new byte[intByteLength];

            while ((n = readStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                writeStream.Write(buffer, 0, n);
            }
        }

        [Obsolete("Use the new DateTime version of this!")]
        public void update(long timestamp, String name, String value)
        {
            update(new DateTime(timestamp), name, value);
        }

        private void LoadBuffer(
            Stream readStream,
            Stream writeStream,
            int intByteLength)
        {
            int n;
            byte[] buffer = new byte[intByteLength];

            if ((n = readStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                writeStream.Write(buffer, 0, n);
            }
        }


        public void update(DateTime timestamp, Dictionary<String, String> map)
        {
            foreach (String key in map.Keys)
            {
                update(timestamp, key, map[key]);
            }
        }

        public void update(DateTime timestamp, String name, String value)
        {
            if (!m_currentVals.ContainsKey(name))
            {
                m_headers.Add(name.Replace(",", "_"));
                m_currentHeadersUnescaped.Add(name);
            }
            m_currentVals[name] = value;
            m_currentTime = timestamp;
        }

        public void flush()
        {
            if (m_headers.Count != m_intHeaderSize)
            {
                m_intHeaderSize = m_headers.Count;
                Relayout();
            }
            WriteLine();
        }
        public void Dispose()
        {
            Close();
        }

        private void InvokeOnAddNewLine(
            string strLine)
        {
            if (OnAddNewLine != null)
            {
                if (OnAddNewLine.GetInvocationList().Length > 0)
                {
                    OnAddNewLine.Invoke(strLine);
                }
            }
        }

        public static string GetHeadersFileName(string strFileName)
        {
            FileInfo fi = new FileInfo(strFileName);
            string strTitleFileName =
                fi.DirectoryName + @"\titles_" +
                fi.Name;
            return strTitleFileName;
        }
    }
}


