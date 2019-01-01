#region

using System.Collections.Generic;
using System.IO;

#endregion

namespace HC.Core.Io.TailFilesClasses
{
    public class TextFileReader
    {
        public FileStream FileStream { get; private set; }
        private readonly StreamReader m_streamReader;

        public TextFileReader(string fileName)
        {
            FileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1, true);
            m_streamReader = new StreamReader(FileStream);
        }


        public string AsyncRead()
        {
            return (m_streamReader.ReadLine());
        }

        public void ReadToEndOfFile()
        {
            FileStream.Position = FileStream.Length;
        }

        public void Closefile()
        {
            FileStream.Close();
        }

        public static List<string> GetList(string strFileName)
        {
            var fileStream = new FileStream(
                strFileName, 
                FileMode.Open, 
                FileAccess.Read, 
                FileShare.ReadWrite, 
                1, 
                true);
            var resultList = new List<string>();
            using (var sr = new StreamReader(fileStream))
            {
                string strLine;
                while ((strLine = sr.ReadLine()) != null)
                {
                    resultList.Add(strLine.Trim());
                }
            }
            return resultList;
        }
    }
}


