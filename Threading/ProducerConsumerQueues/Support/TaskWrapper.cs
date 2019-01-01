using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HC.Core.Logging;

namespace HC.Core.Threading.ProducerConsumerQueues.Support
{
    public delegate void DisposeDel();

    public class TaskWrapper : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public Task Task { get; set; }
        public IDisposable Item { get; set; }
        public DisposeDel DisposeDel { get; set; }

        public TaskWrapper(Task task, IDisposable state)
        {
            Task = task;
            Item = state;
        }


        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            try
            {
                if (DisposeDel != null)
                {
                    DisposeDel();
                    DisposeDel = null;
                }
                var t = Task;
                if (t != null)
                {
                    if (t.IsCompleted)
                    {
                        t.Dispose();
                    }
                    Task = null;
                }
                if (Item != null)
                {
                    Item.Dispose();
                    Item = null;
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            finally
            {
                IsDisposed = true;
            }
        }

        public static void WaitAll(
            TaskWrapper[] toArray)
        {
            WaitAll(toArray, 0);
        }

        public static int WaitAll(
            TaskWrapper[] toArray,
            int intTimeOutMils,
            bool blnWaitAny = false)
        {
            Task[] taskArr = null;
            try
            {
                if (toArray == null ||
                    toArray.Length == 0)
                {
                    return -1;
                }
                List<TaskWrapper> taskList = toArray.ToList();
                taskArr = (from n in taskList
                               where
                                   n != null &&
                                   n.Task != null &&
                                   !n.IsDisposed
                               select n.Task).ToArray();
                taskArr = (from n in taskArr
                           where
                               n != null
                           select n).ToArray();
                if (intTimeOutMils > 0)
                {
                    if (blnWaitAny)
                    {
                        return Task.WaitAny(
                            taskArr);
                    }
                    else
                    {
                        Task.WaitAll(taskArr, intTimeOutMils);
                    }
                }
                else
                {
                    if (blnWaitAny)
                    {
                        return Task.WaitAny(
                            taskArr);
                    }
                    else
                    {
                        Task.WaitAll(
                            taskArr);
                    }
                }
            }
            catch(Exception ex)
            {
                BruteForceWaiAll(intTimeOutMils, taskArr);
                Logger.Log(ex);
            }
            return -1;
        }

        private static void BruteForceWaiAll(int intTimeOutMils, Task[] taskArr)
        {
            try
            {
                if (taskArr != null)
                {
                    for (int i = 0; i < taskArr.Length; i++)
                    {
                        try
                        {
                            var task = taskArr[i];
                            if (task != null)
                            {
                                if (intTimeOutMils > 0)
                                {
                                    task.Wait(intTimeOutMils);
                                }
                                else
                                {
                                    task.Wait();
                                }
                            }
                        }
                        catch (Exception ex2)
                        {
                            Logger.Log(ex2);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void Wait(
            int intTimer = 0)
        {
            if (IsDisposed)
            {
                return;
            }

            if(Task != null)
            {
                if (intTimer == 0)
                {
                    Task.Wait();
                }
                else
                {
                    Task.Wait(intTimer);
                }
            }
        }

        public static void WaitAll(
            List<TaskWrapper> tasks)
        {
            WaitAll(tasks, 0);
        }


        public static void WaitAll(
            List<TaskWrapper> tasks,
            int intTimeOutMils)
        {
            WaitAll(tasks.ToArray(), intTimeOutMils);
        }

        public static void DisposeAll(List<TaskWrapper> taskList)
        {
            foreach (TaskWrapper taskWrapper in taskList)
            {
                taskWrapper.Dispose();
            }
        }
    }
}