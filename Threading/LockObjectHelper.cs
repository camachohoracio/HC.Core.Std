#region 

using System;
using System.Collections;
using System.Collections.Generic;
using HC.Core.Threading.Buffer;

#endregion

namespace HC.Core.Threading
{
    public class LockObjectHelper
        : IEquatable<LockObjectHelper>,
        IComparable<LockObjectHelper>,
        IComparer<LockObjectHelper>,
        IComparer,
        IEqualityComparer<LockObjectHelper>
    {
        #region Members

        private static readonly EfficientMemoryBuffer<string, object> m_lockObjects =
            new EfficientMemoryBuffer<string, object>(10000);

        #endregion

        #region Properties

        public string Name { get; set; }
        public object Handle { get; set; }

        #endregion

        #region Public

        public bool Equals(LockObjectHelper other)
        {
            return Name.Equals(other.Name);
        }

        public int CompareTo(LockObjectHelper other)
        {
            return Name.CompareTo(other.Name);
        }

        public int Compare(object x, object y)
        {
            return Compare(
                x as LockObjectHelper,
                y as LockObjectHelper);
        }

        public bool Equals(LockObjectHelper x, 
            LockObjectHelper y)
        {
            return x.Name.Equals(y.Name);
        }

        public int GetHashCode(LockObjectHelper obj)
        {
            return obj.Name.GetHashCode();
        }

        public int Compare(LockObjectHelper x, LockObjectHelper y)
        {
            return x.Name.CompareTo(y.Name);
        }

        public override string ToString()
        {
            return Name;
        }

        public static object GetLockObject(
            string strKeyLock)
        {
            object currentLockObjectHelper;
            if (!m_lockObjects.TryGetValue(
                strKeyLock,
                out currentLockObjectHelper))
            {
                lock (m_lockObjects)
                {
                    if (!m_lockObjects.TryGetValue(
                        strKeyLock,
                        out currentLockObjectHelper))
                    {
                        currentLockObjectHelper = new object();
                        m_lockObjects[
                            strKeyLock] = currentLockObjectHelper;
                    }
                    return currentLockObjectHelper;
                }
            }
            return currentLockObjectHelper;
        }

        #endregion
    }
}