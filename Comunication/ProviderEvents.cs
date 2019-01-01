using System;
using System.Collections.Generic;
using System.Threading;
using HC.Core.Logging;

namespace HC.Core.Comunication
{
    public static class ProviderEvents
    {
        public delegate object RunMethodDistributedViaServiceDel(
            Type classType,
            string strMethodName,
            List<object> parameters);

        public static event RunMethodDistributedViaServiceDel OnRunMethodDistributedViaService;

        public static object InvokeOnRunMethodDistributedViaService(
            Type classType,
            string strMethodName,
            List<object> parameters)
        {
            try
            {
                while (OnRunMethodDistributedViaService == null)
                {
                    Thread.Sleep(2000);
                    Console.WriteLine("Generic distributed provider is not initialized");
                }
                return OnRunMethodDistributedViaService.Invoke(
                    classType,
                    strMethodName,
                    parameters);
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }
    }
}
