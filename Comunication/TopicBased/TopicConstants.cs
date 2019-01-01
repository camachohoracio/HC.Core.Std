#region

using System;

#endregion

namespace HC.Core.Comunication.TopicBased
{
    public static class TopicConstants
    {
        public const int SUBSCRIBER_POLL_WAIT_TIME_OUT = 4000;
        public const int PUBLISHER_DEFAULT_PORT = 3556;
        public const int PUBLISHER_HEART_BEAT_PORT = 4656;
        public const int SUBSCRIBER_DEFAULT_PORT = 6789;
        public const int SUBSCRIBER_HEART_BEAT_PORT = 7889;
        public const int NUM_TOPIC_CONNECTIONS = 3;
        public static readonly TimeSpan TIME_OUT = new TimeSpan(0, 0, 5, 0);
    }
}