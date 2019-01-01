using System;
using System.Collections.Generic;
using HC.Core.Comunication.RequestResponseBased.Server.RequestHub;
using HC.Core.Exceptions;
using HC.Core.Logging;

namespace HC.Core.Comunication.RequestResponseBased
{
    public static class ReqRespHelper
    {
        public static List<List<object>> GroupList(
            List<object> list,
            int intGroupSize)
        {
            if (intGroupSize == 0)
            {
                throw new HCException("Invalid request size");
            }

            if (list == null || list.Count == 0)
            {
                return new List<List<object>>();
            }
            var groups = new List<List<object>>();
            var currList = new List<object>();
            foreach (object o in list)
            {
                currList.Add(o);

                if (currList.Count >= intGroupSize)
                {
                    groups.Add(currList);
                    currList = new List<object>();
                }
            }
            //
            // add last list
            //
            if (currList.Count > 0)
            {
                groups.Add(currList);
            }
            return groups;
        }

        public static List<RequestDataMessage> GetListOfResponses(
            RequestDataMessage response)
        {
            List<List<object>> groups = GroupList(
                response.Response,
                response.CallbackSize);
            var responseList = new List<RequestDataMessage>();
            foreach (List<object> list in groups)
            {
                try
                {
                    responseList.Add(new RequestDataMessage
                        {
                            Request = response.Request,
                            Response = list,
                            CallbackSize =
                                response.CallbackSize,
                            Id = response.Id,
                            RequestType = EnumRequestType.DataProvider,
                        });
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    break;
                }
            }

            return responseList;
        }
    }
}



