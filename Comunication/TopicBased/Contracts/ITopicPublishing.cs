#region

//using System.ServiceModel;
//using HC.Core.Io.Serialization.FastSerialization.ServiceSerializer;

#endregion

//[ServiceContract]
//[SerializerContractAttr]
namespace HC.Core.Comunication.TopicBased.Contracts
{
    public interface ITopicPublishing
    {
        //[OperationContract(IsOneWay = true)]
        void Publish(TopicMessage topicMessage);

        void Reconnect();
    }
}


