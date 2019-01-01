#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using HC.Core.Comunication.RequestResponseBased;
using HC.Core.Distributed.Controller;
using HC.Core.Distributed.Worker;
using HC.Core.DynamicCompilation;
using HC.Core.Events;
using HC.Core.Logging;
using HC.Core.Threading;

//using HC.Utils.Ui.LiveControllers;

#endregion

namespace HC.Core.Distributed
{
    public static class DistGuiHelper
    {
        public static void ResetGui()
        {
            ThreadWorker.StartTaskAsync(() =>
                                       {
                                           LiveGuiPublisherEvent.RemoveForm(
                                               EnumReqResp.Admin.ToString(),
                                               EnumDistributedGui.CloudWorkers.ToString());
                                           LiveGuiPublisherEvent.RemoveForm(
                                               EnumReqResp.Admin.ToString(),
                                               EnumDistributedGui.CloudControllers.ToString());
                                       });
        }

        public static void PublishJobLog(
            DistController distController,
            string strWorkerId,
            string strJobId,
            ASelfDescribingClass jobLog)
        {
            return;
            //string strKey = strWorkerId + strJobId;
            //LiveGuiPublisher.OwnInstance.PublishGui(
            //        EnumReqResp.Admin.ToString(),
            //        EnumDistributedGui.CloudControllers.ToString(),
            //        EnumDistributedGui.JobsDetails + "_" + distController.ControllerId,
            //        strKey,
            //        jobLog);
        }


