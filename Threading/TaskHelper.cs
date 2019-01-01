#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HC.Core.Logging;
using HC.Core.Threading.ProducerConsumerQueues.Support;

#endregion

namespace HC.Core.Threading
{
    public static class TaskHelper
    {
        public static void WaitAll(List<TaskWrapper> taskList)
        {
            try
            {
                taskList = (from task in taskList
                            where task != null &&
                            task.Task != null &&
                                  !task.Task.IsCompleted &&
                                  !task.Task.IsCanceled &&
                                  !task.Task.IsFaulted
                            select task).ToList();

                bool blnReArray = false;
                TaskWrapper[] taskArr = taskList.ToArray();
                while (taskList.Count > 0)
                {
                    if (blnReArray)
                    {
                        taskArr = taskList.ToArray();
                    }
                    blnReArray = false;
                    for (int i = 0; i < taskArr.Length; i++)
                    {
                        Task task = taskArr[i].Task;
                        if (taskArr[i].IsDisposed ||
                            task == null ||
                            task.IsCompleted ||
                            task.IsCanceled ||
                            task.IsFaulted)
                        {
                            blnReArray = true;
                            taskList.Remove(taskArr[i]);
                        }
                        else
                        {
                            //
                            // no need iterating more
                            //
                            break;
                        }
                    }
                    Thread.Sleep(10);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}
