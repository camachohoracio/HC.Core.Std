#region

using System;

#endregion

namespace HC.Core.Events
{
    public delegate void SetMessageDelegate(
        string strMessage,
        int intProgress);

    public delegate void SetMessageDelegate2(
        object sender,
        SendMessageEventArgs e);

    public static class SendMessageEvent
    {
        /// <summary>
        /// Progress bar event
        /// </summary>
        public static event EventHandler<SendMessageEventArgs> SendTextMessage;

        /// <summary>
        /// Event fired when progress
        /// </summary>
        /// <param name="sender">
        /// Sender
        /// </param>
        /// <param name="strMesssage">
        /// Message
        /// </param>
        public static void OnSendMessage(object sender, string strMesssage)
        {
            var e = new SendMessageEventArgs();
            e.StrMessage = strMesssage;
            OnSendMessage(sender, e);
        }

        public static void OnSendMessage(
            string strMesssage)
        {
            OnSendMessage(
                strMesssage,
                -1);
        }

        public static void OnSendMessage(
            string strMesssage,
            int intProgress)
        {
            var e = new SendMessageEventArgs();
            e.StrMessage = strMesssage;
            e.Progress = intProgress;
            OnSendMessage(null, e);
        }

        public static void OnSendMessage(
            object sender,
            string strMesssage,
            int intProgress)
        {
            var e = new SendMessageEventArgs();
            e.StrMessage = strMesssage;
            e.Progress = intProgress;
            OnSendMessage(sender, e);
        }

        /// <summary>
        /// Send a message
        /// </summary>
        /// <param name="sender">
        /// sender
        /// </param>
        /// <param name="e">
        /// Event arguments
        /// </param>
        public static void OnSendMessage(
            object sender, 
            SendMessageEventArgs e)
        {
            if (SendTextMessage != null)
            {
                SendTextMessage(sender, e);
            }
        }
    }
}


