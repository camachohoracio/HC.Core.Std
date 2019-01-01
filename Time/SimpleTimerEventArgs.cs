using System;

namespace HC.Core.Time
{
    class SimpleTimerEventArgs : EventArgs
    {
        public string Id { get; private set; }

        public SimpleTimerEventArgs(string id)
        {
            Id = id;
        }

    }
}


