#region

using System;
using System.Net;
using System.Reflection;

#endregion

namespace HC.Core.Exceptions
{
    public class HCException : Exception
    {
        #region Constructors

        public HCException()
            : this(string.Empty) { }

        public HCException(string strMessage)
            : this(strMessage, true) { }

        public HCException(
            string strMessage,
            bool blnSendEmail)
            : base(strMessage)
        {
            //string strTitle = GetMessageTitle(this);
            //if (blnSendEmail)
            //{
            //    MailImpl.SendMessage(
            //        strTitle,
            //        StackTrace);
            //}
        }

        #endregion

        #region Public

        public static void AssertMustBeTrue(
            bool blnCondition)
        {
            AssertMustBeTrue(blnCondition,
                -1,
                string.Empty);
        }

        public static void ThrowIfTrue(
            bool blnCondition)
        {
            ThrowIfTrue(blnCondition, "Unknown Error");
        }

        public static void ThrowIfTrue(
            bool blnCondition,
            string strMessage)
        {
            AssertMustBeTrue(
                !blnCondition,
                -1,
                strMessage);
        }

        public static void AssertMustBeTrue(
            bool blnCondition,
            string strMessage)
        {
            AssertMustBeTrue(blnCondition,
                -1,
                strMessage);
        }

        public static void AssertMustBeTrue(
            bool blnCondition,
            int intErrorCode,
            string strMessage)
        {
            AssertMustBeTrue(
                blnCondition,
                intErrorCode,
                strMessage,
                true);
        }

        public static void AssertMustBeTrue(
            bool blnCondition, 
            int intErrorCode, 
            string strMessage,
            bool blnPublishException)
        {
            if (!blnCondition)
            {
                DateTime now = DateTime.Now;
                string strExcDescr = strMessage +
                                     ". Error code = " +
                                     intErrorCode + ". Time = " +
                                     now;
                var exception = new HCException(strExcDescr);
                string strTitle = GetMessageTitle(exception);
                MailImpl.SendEmail(
                    strTitle,
                    exception.StackTrace);
                throw new Exception(strExcDescr, exception);
            }
        }

        public static string GetMessageTitle(Exception exception)
        {
            //WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
            //if (windowsIdentity != null)
            {
                //string strUserName = windowsIdentity.Name;
                string strPcName = Dns.GetHostName();
                return //strUserName + "_" +
                       strPcName + "_" +
                       Assembly.GetExecutingAssembly().FullName + "_" +
                       exception.Message;
            }
            return string.Empty;
        }

        public override string ToString()
        {
            return base.Message;
        }

        #endregion

        public static void Throw(string p)
        {
            ThrowIfTrue(true,p);
        }
    }
}


