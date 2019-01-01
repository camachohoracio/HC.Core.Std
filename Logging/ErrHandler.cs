using System;
using log4net.Core;

namespace HC.Core.Logging
{
    public delegate void ErrorDel(string message, Exception e);

    public class ErrHandler : IErrorHandler
    {
        private readonly ErrorDel m_errorDel;

        public ErrHandler(ErrorDel errorDel)
        {
            m_errorDel = errorDel;
        }

        public void Error(string message, Exception e, ErrorCode errorCode)
        {
            m_errorDel(message, e);
        }

        public void Error(string message, Exception e)
        {
            m_errorDel(message, e);
        }

        public void Error(string message)
        {
            m_errorDel(message, null);
        }
    }
}



