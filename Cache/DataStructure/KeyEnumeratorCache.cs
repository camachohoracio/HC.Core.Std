#region

using System.Collections;

#endregion

namespace HC.Core.Cache.DataStructure
{
    public class KeyEnumeratorCache :
        AbstractEnumeratorCache, IEnumerator
    {
        #region Constructors

        public KeyEnumeratorCache(
            ICache cache)
            : base(cache)
        {
        }

        #endregion

        public object Current
        {
            get { return null; }
        }

        #region IEnumerator Members

        object IEnumerator.Current
        {
            get { return null; }
        }

        #endregion
    }
}



