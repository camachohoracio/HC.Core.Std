#region

//using System.ServiceModel;

#endregion

//[ServiceContract(CallbackContract = typeof (ITopicPublishing))]
namespace HC.Core.Comunication.TopicBased.Contracts
{
    public interface ISubscription
    {
        //[OperationContract]
        void Subscribe(string strTopicName);

        //[OperationContract]
        void UnSubscribe(string topicName);
    }
}


