#region

using System;

#endregion

namespace HC.Core.Events
{
    public class UpdateServerEventArgs : EventArgs
    {
        public string Server { get; set; }
        public string Database { get; set; }
    }
}


