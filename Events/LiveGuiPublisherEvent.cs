#region

using System.Linq;
using HC.Core.Io.KnownObjects.KnownTypes;

#endregion

namespace HC.Core.Events
{
    [IsAKnownTypeAttr]
    public enum RequestType
    {
        PublishGrid,
        RemoveGridTab,
        RemoveChartTab,
        RemoveGrid,
        RemoveChart,
        PublishChart,
        Log,
        RemoveLabels,
    }

    public delegate void PublishLogDel(
        string strContext,
        string strFormName,
        string strGridName,
        string strObjKey,
        string strLog);

    public delegate void PublishGridDel(
        string strContext,
        string strFormName,
        string strGridName,
        string strObjKey,
        object obj,
        int intTimeSeconds,
        bool blnTransposeObject,
        RequestType requestType);

    public delegate void RemoveTabDel(
        string strContext,
        string strFormName,
        string strTabName);

    public delegate void RemoveFormDel(
        string strContext,
        string strFormName);

    public static class LiveGuiPublisherEvent
    {
        #region Events

        public static event PublishGridDel OnPublishGrid;
        public static event PublishLogDel OnPublishLog;
        public static event RemoveTabDel OnRemoveTab;
        public static event RemoveFormDel OnRemoveForm;

        #endregion

        #region Public

        public static void PublishGrid(
            string strContext,
            string strFormName,
            string strGridName,
            string strObjKey,
            object obj)
        {
            PublishGrid(
                strContext,
                strFormName,
                strGridName,
                strObjKey,
                obj,
                0);
        }

        public static void PublishGrid(
            string strContext,
            string strFormName,
            string strGridName,
            string strObjKey,
            object obj,
            int intTimeSeconds)
        {
            PublishGrid(
                strContext,
                strFormName,
                strGridName,
                strObjKey,
                obj,
                intTimeSeconds,
                false);
        }

        public static void PublishGrid(
            string strContext,
            string strFormName,
            string strGridName,
            string strObjKey,
            object obj,
            int intTimeSeconds,
            bool blnTransposeObject)
        {
            PublishGrid(
                strContext,
                strFormName,
                strGridName,
                strObjKey,
                obj,
                intTimeSeconds,
                blnTransposeObject,
                RequestType.PublishGrid);
        }

        public static void PublishGrid(
            string strContext,
            string strFormName,
            string strGridName,
            string strObjKey,
            object obj,
            int intTimeSeconds,
            bool blnTransposeObject,
            RequestType requestType)
        {
            if(OnPublishGrid != null &&
                OnPublishGrid.GetInvocationList().Any())
            {
                OnPublishGrid(
                    strContext,
                    strFormName,
                    strGridName,
                    strObjKey,
                    obj,
                    intTimeSeconds,
                    blnTransposeObject,
                    requestType);
            }
        }

        public static void PublishLog(
            string strContext,
            string strFormName,
            string strGridName,
            string strObjKey,
            string strLog)
        {
            if(OnPublishLog != null)
            {
                OnPublishLog(
                    strContext,
                    strFormName,
                    strGridName,
                    strObjKey,
                    strLog);
            }
        }

        public static void RemoveForm(string strContext, string strFormName)
        {
            if (OnRemoveForm != null &&
                OnRemoveForm.GetInvocationList().Any())
            {
                OnRemoveForm(strContext, strFormName);
            }
        }

        public static void RemoveTab(string strContext, string strFormName,
            string strTabName)
        {
            if (OnRemoveTab != null &&
                OnRemoveTab.GetInvocationList().Any())
            {
                OnRemoveTab(strContext, strFormName, strTabName);
            }
        }

        #endregion
    }
}



