namespace HC.Core.Cache.SqLite
{
    public static class SqliteConstants
    {
        public const string KEY_COL_NAME = "KeyObjIndex";
        public static int DB_OPEN_CONNECTIONS = Config.GetDbOpenConnections();
        public static int DB_READ_THREAD_SIZE = Config.GetDbReadThreadSize();
        public const int DISPOSE_TIME_OUT_MILLSECS = 60*60*1000;
        public const int TIME_OUT = 60 * 10;
        public const int DB_QUEUE_CAPACITY = 30;
    }
}




