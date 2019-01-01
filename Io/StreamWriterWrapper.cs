using System.IO;

namespace HC.Core.Io
{
    public class StreamWriterWrapper
    {
        public bool IsClosed { get; private set; }
        private readonly StreamWriter m_sw;

        public StreamWriterWrapper(string strFileName)
        {
            m_sw = new StreamWriter(strFileName);
        }

        public void Close()
        {
            IsClosed = true;
            m_sw.Close();
        }

        public void WriteLine(string s)
        {
            if (m_sw == null || IsClosed)
            {
                return;
            }
            m_sw.WriteLine(s);
        }
    }
}