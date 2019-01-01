#region

using System;
using HC.Core.Logging;
using HC.Core.Threading;
using HC.Core.Threading.Buffer;

#endregion

namespace HC.Core.Reflection
{
    public static class ReflectorCache
    {
        #region Members

        private static readonly EfficientMemoryBuffer<Type, IReflector> m_expressionCache;

        #endregion

        #region Constructors

        static ReflectorCache()
        {
            m_expressionCache = new EfficientMemoryBuffer<Type, IReflector>(5000);
        }

        #endregion

        #region Public

        public static IReflector GetReflector(Type type)
        {
            IReflector reflector;
            if (!m_expressionCache.TryGetValue(type, out reflector))
            {
                lock (LockObjectHelper.GetLockObject(type.Name + typeof(ReflectorCache).Name))
                {
                    if (!m_expressionCache.TryGetValue(type, out reflector))
                    {
                        reflector = GenerateReflector(type);
                        string strMessage = "Loaded reflector for type [" +
                                            type.Name + "]";
                        Console.WriteLine(strMessage);
                        Logger.Log(strMessage);
                    }
                }
            }
            return reflector;
        }

        private static IReflector GenerateReflector(Type type)
        {
            var genericListType = typeof (Reflector<>);
            var specificBinderType = genericListType.MakeGenericType(type);
            var expressionBinder = (IReflector) Activator.CreateInstance(specificBinderType);
            m_expressionCache[type] = expressionBinder;
            return expressionBinder;
        }

        public static void RemoveExpressionBinder(Type type)
        {
            m_expressionCache.Remove(type);
        }

        #endregion
    }
}