        public static void PublishControllerGui(DistController distController)
        {
            try
            {
                while(!distController.IsReady)
                {
                    Thread.Sleep(1000);
                }
                PublishControllerStats(distController);
                PublishControllerWorkerStats(distController);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void PublishWorkerLog(
            DistWorker distWorker,
            string strLog,
            string strJobId)
        {
            LiveGuiPublisherEvent.PublishLog(
                    EnumReqResp.Admin.ToString(),
                    EnumDistributedGui.CloudWorkers.ToString(),
                    EnumDistributedGui.Logger + "_" + distWorker.WorkerId,
                    strJobId,
                    strLog);
        }

        public static void PublishWorkerStats(DistWorker distWorker)
        {
            try
            {
                var guiValues = new SelfDescribingClass();
                guiValues.SetClassName(EnumDistributedGui.WorkerGuiClass);
                guiValues.SetDateValue(
                    EnumDistributed.Time,
                    DateTime.Now);
                guiValues.SetIntValue(EnumDistributedGui.JobsCompleted, distWorker.JobsCompleted);
                guiValues.SetIntValue(EnumDistributedGui.Threads, distWorker.Threads);
                guiValues.SetIntValue(EnumDistributedGui.JobsInProgress, 
                    distWorker.JobsInProgress);
                
                LiveGuiPublisherEvent.PublishGrid(
                    EnumReqResp.Admin.ToString(),
                    EnumDistributedGui.CloudWorkers.ToString(),
                    "Stats_" + distWorker.WorkerId,
                    distWorker.WorkerId,
                    guiValues,
                    0,
                    true);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void PublishControllerWorkerStats(DistController distController)
        {
            try
            {
                foreach (KeyValuePair<string, string> kvp in distController.DistControllerToWorkerHeartBeat.WorkersStatus)
                {
                    string strWorkerId = kvp.Key;
                    var guiValues = new SelfDescribingClass();
                    guiValues.SetDateValue(
                        EnumDistributed.Time,
                        DateTime.Now);
                    guiValues.SetClassName(EnumDistributedGui.WorkersGuiClass);
                    guiValues.SetStrValue(EnumDistributed.WorkerId, strWorkerId);
                    guiValues.SetStrValue(EnumDistributedGui.State, kvp.Value);
                    int intJobsInProgress = (from n in distController.DistControllerJobPull.MapJobIdToWorkerId
                        where DistControllerJobLogger.GetWorkerId(n.Value).Equals(strWorkerId) select n).Count();
                    guiValues.SetIntValue(
                        EnumDistributedGui.JobsInProgress, 
                        intJobsInProgress);
                    int intJobsDone;
                    distController.DistControllerJobPull.MapWorkerToJobsDone.TryGetValue(
                        strWorkerId,
                        out intJobsDone);
                    guiValues.SetIntValue(
                        EnumDistributedGui.JobsDone,
                        intJobsDone);

                    DateTime lastPingTime;
                    
                    if(!distController.DistControllerToWorkerHeartBeat.WorkersPingTimes.TryGetValue(
                        strWorkerId, out lastPingTime))
                    {
                        lastPingTime = DateTime.Now;
                    }
                    guiValues.SetDateValue(EnumDistributedGui.LastPingTime, lastPingTime);

                    LiveGuiPublisherEvent.PublishGrid(
                        EnumReqResp.Admin.ToString(),
                        EnumDistributedGui.CloudControllers.ToString(),
                        EnumDistributedGui.Workers.ToString() + "_" +
                                            distController.ServerName + "_" +
                                            distController.GridTopic,
                        strWorkerId,
                        guiValues,
                        0,
                        false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void PublishControllerStats(DistController distController)
        {
            try
            {
                var guiValues = new SelfDescribingClass();
                guiValues.SetClassName(EnumDistributedGui.ControllerGuiClass);
                guiValues.SetDateValue(
                    EnumDistributed.Time,
                    DateTime.Now);
                guiValues.SetStrValue(
                    EnumDistributed.ControllerId,
                    distController.ControllerId);
                guiValues.SetIntValue(
                    EnumDistributedGui.JobsInProgress,
                    distController.JobsToDoMap.Count);
                guiValues.SetIntValue(
                    EnumDistributedGui.JobsAssigned,
                    distController.DistControllerJobPull.MapJobIdToWorkerId.Count);
                guiValues.SetIntValue(
                    EnumDistributedGui.JobsDone,
                    distController.JobsDone);
                guiValues.SetDblValue(
                    EnumDistributedGui.PingLatencySecs,
                    0);
                guiValues.SetDblValue(
                    EnumDistributedGui.NumWorkersConnected,
                    distController.DistControllerToWorkerHeartBeat.WorkersPingTimes.Count);

                string strControllerName =
                    Dns.GetHostName() + "_" +
                    distController.ControllerId;

                LiveGuiPublisherEvent.PublishGrid(
                    EnumReqResp.Admin.ToString(),
                    EnumDistributedGui.CloudControllers.ToString(),
                    EnumDistributedGui.Jobs + "_" + 
                        distController.ServerName + "_" +
                        distController.GridTopic,
                    strControllerName,
                    guiValues,
                    0,
                    true);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void PublishControllerLog(
            DistController distController,
            string strLog)
        {
            try
            {
                LiveGuiPublisherEvent.PublishLog(
                    EnumReqResp.Admin.ToString(),
                    EnumDistributedGui.CloudControllers.ToString(),
                    EnumDistributedGui.Logger.ToString() + "_" +
                        distController.ServerName + "_" +
                        distController.GridTopic,
                    Guid.NewGuid().ToString(),
                    strLog);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void PublishJobLogStatus(
            DistController distController, 
            ASelfDescribingClass jobLog, 
            string strLog)
        {
            if (jobLog == null)
            {
                return;
            }
            jobLog.SetStrValue(EnumDistributedGui.State, strLog);
            jobLog.SetDateValue(EnumDistributedGui.LastUpdateTime, DateTime.Now);
            PublishJobLog(
                distController,
                DistControllerJobLogger.GetWorkerId(jobLog),
                DistControllerJobLogger.GetJobId(jobLog),
                jobLog);
        }

        public static void PublishJobLogDone(
            DistController distController, 
            ASelfDescribingClass jobLog)
        {
            jobLog.SetStrValue(EnumDistributedGui.State, EnumDistributedGui.Done.ToString());
            jobLog.SetDateValue(EnumDistributedGui.LastUpdateTime, DateTime.Now);
            DateTime startTime;
            jobLog.TryGetDateValue(EnumDistributedGui.StartTime, out startTime);
            jobLog.SetDateValue(EnumDistributedGui.EndTime, DateTime.Now);
            jobLog.SetDblValue(EnumDistributedGui.TotalTimeMins, (DateTime.Now - startTime).TotalMinutes);

            PublishJobLog(
                distController,
                DistControllerJobLogger.GetWorkerId(jobLog),
                DistControllerJobLogger.GetJobId(jobLog),
                jobLog);
        }
    }
}

