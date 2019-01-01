#region

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using HC.Core.ConfigClasses;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Logging;
using HC.Core.Reflection;

#endregion

namespace HC.Core.Comunication.RequestResponseBased.Server.RequestHub
{
    public class RequestDataMessage : ASerializable, IDisposable
    {
        #region Members

        private static IReflector m_binder;
        private List<RequestDataMessage> m_response;
        private bool m_isClientDisconnected;
        private TaskCompletionSource<object> m_tcs;

        #endregion

        #region Properties

        public string Id { get; set; }
        public object Request { get; set; }
        public string RequestorName { get; set; }
        public EnumRequestType RequestType { get; set; }
        public int CallbackSize { get; set; }
        public List<object> Response { get; set; }
        public bool IsAsync { get; set; }

        #endregion

        #region Constructors

        public RequestDataMessage()
        {
            RequestorName = HCConfig.ClientUniqueName;
        }

        #endregion

        public void SetResponse(List<RequestDataMessage> response)
        {
            m_response = response;
        }

        public List<RequestDataMessage> GetResponse()
        {
            return m_response;
        }

        public void SetIsClientDisconnected(bool blnValue)
        {
            m_isClientDisconnected = blnValue;
        }

        public bool GetIsClientDisconnected()
        {
            return m_isClientDisconnected;
        }

        public override string ToString()
        {
            try
            {
                LoadBinder();
                var sb = new StringBuilder();
                bool blnIsTitle = true;
                foreach (string strPropertyName in m_binder.GetPropertyNames())
                {
                    if (!blnIsTitle)
                    {
                        sb.Append(",\n");
                    }
                    else
                    {
                        blnIsTitle = false;
                    }
                    sb.Append(strPropertyName + " = " +
                              m_binder.GetPropertyValue(this, strPropertyName));
                }
                return sb.ToString();
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return string.Empty;
        }

        public void Dispose()
        {
            EventHandlerHelper.RemoveAllEventHandlers(this);
            if (m_response != null &&
                m_response.Count > 0)
            {
                for (int i = 0; i < m_response.Count; i++)
                {
                    m_response[i].Dispose();
                }
            }
            if (Response != null &&
                Response.Count > 0)
            {
                Response.Clear();
            }
        }

        private static void LoadBinder()
        {
            if (m_binder == null)
            {
                m_binder = ReflectorCache.GetReflector(typeof (RequestDataMessage));
            }
        }


        public string Error { get; set; }

        public TaskCompletionSource<object> GetTcs()
        {
            return m_tcs;
        }

        public void SetTcs(TaskCompletionSource<object> tcs)
        {
            m_tcs = tcs;
        }

    }
}


