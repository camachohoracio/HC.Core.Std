#region

using System;

#endregion

namespace HC.Core.Logging
{
    public interface ILoggerService
    {
        void Write(string strMessage);
        void Write(Exception ex);
        void Write(Exception ex, bool blnPublishGui);
    }
}


