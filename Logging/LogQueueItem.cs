using System;
using System.Collections.Generic;

namespace HC.Core.Logging
{
    public class LogQueueItem : IDisposable
    {
        public void Dispose()
        {
        }

        public Exception ex { get; set; }

        public bool blnSendMail { get; set; }

        public bool blnPublishGui { get; set; }

        public bool blnPublishTopic { get; set; }

        public string Title { get; set; }

        public string Messge { get; set; }

        public string Sender { get; set; }

        public bool IsHtml { get; set; }

        public List<string> ImageFileNames { get; set; }

        public List<string> To { get; set; }
    }
}
