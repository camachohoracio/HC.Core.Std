#region

using System;

#endregion

namespace HC.Core.Events
{
    public class UpdateServerEvent
    {
        public static event EventHandler<UpdateServerEventArgs> UpdateServer;

        public static void OnUpdateServer(
            object sender,
            string strServer,
            string strDatabase)
        {
            var e = new UpdateServerEventArgs();
            e.Server = strServer;
            e.Database = strDatabase;
            OnUpdateServer(sender, e);
        }

        public static void OnUpdateServer(object sender, UpdateServerEventArgs e)
        {
            if (UpdateServer != null)
            {
                UpdateServer(sender, e);
            }
        }
    }
}


