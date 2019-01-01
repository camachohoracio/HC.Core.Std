using System;

namespace HC.Core.Distributed.Controller
{
    public class JobDoneWrapper
    {
        public bool SucessDone { get; set; }
        public DateTime DateCreated { get; set; }

        public JobDoneWrapper()
        {
            DateCreated = DateTime.Now;
        }

    }
}