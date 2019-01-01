#region

using HC.Core.Comunication.RequestResponseBased.Server.RequestHub;
//using HC.Core.Io.Serialization.FastSerialization.ServiceSerializer;

#endregion

namespace HC.Core.Comunication.RequestResponseBased.Server
{
    //[ServiceContract]
    //[SerializerContractAttr]
    public interface IReqRespService
    {
        //[OperationContract]
        RequestDataMessage RequestDataOperation(RequestDataMessage transferMessage);
    }
}


