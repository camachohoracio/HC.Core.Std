#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HC.Core.ConfigClasses;
using HC.Core.DataStructures;
using HC.Core.Exceptions;
using HC.Core.Io;
using HC.Core.Io.KnownObjects;
using HC.Core.Io.KnownObjects.KnownTypes;
using HC.Core.Logging;
using HC.Core.Threading.ProducerConsumerQueues;
using HC.Core.Threading.ProducerConsumerQueues.Support;
using NUnit.Framework;

#endregion

namespace HC.Core.Cache.SqLite
{
    public static class SqliteCacheTests
    {
        private static int m_intItemCounter;
        private static int m_intCounter;
        private static int m_intRows;

        [SetUp]
        public static void SetupTest()
        {
            HCConfig.SetConfigDir(@"C:\HC\Config");
            AssemblyCache.Initialize();
            KnownTypesCache.LoadKnownTypes();
            const string strDllName = "SQLite.Interop.dll";
            const string strSourcePath = @"C:\HC\bin\AssemblyCache\x64\" +
                strDllName;
            string strDestPath = Path.Combine(
                FileHelper.GetCurrentAssemblyPath(),
                strDllName);
            if (!FileHelper.Exists(strDestPath))
            {
                File.Copy(
                    strSourcePath,
                    strDestPath);
            }
            //SqliteConstants.DB_OPEN_CONNECTIONS = 5;
            //SqliteConstants.DB_READ_THREAD_SIZE = 3;
        }

        [Test]
        public static void TestKeyList()
        {
            const string strBarFileName = "c:\\hc\\data\\dbtest\\bar\\testKeys_.db";

            const string strTableName = "Bar";
            const string strDefaultIndex = SqliteConstants.KEY_COL_NAME;
            var db = new SqliteCacheFullSchema<Bar>(
                    strBarFileName,
                    strTableName,
                    null,
                    strDefaultIndex);
            var bars = Bar.getBarList(100);
            var map = new Dictionary<string, Bar>();
            int i = 0;
            var keys = new List<string>();
            foreach (Bar bar in bars)
            {
                string strKey = "Bar_" + i++;
                keys.Add(strKey);
                map[strKey] = bar;
            }
            for (int j = keys.Count; j < 500; j++)
            {
                string strKey = "Bar_" + i++;
                keys.Add(strKey);
            }
            List<TaskWrapper> taks = db.Insert(map);
            TaskWrapper.WaitAll(taks);
            var keys2 = db.ContainsKeys(keys);
            Assert.IsTrue(keys2.Count == map.Count);

            Console.WriteLine(keys2);
        }

        [Test]
        public static void TestFooBar()
        {
            try
            {
                var lockObj = new Object();
                //const int intQueueCapacity = 50;
                var rng = new Random();
                //while (true)
                //{
                //    IterateQueue(lockObj, rng);
                //}
                var queue =
                    new ProducerConsumerQueue<StringWrapper>(20, 100,false,false);
                queue.SetAutoDisposeTasks(true);
                queue.OnWork += item => IterateQueue(lockObj, rng);

                //const int intToEnqueue = 100000;
                int intEnqueued = 0;
                while(true)
                {
                    queue.EnqueueTask(new StringWrapper());
                    intEnqueued++;
                }

                while (queue.TasksDone < intEnqueued)
                {
                    Thread.Sleep(100);
                }
                    
                while (true)
                {
                    Console.WriteLine("Finish...");
                    Thread.Sleep(1000);
                }

            }
            catch (Exception ex)
            {

                Logger.Log(ex);
            }
        }

