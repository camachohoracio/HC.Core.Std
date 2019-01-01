#region

using System;
using System.IO;
using HC.Core.Helpers;

#endregion

namespace HC.Core.Io.TailFilesClasses
{
    public delegate void NewMessageDelegate(TailMessage message);

    public class TailFile
    {
        #region Members

        private readonly NewMessageDelegate m_newMessageDelegate;
        private readonly string m_strFilter;
        private readonly string m_strFullFullFileName;
        private string m_fileName;
        private string m_filePath;
        private FileSystemWatcher m_fileWatcher;
        private TextFileReader m_textFileReader;

        #endregion

        #region Constructors

        public TailFile(
            string strStrFullFileName,
            NewMessageDelegate newMessageDelegate,
            string strFilter)
        {
            m_strFullFullFileName = strStrFullFileName;
            m_newMessageDelegate = newMessageDelegate;
            m_strFilter = strFilter;
            Init(strStrFullFileName);
        }

        #endregion

        private void Init(string strFileName)
        {
            m_textFileReader = null;
            m_filePath = "";
            m_fileName = "";
            FindPathAndFile(strFileName);
        }

        private void FindPathAndFile(string strFileName)
        {
            var elements = strFileName.Split(new[] {'\\'});

            m_fileName = elements[elements.Length - 1];
            for (var i = 0; i < elements.Length - 1; i++)
                m_filePath += elements[i] + '\\';
        }

        // This method reads the file data from the stream and prints the output
        private void ReadAndPrintFromFile(int i)
        {
            string input;

            if (i != 0)
                m_textFileReader.FileStream.Position = m_textFileReader.FileStream.Length - i;

            if (string.IsNullOrEmpty(m_strFilter))
            {
                while ((input = m_textFileReader.AsyncRead()) != null)
                    m_newMessageDelegate(new TailMessage(
                                             m_strFullFullFileName,
                                             m_strFilter,
                                             input));
            }
            else
            {
                while ((input = m_textFileReader.AsyncRead()) != null)
                    if (input.Contains(m_strFilter))
                        m_newMessageDelegate(new TailMessage(
                                                 m_strFullFullFileName,
                                                 m_strFilter,
                                                 input));
            }
        }

        //[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void DoMonitoring(bool startAtTop)
        {
            try
            {
                // Open the file
                m_textFileReader = new TextFileReader(m_filePath + m_fileName);

                if (startAtTop)
                    ReadAndPrintFromFile(0);
                else
                    m_textFileReader.ReadToEndOfFile();

                // Create a new FileSystemWatcher and set its properties.
                m_fileWatcher = new FileSystemWatcher
                                    {
                                        Path = m_filePath,
                                        Filter = m_fileName,
                                        NotifyFilter = NotifyFilters.Size |
                                                       NotifyFilters.LastAccess |
                                                       NotifyFilters.LastWrite |
                                                       NotifyFilters.FileName |
                                                       NotifyFilters.DirectoryName |
                                                       NotifyFilters.Attributes |
                                                       NotifyFilters.CreationTime |
                                                       NotifyFilters.Security
                                    };

                m_fileWatcher.Changed += OnChanged;
                m_fileWatcher.Deleted += OnChanged;
                m_fileWatcher.Created += OnChanged;
                m_fileWatcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                PrintToScreen.WriteLine(ex.ToString());
            }
        }

        public void CloseFile()
        {
            if (m_fileWatcher != null)
                m_fileWatcher.Changed -= OnChanged; // Add event handler(s).
            if (m_textFileReader != null)
                m_textFileReader.Closefile();
        }

        #region Event handlers

        // Event handler for file changed.  This causes all the work to be done.
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            ReadAndPrintFromFile(0);
        }

        #endregion Event handlers
    }
}


