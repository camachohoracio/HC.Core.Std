using System;
using HC.Core.DynamicCompilation;

namespace HC.Core.Distributed.Controller
{
    public static class DistControllerJobLogger
    {
        public static string GetWorkerId(ASelfDescribingClass jobLog)
        {
            if(jobLog == null)
            {
                return string.Empty;
            }
            string strWorkerId;
            jobLog.TryGetStrValue(
                EnumDistributedGui.WorkerId,
                out strWorkerId);
            return strWorkerId;
        }

        public static SelfDescribingClass GetJobLog(
            string strWorkerId, 
            string strJobId,
            string strPullId)
        {
            var jobLog = new SelfDescribingClass();
            jobLog.SetClassName(EnumDistributedGui.JobsInProgress + typeof(DistControllerJobLogger).Name);
            jobLog.SetStrValue(EnumDistributedGui.WorkerId, strWorkerId);
            jobLog.SetStrValue(EnumDistributedGui.JobId, strJobId);
            jobLog.SetStrValue(EnumDistributedGui.State, "JobsInProgress");
            jobLog.SetDateValue(EnumDistributedGui.StartTime, DateTime.Now);
            jobLog.SetDateValue(EnumDistributedGui.LastUpdateTime, DateTime.Now);
            jobLog.SetDateValue(EnumDistributedGui.EndTime, new DateTime());
            jobLog.SetIntValue(EnumDistributedGui.TotalTimeMins, 0);
            jobLog.SetStrValue(EnumDistributed.PullId, strPullId);
            return jobLog;
        }

        public static string GetJobId(ASelfDescribingClass jobLog)
        {
            if (jobLog == null)
            {
                return string.Empty;
            }
            string strJobId;
            jobLog.TryGetStrValue(
                EnumDistributedGui.JobId,
                out strJobId);
            return strJobId;
        }
    }
}
