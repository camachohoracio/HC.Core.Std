#region

using System;

#endregion

namespace HC.Core.Events
{
    /// <summary>
    /// progress bar messages.
    /// Note: This class is not threadsafe.
    /// </summary>
    public class SendMessageEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Message
        /// </summary>
        public string StrMessage { get; set; }

        public int Progress { get; set; }

        #endregion

        #region Constructors

        public SendMessageEventArgs()
        {
            Progress = -1;
        }

        #endregion
    }
}


