#region

using System.Collections;

#endregion

namespace HC.Core.Cache.DataStructure
{
    public class ValueEnumeratorCache :
        AbstractEnumeratorCache, IEnumerator
    {
        #region Constructors

        public ValueEnumeratorCache(
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



