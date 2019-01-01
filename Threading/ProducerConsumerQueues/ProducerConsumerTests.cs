#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using HC.Core.DataStructures;
using HC.Core.Helpers;
using HC.Core.Threading.ProducerConsumerQueues.Support;

#endregion

namespace HC.Core.Threading.ProducerConsumerQueues
{
    public class ProducerConsumerTests
    {
        #region Members

        private IThreadedQueue<TestWorker> m_prducerConsumerQueue;
        private int m_tasksInProgress;
        private int m_totalQueuedTasks;

        #endregion

        public static void DoTest()
        {
            //
            // test 1
            //
            const int intTaskLength = 100;
            //TestParallelFor(intTaskLength);

            //
            // tesk 2
            //
            TestPipeLine(intTaskLength);

            Console.ReadKey();
        }

        private static void TestPipeLine(int intTaskLength)
        {
            const int intTreadSize = 1000;

            var tasks = new List<TaskWrapper>();
            DateTime start = DateTime.Now;
            var producerConsumerQueue =
                new ProducerConsumerQueueLite<IntWrapper>(
                    intTreadSize);
            producerConsumerQueue.OnWork +=
                OnWorkCall;

            for (int i = 0; i < intTaskLength; i++)
            {
                var task = producerConsumerQueue.EnqueueTask(new IntWrapper {Int = i});
                tasks.Add(task);
            }

            TaskWrapper.WaitAll(tasks.ToArray());
            DateTime end = DateTime.Now;
            PrintToScreen.WriteLine(@"Done task 2 in " + (end - start).TotalSeconds + @"secs");
        }

        public void TestQueueWithWorker(
            int intTaskLenght,
            int intThreadSize,
            int intQueueSize,
            int intSleepSeconds,
            bool blnIsLifo,
            bool blnDropItems)
        {
            var tasks = new List<TaskWrapper>();
            DateTime start = DateTime.Now;
            m_prducerConsumerQueue = null;
                //new ProducerConsumerQueuePooled<TestWorker>(
                //    intThreadSize,
                //    intQueueSize,
                //    blnIsLifo,
                //    blnDropItems);

            m_prducerConsumerQueue.OnWork +=
                OnWorkCallFromWorker;

            //Parallel.For(0, intTaskLenght, delegate(int i)
            for(int i = 0; i<intTaskLenght; i++)
            {
                var testWorker = new TestWorker(intSleepSeconds);
                testWorker.Resource = "job " + i;

                var task = m_prducerConsumerQueue.EnqueueTask(testWorker);

                lock (tasks)
                {
                    tasks.Add(task);
                }
            }
            //);

            TaskWrapper.WaitAll(tasks.ToArray());
            DateTime end = DateTime.Now;
            TimeSpan elapsed = end - start;
            PrintToScreen.WriteLine("Done task 2 in " + elapsed.TotalSeconds + " sec.");
            PrintToScreen.WriteLine("Total queued tasks = " + m_totalQueuedTasks);
            Debugger.Break();
        }


        public void TestDropQueue(
            int intTaskLenght,
            int intSleepSeconds)
        {
            var tasks = new List<TaskWrapper>();
            DateTime start = DateTime.Now;
            m_prducerConsumerQueue = null;
                //new WaitHandle<TestWorker>(
                //    1,
                //    2,
                //    false,
                //    true);

            m_prducerConsumerQueue.OnWork +=
                OnWorkCallFromWorker;

            //Parallel.For(0, intTaskLenght, delegate(int i)
            for (int i = 0; i < intTaskLenght; i++)
            {
                var testWorker = new TestWorker(intSleepSeconds);
                testWorker.Resource = "job " + i;

                var task = m_prducerConsumerQueue.EnqueueTask(testWorker);

                lock (tasks)
                {
                    tasks.Add(task);
                }
            }
            //);

            TaskWrapper.WaitAll(tasks.ToArray());
            DateTime end = DateTime.Now;
            TimeSpan elapsed = end - start;
            PrintToScreen.WriteLine("Done task 2 in " + elapsed.TotalSeconds + " sec.");
            PrintToScreen.WriteLine("Total queued tasks = " + m_totalQueuedTasks);
            Debugger.Break();
        }
        private void OnWorkCallFromWorker(TestWorker testWorker)
        {
            PrintToScreen.WriteLine(@"Start job = " +  testWorker.Resource);
            PrintToScreen.WriteLine("Queue size before = " + m_prducerConsumerQueue.QueueSize);
            Interlocked.Increment(ref m_tasksInProgress);
            PrintToScreen.WriteLine("Tasks in progress before = " + m_tasksInProgress);
            testWorker.Work();
            PrintToScreen.WriteLine("End job = " + testWorker.Resource);
            PrintToScreen.WriteLine("Queue size after = " + m_tasksInProgress);
            Interlocked.Decrement(ref m_tasksInProgress);
            Interlocked.Increment(ref m_totalQueuedTasks);
            PrintToScreen.WriteLine("Tasks in progress after = " + m_tasksInProgress);
            PrintToScreen.WriteLine("Total queued tasks = " + m_totalQueuedTasks);
        }

        //private static void TestParallelFor(int intTaskLength)
        //{
        //    DateTime start = DateTime.Now;

        //    //
        //    // unlimite thread size
        //    //
        //    Parallel.For(0, intTaskLength, OnWorkCall);

        //    DateTime end = DateTime.Now;
        //    PrintToScreen.WriteLine(@"Done task 1 in " + (end - start).TotalSeconds + @"secs");
        //}

        private static void OnWorkCall(IntWrapper intState)
        {
            PrintToScreen.WriteLine(@"Start job = "+ intState);
            Thread.Sleep(1000);
            PrintToScreen.WriteLine(@"End job = " + intState);
        }
    }
}



