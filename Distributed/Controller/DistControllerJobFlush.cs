using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HC.Core.DynamicCompilation;
using HC.Core.Logging;

namespace HC.Core.Distributed.Controller
{
    public static class DistControllerJobFlush
    {
        public static SelfDescribingClass DoFlushJobs(
            DistController distController,
            ASelfDescribingClass paramsClass,
            string strJobId,
            DistControllerJobPull distControllerJobPull,
            ConcurrentDictionary<string, ASelfDescribingClass> jobsInProgressMap)
        {
            try
            {
                string strClassName;
                paramsClass.TryGetStrValue(
                    EnumCalcCols.ClassName,
                    out strClassName);
                KeyValuePair<string, ASelfDescribingClass>[] jobsInProgressArr;
                int intFlushedJobs = 0;
                lock (distControllerJobPull.JobsInProgressLock)
                {
                    jobsInProgressArr = jobsInProgressMap.ToArray();
                }
                for (int i = 0; i < jobsInProgressArr.Length; i++)
                {
                    bool blnDoRemove = false;
                    ASelfDescribingClass currParams = jobsInProgressArr[i].Value;
                    if (!string.IsNullOrEmpty(strClassName))
                    {
                        string strCurrClassName;
                        if (currParams.TryGetStrValue(EnumCalcCols.ClassName,
                                                      out strCurrClassName) &&
                            !string.IsNullOrEmpty(strCurrClassName))
                        {
                            if (strCurrClassName.Equals(strClassName))
                            {
                                blnDoRemove = true;
                            }
                        }
                    }
                    else
                    {
                        blnDoRemove = true;
                    }
                    if (blnDoRemove)
                    {
                        lock (distControllerJobPull.JobsInProgressLock)
                        {
                            ASelfDescribingClass dummy;
                            jobsInProgressMap.TryRemove(
                                jobsInProgressArr[i].Key,
                                out dummy);
                            ASelfDescribingClass jobLog;
                            distControllerJobPull.MapJobIdToWorkerId.TryRemove(
                                strJobId, out jobLog);
                            DistGuiHelper.PublishJobLogStatus(
                                distController,
                                jobLog,
                                "JobFlushed");
                            intFlushedJobs++;
                        }
                    }
                }

                var resultTsEv = new SelfDescribingClass();
                resultTsEv.SetClassName(EnumCalcCols.FlushJobs.ToString());
                resultTsEv.SetDateValue("Time", DateTime.Now);
                resultTsEv.SetObjValueToDict(
                    EnumCalcCols.Result,
                    "Successfull flushed [" + intFlushedJobs + "] jobs");
                return resultTsEv;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                var resultTsEv = new SelfDescribingClass();
                resultTsEv.SetClassName(EnumCalcCols.FlushJobs.ToString());
                resultTsEv.SetDateValue("Time", DateTime.Now);
                resultTsEv.SetObjValueToDict(
                    EnumCalcCols.Result,
                    "failed flushed jobs");
                return resultTsEv;
            }
        }
    }
}