        private static void IterateQueue(object lockObj, Random rng)
        {
            try
            {
                double dblRng;

                lock (lockObj)
                {
                    dblRng = rng.NextDouble();
                    m_intItemCounter++;
                }
                bool blnDoFoo = dblRng > 0.5;
                lock (lockObj)
                {
                    dblRng = rng.NextDouble();
                }
                var intImportValues = (int)(500 * dblRng);

                if (blnDoFoo)
                {
                    var intDbNumber = (int)(20 * dblRng);
                    TestFoo(dblRng, intImportValues, intDbNumber);
                }
                else
                {
                    var intDbNumber = (int)(10 * dblRng);
                    TestBar(dblRng, intImportValues, intDbNumber);
                }

                //Thread.sleep(1000);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void TestBar(

                double dblRng,
                int intImportValues,
                int intDbNumber)
        {

            try
            {
                string strBarFileName = "c:\\hc\\data\\dbtest\\bar\\sdbTestFullSchemaParallel_" +
                        intDbNumber + ".db";

                const string strTableName = "Bar";
                const string strDefaultIndex = SqliteConstants.KEY_COL_NAME;
                var db = new SqliteCacheFullSchema<Bar>(
                        strBarFileName,
                        strTableName,
                        null,
                        strDefaultIndex);

                List<Bar> list = Bar.getBarList(intImportValues);

                var map = new Dictionary<String, Bar>();
                var keyList = new List<String>();
                for (int i = 0; i < intImportValues; i++)
                {
                    Bar currItem = list[i];
                    currItem.m_string = currItem.m_string + "_" + m_intItemCounter;
                    String strKey = "key_" + i;
                    map.Add(strKey,
                            currItem);
                    keyList.Add(strKey);
                }
                list.Clear();

                List<TaskWrapper> tasks = db.Insert(map);
                TaskWrapper.WaitAll(tasks.ToArray());

                List<Bar> list2 = db.LoadDataFromKeys(keyList);
                list2.Sort((a, b) => a.m_j.CompareTo(b.m_j));

                int intCount2 = list2.Count;
                Console.WriteLine("[" + intCount2 + "] items already in db");

                for (int i = 0; i < intCount2; i++)
                {

                    Bar currItem = list2[i];
                    String strCurrKey = currItem.GetKey();
                    HCException.ThrowIfTrue(
                            !currItem.CompareSimple(map[strCurrKey]),
                            "Items not the same");
                }
                list2.Clear();
                map.Clear();

                bool blnRemoveItems = dblRng > 0.7;
                if (blnRemoveItems || intCount2 > 5000)
                {
                    db.Clear();
                    int intCount = db.Count;
                    if (intCount > 0)
                    {
                        Console.WriteLine("!!!!!!!!!!!count  [" + intCount + "]");
                    }
                }
                keyList.Clear();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void TestFoo(
                double dblRng,
                int intImportValues,
                int intDbNumber)
        {
            try
            {
                string strFooFileName = @"c:\hc\data\dbtest\foo\sdbTestFullSchemaParallel_" +
                        intDbNumber + ".db";

                const string strTableName = "Foo";
                const string strDefaultIndex = SqliteConstants.KEY_COL_NAME;

                using (var db = new SqliteCacheFullSchema<Foo>(
                        strFooFileName,
                        strTableName,
                        null,
                        strDefaultIndex))
                {

                    List<Foo> list = Foo.GetFooList(intImportValues);

                    var map = new Dictionary<String, Foo>();
                    //var keyList = new List<String>();
                    for (int i = 0; i < intImportValues; i++)
                    {
                        Foo currItem = list[i];
                        currItem.m_string = currItem.m_string + "_" + m_intItemCounter;
                        String strKey = "key_" + i;
                        map.Add(strKey,
                                currItem);
                        //keyList.Add(strKey);
                    }
                    list.Clear();

                    //var tasks = new List<Task>();

                    //for (int i = 0; i < 20; i++) {

                    List<TaskWrapper> currTasks = db.Insert(map);
                    TaskWrapper.WaitAll(currTasks.ToArray());
                    //}
                    //Task.WaitAll(tasks.ToArray());

                    List<Foo> list2 = db.LoadDataFromKeys(map.Keys.ToList());
                    int intCurrSize = list2.Count;
                    //List<Foo> list2 = db.LoadDataFromKeys(keyList);
                    //list2.Sort((a, b) => a.m_j.CompareTo(b.m_j));
                    Console.WriteLine("[" + intCurrSize + "] items already in db");
                    list2.Clear();
                    //for (int i = 0; i < list2.Count; i++)
                    //{
                    //    Foo currItem = list2[i];
                    //    String strCurrKey = currItem.GetKey();
                    //    HCException.ThrowIfTrue(
                    //            !currItem.CompareSimple(map[strCurrKey]),
                    //            "Items not the same");
                    //}
                    //list2.Clear();
                    map.Clear();

                    bool blnRemoveItems = dblRng > 0.7;
                    if (blnRemoveItems || intCurrSize > 5000)
                    {
                        db.Clear();
                        int intCount = db.Count;
                        if (intCount > 0)
                        {
                            Console.WriteLine("!!!!!!!!!!!!!!db count [" + intCount + "]");
                        }
                    }
                    //keyList.Clear();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        [Test]
        public static void TestParallelInsert()
        {

            try
            {
                const int intFooSize = 200;
                const string strFileName = "c:\\hc\\dbTestFullSchemaParallel.db";
                FileHelper.Delete(strFileName);

                const string strTableName = "Foo";
                string strDefaultIndex = SqliteConstants.KEY_COL_NAME;
                SqliteCacheFullSchema<Foo> fooDb = new SqliteCacheFullSchema<Foo>(
                        strFileName,
                        strTableName,
                        null,
                        strDefaultIndex);

                int intParallelSize = 100;
                object m_lockObj = new object();

                Parallel.For(0, intParallelSize, delegate(int i)
                    {
                        try
                        {
                            lock (m_lockObj)
                            {
                                m_intCounter++;
                                Console.WriteLine("Parallel import " +
                                        m_intCounter);
                            }

                            List<Foo> fooList = Foo.GetFooList(intFooSize);
                            Dictionary<string, Foo> map = new Dictionary<string, Foo>();

                            for (int j = 0; j < intFooSize; j++)
                            {

                                lock (m_lockObj)
                                {
                                    m_intItemCounter++;
                                }
                                Foo currFoo = fooList[j];
                                currFoo.m_string = currFoo.m_string + "_" + m_intItemCounter;
                                map.Add("key_" + j,
                                        currFoo);
                            }
                            List<TaskWrapper> task = fooDb.Insert(map);
                            Console.WriteLine("got tasks0...");
                            TaskWrapper.WaitAll(task.ToArray());

                            lock (m_lockObj)
                            {
                                m_intCounter--;
                                m_intRows += intFooSize;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex);
                        }
                    });

                List<String> keysToLoad = new List<String>();
                for (int i = 0; i < intFooSize; i++)
                {
                    String strCurrKey = "key_" + i;
                    keysToLoad.Add(strCurrKey);
                    bool blnContainsKey = fooDb.ContainsKey(strCurrKey);
                    Assert.IsTrue(blnContainsKey, "Key not found");
                    List<Foo> fooArr = fooDb.LoadDataFromKey(strCurrKey);

                    if (fooArr.Count != intParallelSize)
                    {
                        throw new Exception("invalid number of rows " + i + "=" + fooArr.Count);
                    }

                    Assert.IsTrue(fooArr.Count == intParallelSize,
                        "invalid number of rows " + i + "=" + fooArr.Count);
                }

                string strQuery = "SELECT count(*) FROM FOO";

                var intRowCount = (int)fooDb.ExecuteScalar<long>(strQuery);
                Assert.IsTrue(intRowCount == m_intRows, "invalid number of rows");


                fooDb.Delete(keysToLoad);
                intRowCount = (int)fooDb.ExecuteScalar<long>(strQuery);
                Assert.IsTrue(intRowCount == 0, "invalid number of rows");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Assert.IsTrue(false, "Exception occured");
            }
        }

        [Test]
        public static void TestSchemaValues()
        {

            try
            {
                const string strFileName = "c:\\hc\\dbTestFullSchema.db";

                FileHelper.Delete(strFileName);

                const string strTableName = "Foo";
                const string strDefaultIndex = SqliteConstants.KEY_COL_NAME;
                var fooDb = new SqliteCacheFullSchema<Foo>(
                        strFileName,
                        strTableName,
                        null,
                        strDefaultIndex);

                Assert.IsTrue(FileHelper.Exists(strFileName), "File not found");

                string strQuery = "SELECT EXISTS(SELECT name FROM sqlite_master WHERE name='" +
                        strTableName + "')";

                int intExistsTable = (int)fooDb.ExecuteScalar<long>(strQuery);
                Assert.IsTrue(intExistsTable == 1, "table not found");

                string strIndex = strTableName + "_" + strDefaultIndex + "_INDEX";
                strQuery = "SELECT EXISTS(SELECT name FROM sqlite_master WHERE name='" +
                        strIndex + "')";

                int intExistsIndex = (int)fooDb.ExecuteScalar<long>(strQuery);
                Assert.IsTrue(intExistsIndex == 1, "table not found");

                int intFooSize = 20000;
                List<Foo> fooList = Foo.GetFooList(intFooSize);
                Dictionary<string, Foo> map = new Dictionary<string, Foo>();

                for (int i = 0; i < intFooSize; i++)
                {

                    map.Add("key_" + i,
                            fooList[i]);
                }

                TaskWrapper.WaitAll(fooDb.Insert(map).ToArray());

                strQuery = "SELECT count(*) FROM FOO";

                int intRowCount = (int)fooDb.ExecuteScalar<long>(strQuery);
                Assert.IsTrue(intRowCount == intFooSize, "invalid number of rows");

                string strKey = "key_0";
                bool blnContainsKey = fooDb.ContainsKey(strKey);
                Assert.IsTrue(blnContainsKey, "key not found");

                List<Foo> data = fooDb.LoadDataFromKey(strKey);
                bool blnEquals = fooList[0].CompareSimple(data[0]);
                Assert.IsTrue(blnEquals, "rows not equal");

                List<String> keysToLoad = new List<String>();

                for (int i = 0; i < intFooSize / 2; i++)
                {
                    keysToLoad.Add("key_" + i);
                }
                data = fooDb.LoadDataFromKeys(keysToLoad);
                data.Sort((a, b) => a.m_j.CompareTo(b.m_j));

                List<String> keysToDelete = new List<String>();
                int intSampleSize = Math.Min(500, intFooSize / 2);

                for (int i = 0; i < intSampleSize; i++)
                {
                    String strCurrKey = keysToLoad[i];
                    blnContainsKey = fooDb.ContainsKey(strCurrKey);
                    Assert.IsTrue(blnContainsKey, "key not found");
                    blnEquals = fooList[i].CompareSimple(data[i]);
                    Assert.IsTrue(blnEquals, "rows not equal");
                    keysToDelete.Add(strCurrKey);
                }

                fooDb.Delete(strKey);
                blnContainsKey = fooDb.ContainsKey(strKey);
                Assert.IsTrue(!blnContainsKey, "key found");

                fooDb.Delete(keysToDelete);
                data = fooDb.LoadDataFromKeys(keysToDelete);
                Assert.IsTrue(data == null || data.Count == 0, "Rows found");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Assert.IsTrue(false, "Exception occured");
            }
        }
    }
}
