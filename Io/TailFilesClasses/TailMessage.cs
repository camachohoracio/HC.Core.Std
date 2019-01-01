#region

using System;
using System.Globalization;

#endregion

namespace HC.Core.Io.TailFilesClasses
{
    public class TailMessage
    {
        #region Properties

        public DateTime Time { get; private set; }
        public string Message { get; private set; }
        public string FileName { get; private set; }
        public string Filter { get; private set; }

        #endregion

        #region Constructors

        public TailMessage(
            string strFilename,
            string strFilter,
            string strMessage)
        {
            FileName = strFilename;
            Filter = strFilter;

            var pos = strMessage.IndexOf(" ");

            if (pos >= 0)
            {
                var dateTimeAsString = strMessage.Substring(0, pos);
                DateTime time;
                if (!DateTime.TryParseExact(dateTimeAsString, "HH:mm:ss.fffff", CultureInfo.InvariantCulture,
                                            DateTimeStyles.None, out time))
                {
                    time = DateTime.Now;
                }
                Time = time;
            }
            else
            {
                Time = DateTime.Now;
            }
            Message = strMessage;
        }

        #endregion

        #region Public

        public override string ToString()
        {
            return Message;
        }

        #endregion
    }
}


