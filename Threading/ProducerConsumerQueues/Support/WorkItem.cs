#region

using System.Threading;
using System.Threading.Tasks;

#endregion

namespace HC.Core.Threading.ProducerConsumerQueues.Support
{
    public class WorkItem<T>
    {
        #region Properties

        public TaskCompletionSource<object> TaskSource { get; private set; }
        public CancellationToken? CancelToken { get; private set; }
        public T State { get; private set; }

        public TaskWrapper TaskWrapper { get; set; }

        #endregion

        #region Constructors

        public WorkItem(
            TaskCompletionSource<object> taskSource,
            CancellationToken? cancelToken,
            T state,
            TaskWrapper taskWrapper)
        {
            TaskWrapper = taskWrapper;
            TaskSource = taskSource;
            CancelToken = cancelToken;
            State = state;
        }

        #endregion
    }
}



