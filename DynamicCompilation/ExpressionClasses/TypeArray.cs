#region

using System;

#endregion

namespace HC.Core.DynamicCompilation.ExpressionClasses
{
    public static class TypeArray
    {
        /// <summary>
        ///   Creates an array of types
        /// </summary>
        /// <typeparam name = "T1">The first type</typeparam>
        /// <returns></returns>
        public static Type[] Make<T1>()
        {
            return new[] {typeof (T1)};
        }

        /// <summary>
        ///   Creates array of types
        /// </summary>
        /// <typeparam name = "T1"></typeparam>
        /// <typeparam name = "T2"></typeparam>
        /// <returns></returns>
        public static Type[] Make<T1, T2>()
        {
            return new[] {typeof (T1), typeof (T2)};
        }

        /// <summary>
        ///   Creates array of types
        /// </summary>
        /// <typeparam name = "T1"></typeparam>
        /// <typeparam name = "T2"></typeparam>
        /// <returns></returns>
        public static Type[] Make<T1, T2, T3>()
        {
            return new[] {typeof (T1), typeof (T2), typeof (T3)};
        }

        /// <summary>
        ///   Creates array of types
        /// </summary>
        /// <typeparam name = "T1"></typeparam>
        /// <typeparam name = "T2"></typeparam>
        /// <returns></returns>
        public static Type[] Make<T1, T2, T3, T4>()
        {
            return new[]
                       {
                           typeof (T1), typeof (T2),
                           typeof (T3),
                           typeof (T4)
                       };
        }
    }
}


