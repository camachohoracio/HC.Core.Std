#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using HC.Core.Cache;
using HC.Core.Comunication;
using HC.Core.Comunication.RequestResponseBased;
using HC.Core.Comunication.RequestResponseBased.Client;
using HC.Core.Comunication.RequestResponseBased.Server.RequestHub;
using HC.Core.ConfigClasses;
using HC.Core.Distributed.Controller;
using HC.Core.Distributed.Worker;
using HC.Core.DynamicCompilation;
using HC.Core.Events;
using HC.Core.Exceptions;
using HC.Core.Io;
using HC.Core.Logging;
using HC.Core.Reflection;
using HC.Core.Threading;

#endregion

namespace HC.Core.Distributed
{
    public class ExecuteMethodCalc : ITsCalcWorker
    {
        #region Members

        private static readonly ConcurrentDictionary<string, ProviderCounterItem> m_methodCounter =
            new ConcurrentDictionary<string, ProviderCounterItem>();
        private static readonly ConcurrentDictionary<string, object> m_methodChanges =
            new ConcurrentDictionary<string, object>();
        private static readonly object m_counterLock = new object();
        private static ThreadWorker m_logWorker;

        #endregion

        #region Properties

        public string Resource { get; set; }
        public List<ITsEvent> TsEvents { get; set; }
        public CacheDictionary<string, List<ITsEvent>> Cache { get; set; }
        public bool DoCache { get; set; }
        public ASelfDescribingClass Params { get; set; }

        #endregion

        #region Members

        private static readonly HashSet<string> m_loadedAssemblies;
        private static readonly string m_strDefaultServer;
        private static int m_intRequestCounter;
        private static int m_intCalcCounter;
        private static bool m_blnIsLogWorkerLoaded;
        private static readonly object m_logWorkerLock = new object();

        #endregion

        #region Constructors

        static ExecuteMethodCalc()
        {
            try
            {
                m_strDefaultServer = Core.Config.GetTopicServerName();
                List<Assembly> loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
                List<string> assembliesNames = (from n in loadedAssemblies select n.GetName().Name.ToLower()).ToList();
                m_loadedAssemblies = new HashSet<string>(assembliesNames);
                LoadLogWorker();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public ExecuteMethodCalc()
        {
            TsEvents = new List<ITsEvent>();
        }

        #endregion

        private static void LoadLogWorker()
        {
            if (m_blnIsLogWorkerLoaded)
            {
                return;
            }
            lock (m_logWorkerLock)
            {
                if (m_blnIsLogWorkerLoaded)
                {
                    return;
                }
                m_blnIsLogWorkerLoaded = true;
                m_logWorker = new ThreadWorker();
                m_logWorker.OnExecute += () =>
                {
                    while (true)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(DistConstants.m_strServerName))
                            {
                                var methodCounterArr = m_methodChanges.ToArray();
                                for (int i = 0; i < methodCounterArr.Length; i++)
                                {
                                    var kvp = methodCounterArr[i];
                                    ProviderCounterItem providerCounterItem;
                                    if (!m_methodCounter.TryGetValue(kvp.Key, out providerCounterItem))
                                    {
                                        continue;
                                    }
                                    var selfDescrClass = new SelfDescribingClass();
                                    selfDescrClass.SetClassName(typeof(ExecuteMethodCalc).Name + "_queues");
                                    selfDescrClass.SetStrValue("Method", kvp.Key);
                                    selfDescrClass.SetIntValue("Todo", providerCounterItem.Todo);
                                    selfDescrClass.SetIntValue("Done", providerCounterItem.Done);
                                    selfDescrClass.SetDateValue("Time", DateTime.Now);

                                    LiveGuiPublisherEvent.PublishGrid(
                                        EnumReqResp.Admin.ToString(),
                                        EnumDistributedGui.CloudWorkers.ToString() + "_" +
                                        DistConstants.m_strServerName + "_" +
                                        DistConstants.m_intPort + "_" +
                                        HCConfig.ClientUniqueName,
                                        "MethodCalcs",
                                        kvp.Key,
                                        selfDescrClass,
                                        0,
                                        false);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex);
                        }
                        m_methodChanges.Clear();
                        Thread.Sleep(5000);
                    }
                };
                m_logWorker.Work();
            }
        }

        #region Public

