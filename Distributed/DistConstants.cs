namespace HC.Core.Distributed
{
    public static class DistConstants
    {
        public const int WORKER_PUBLISH_GUI_TIME_SECS = 10;
        public const int JOB_ADVERTISE_TIME_SECS = 10;
        public const int PING_WORKER_TIME_SECS = 10;
        public const int ALIVE_WORKER_TIME_SECS = 300;
        public const int ALIVE_CONTROLLER_TIME_SECS = 40;
        public const int PING_CONTROLLER_TIME_SECS = 10;
        public static string m_strServerName;
        public static int m_intPort;
        public const int TIME_OUT_SECS = 60;
        public const int CALLBACK_SIZE = 100;
        public static bool IsServerMode { get; set; }
    }
}