        public void Work()
        {
            try
            {
                object result = null;
                string strError;
                try
                {
                    result = RunMethodLocallyAndLog(Params, out strError);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    strError = ex.Message + " " +
                               ex.StackTrace;
                }

                var resultTsEv = new SelfDescribingTsEvent(GetType().Name)
                {
                    Time = DateTime.Now
                };
                if (!string.IsNullOrEmpty(strError))
                {
                    resultTsEv.SetStrValue(
                        EnumCalcCols.Error,
                        strError);
                }
                //if (result == null)
                //{
                //    throw new HCException("Null result ["  +
                //        Params.ToString() + "]. Error [" + 
                //        strError + "]");
                //}
                resultTsEv.SetObjValueToDict(
                    EnumCalcCols.Result,
                    result);
                TsEvents.Add(resultTsEv);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static object RunMethodLocallyAndLog(
            ASelfDescribingClass selfDescribingClass,
            out string strError)
        {
            string strOldTitle = string.Empty;
            try
            {
                LoadLogWorker();
                string strClassName =
                    selfDescribingClass.GetStrValue(
                        EnumCalcCols.MethodClassName);
                string strAssemblyName =
                    selfDescribingClass.GetStrValue(
                        EnumCalcCols.MethodAssemblyName).ToLower();

                string strMethodName = selfDescribingClass.GetStrValue(EnumCalcCols.MethodName);
                string strMethodDescr = strAssemblyName + "." +
                                        strClassName + "." +
                                        strMethodName;

                try
                {
                    if (NetworkHelper.IsADistWorkerConnected)
                    {
                        strOldTitle = Console.Title;
                        Console.Title = strOldTitle +
                            "|Method:" + strClassName.Split('.').Last() + "." +
                                        strMethodName + "(.)";
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }

                lock (m_counterLock)
                {
                    ProviderCounterItem providerCounterItem;
                    if (!m_methodCounter.TryGetValue(strMethodDescr, out providerCounterItem))
                    {
                        providerCounterItem = new ProviderCounterItem();
                        m_methodCounter[strMethodDescr] = providerCounterItem;
                    }
                    providerCounterItem.Todo++;
                    m_methodChanges[strMethodDescr] = null;
                }

                var result = RunMethodLocally(selfDescribingClass, out strError);

                lock (m_counterLock)
                {
                    ProviderCounterItem providerCounterItem;
                    if (!m_methodCounter.TryGetValue(strMethodDescr, out providerCounterItem))
                    {
                        providerCounterItem = new ProviderCounterItem();
                        m_methodCounter[strMethodDescr] = providerCounterItem;
                    }
                    providerCounterItem.Todo--;
                    providerCounterItem.Done++;
                    m_methodChanges[strMethodDescr] = null;
                }

                return result;
            }
            catch (Exception ex)
            {
                strError = ex.Message + " " + ex.StackTrace;
                Logger.Log(ex);
            }
            finally
            {
                if (NetworkHelper.IsADistWorkerConnected)
                {
                    Console.Title = strOldTitle;
                }
            }
            return null;
        }

        public static object RunMethodLocally(
            ASelfDescribingClass methodParams,
            out string strError)
        {
            MethodInfo methodInfo = null;
            try
            {
                //DateTime logTime = DateTime.Now;
                string strClassName =
                    methodParams.GetStrValue(
                        EnumCalcCols.MethodClassName);
                string strAssemblyName =
                    methodParams.GetStrValue(
                        EnumCalcCols.MethodAssemblyName).ToLower();
                if (!m_loadedAssemblies.Contains(strAssemblyName))
                {
                    string strAssemblyFileName = methodParams.GetStrValue(
                        EnumCalcCols.AssemblyFileName);
                    if (FileHelper.Exists(strAssemblyFileName))
                    {
                        Assembly.LoadFrom(strAssemblyFileName);
                    }
                    m_loadedAssemblies.Add(strAssemblyName);
                }

                string strTypeDescr = strClassName + "," +
                                      strAssemblyName;

                Type classType = Type.GetType(
                    strTypeDescr);

                if (classType == null)
                {
                    throw new HCException("Type null: " +
                                          strTypeDescr);
                }

                string strMethodName = methodParams.GetStrValue(EnumCalcCols.MethodName);
                //string strMessage = "*Running method[" +
                //                    strTypeDescr + "." + strMethodName + "]...";
                //Console.WriteLine(strMessage);
                //Logger.Log(strMessage);

                var paramsList = (List<object>)methodParams.GetObjValue(EnumCalcCols.ParamsList);
                methodInfo = GetMethodInfo(classType, strMethodName, paramsList);

                if (methodInfo == null)
                {
                    throw new HCException("No method found for " + classType.Name);
                }

                object result = methodInfo.Invoke(null, paramsList.ToArray());

                if (result == null)
                {
                    if (methodInfo.ReturnType != typeof(void))
                    {
                        result = ReflectorCache.GetReflector(methodInfo.ReturnType).CreateInstance();
                    }
                }
                //
                // tidy up
                //
                paramsList.Clear();
                //strMessage = "*End method[" +
                //             strTypeDescr + "." + strMethodName + "]. Time = " +
                //             (DateTime.Now - logTime).TotalSeconds;
                //Console.WriteLine(strMessage);
                //Logger.Log(strMessage);
                strError = string.Empty;
                return result;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Logger.Log(ex.InnerException);

                strError = ex.Message + "||" +
                    (ex.InnerException != null ?
                        ex.InnerException.Message : string.Empty);

                try
                {
                    if (methodInfo != null)
                    {
                        object result = ReflectorCache.GetReflector(
                            methodInfo.ReturnType).CreateInstance();
                        return result;
                    }
                    return null;
                }
                catch (Exception ex2)
                {
                    Logger.Log(ex2);
                }
                return null;
            }
        }

        public static object RunMethodDistributedViaService(
            Type classType,
            string strMethodName,
            List<object> parameters,
            bool blnForceRemote = false)
        {
            try
            {
                return RunMethodDistributedViaService(
                    m_strDefaultServer,
                    classType,
                    strMethodName,
                    parameters,
                    blnForceRemote);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static object RunMethodDistributedViaService(
            string strServer,
            Type classType,
            string strMethodName,
            List<object> parameters,
            bool blnForceRemote = false)
        {
            try
            {
                var logTime = DateTime.Now;
                ASelfDescribingClass calcParams = GetCalcParams(
                    parameters,
                    strMethodName,
                    classType);

                if ((NetworkHelper.IsADistWorkerConnected ||
                    DistConstants.IsServerMode) &&
                    !blnForceRemote)
                {
                    //
                    // run method locally
                    //
                    string strError;
                    object methodResult = RunMethodLocallyAndLog(calcParams,
                        out strError);
                    if (!string.IsNullOrEmpty(strError))
                    {
                        throw new HCException(strError);
                    }
                    return methodResult;
                }

                //
                // run method remotely
                //
                Interlocked.Increment(ref m_intRequestCounter);
                string strRequestId =
                    HCConfig.ClientUniqueName + "_" +
                    strMethodName + "_" +
                    m_intRequestCounter + "_" +
                    Guid.NewGuid();
                var request = new RequestDataMessage
                {
                    CallbackSize = DistConstants.CALLBACK_SIZE,
                    Id = strRequestId,
                    IsAsync = true,
                    Request = calcParams,
                    RequestType = EnumRequestType.Calc,
                };

                ARequestResponseClient connection;
                //if(QuickTsDataProvider.IntradayProviders.Contains(
                //    classType.Name))
                //{
                //    connection = QuickTsDataProvider.GetIntradayConnection();
                //}
                //else // let only one controller work for distribution
                {
                    connection = ARequestResponseClient.GetDefaultConnection();
                }

                List<object> tsEvents = connection.SendRequestAndGetResponse(
                    request,
                    DistConstants.TIME_OUT_SECS);

                if (tsEvents.Count == 0)
                {
                    throw new HCException("Empty result");
                }

                var result = tsEvents[0] as ASelfDescribingClass;
                if (result != null)
                {
                    string strMessage = "Done distMethod [" +
                                        classType.Name + "." + strMethodName + "(.)] in [" +
                                        Math.Round((DateTime.Now - logTime).TotalMinutes, 2) + "]mins";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);
                    object tsEventsObj;
                    List<ITsEvent> tsEventsCasted;
                    ASelfDescribingClass resultItem = null;
                    if (result.TryGetObjValue("TsEvents", out tsEventsObj) &&
                        tsEventsObj != null &&
                        (tsEventsCasted = tsEventsObj as List<ITsEvent>) != null &&
                        tsEventsCasted.Count > 0 &&
                        (resultItem = tsEventsCasted[0] as ASelfDescribingClass) != null)
                    {
                        object objResult = resultItem.GetObjValue(
                            EnumCalcCols.Result);
                        if (objResult != null)
                        {
                            return objResult;
                        }
                    }
                    string strError = string.Empty;
                    if (resultItem != null)
                    {
                        resultItem.TryGetStrValue(
                            EnumCalcCols.Error,
                            out strError);
                    }
                    throw new HCException(
                        "Empty result [" + Environment.StackTrace + "]. Error [" +
                                            strError +
                                            "]. Request[" + request + "]");
                }

                if (tsEvents[0] is string &&
                    ((string)tsEvents[0]).Equals(EnumDistributed.AlreadyDone.ToString()))
                {
                    const string strMessage = "job already done, request again";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);
                    return RunMethodDistributedViaService(
                        strServer,
                        classType,
                        strMethodName,
                        parameters);
                }

                throw new HCException("Empty result");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static object RunMethodDistributed(
            Type classType,
            string strMethodName,
            List<object> parameters,
            string strTopic)
        {
            try
            {
                ASelfDescribingClass calcParams = GetCalcParams(
                    parameters,
                    strMethodName,
                    classType);
                if (NetworkHelper.IsADistWorkerConnected)
                {
                    //
                    // run method locally
                    //
                    string strError;
                    var methodResult = RunMethodLocallyAndLog(calcParams,
                        out strError);
                    if (!string.IsNullOrEmpty(strError))
                    {
                        throw new HCException(strError);
                    }
                    return methodResult;
                }
                DistController distCalcController = DistController.GetController(strTopic);
                ASelfDescribingClass calcResult = distCalcController.DoWork(calcParams);
                var tsEvents = (List<ITsEvent>)calcResult.GetObjValue(EnumCalcCols.TsEvents);
                var result = tsEvents[0] as ASelfDescribingClass;

                string strMessage = String.Format("Recieved message = {0}", calcResult);
                Console.WriteLine(strMessage);
                if (result != null)
                {
                    var finalResult = result.GetObjValue(
                        EnumCalcCols.Result);
                    if (finalResult != null)
                    {
                        return finalResult;
                    }
                    throw new HCException("Null result");
                }
                throw new HCException("Null result");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static SelfDescribingClass GetCalcParams(
            List<object> parameters,
            string strMethodName,
            Type calcType)
        {
            try
            {
                var calcParams = new SelfDescribingClass();
                calcParams.SetClassName(typeof(ExecuteMethodCalc).Name);
                calcParams.SetStrValue(
                    EnumCalcCols.MethodAssemblyName,
                    calcType.Assembly.GetName().Name);
                calcParams.SetStrValue(
                    EnumCalcCols.MethodClassName,
                    calcType.FullName);
                calcParams.SetStrValue(
                    EnumCalcCols.MethodName,
                    strMethodName);
                calcParams.SetStrValue(
                    EnumCalcCols.AssemblyFileName,
                    calcType.Assembly.Location);
                calcParams.SetObjValueToDict(
                    EnumCalcCols.ParamsList,
                    parameters);

                calcParams.SetStrValue(
                    EnumCalcCols.Dns,
                    Dns.GetHostName());

                Interlocked.Increment(ref m_intCalcCounter);
                calcParams.SetStrValue(
                    EnumCalcCols.JobId,
                    m_intCalcCounter + "_" + HCConfig.ClientUniqueName + "_" + Guid.NewGuid().ToString());
                calcParams.SetStrValue(
                    EnumCalcCols.ClassName,
                    typeof(ExecuteMethodCalc).FullName);
                calcParams.SetStrValue(
                    EnumCalcCols.AssemblyName,
                    typeof(ExecuteMethodCalc).Assembly.GetName().Name);
                return calcParams;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public string GetResourceName()
        {
            return string.Empty;
        }

        #endregion

        #region Private

        private static MethodInfo GetMethodInfo(
            Type classType,
            string strMethodName,
            List<object> paramsList)
        {
            try
            {
                MethodInfo[] methods = classType.GetMethods(BindingFlags.Static | BindingFlags.Public);
                var matchedMethods = (from n in methods
                                      where n.Name.Equals(strMethodName)
                                      select n).ToList();
                if (!matchedMethods.Any())
                {
                    throw new HCException("Method not found: " +
                        classType.Name + "." +
                                                 strMethodName + "(.);");
                }

                MethodInfo methodInfo = null;
                if (matchedMethods.Count > 0)
                {
                    for (int i = 0; i < matchedMethods.Count; i++)
                    {
                        ParameterInfo[] methodParams = matchedMethods[i].GetParameters();
                        if (methodParams.Length == paramsList.Count)
                        {
                            bool blnIsValid = true;
                            for (int j = 0; j < methodParams.Length; j++)
                            {
                                if (paramsList[j] != null &&
                                    methodParams[j].ParameterType != paramsList[j].GetType())
                                {
                                    if (!paramsList[j].GetType().IsInstanceOfType(paramsList[j]))
                                    {
                                        blnIsValid = false;
                                        break;
                                    }
                                }
                            }
                            if (blnIsValid)
                            {
                                methodInfo = matchedMethods[i];
                                break;
                            }
                        }
                    }
                }

                if (methodInfo == null)
                {
                    throw new HCException("Method not found: " + classType.Name + "." +
                        strMethodName + "(.)");
                }
                return methodInfo;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        #endregion

        public void Dispose()
        {
            Resource = null;
            if (TsEvents != null)
            {
                TsEvents.Clear();
                TsEvents = null;
            }
            if (Cache != null)
            {
                Cache.Clear();
                Cache = null;
            }
            if (Params != null)
            {
                Params.Dispose();
                Params = null;
            }
        }
    }
}