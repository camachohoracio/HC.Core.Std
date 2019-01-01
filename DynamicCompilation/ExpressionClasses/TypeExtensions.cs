#region

using System;

#endregion

namespace HC.Core.DynamicCompilation.ExpressionClasses
{
    public static class TypeExtensions
    {
        /// <summary>
        ///   Determines if a type represents something that can be
        ///   set to null
        /// </summary>
        /// <param name = "type"></param>
        /// <returns></returns>
        public static bool IsNullable(this Type type)
        {
            if (type.IsValueType == false)
            {
                return true;
            }
            if (Nullable.GetUnderlyingType(type) != null)
            {
                return true;
            }
            return false;
        }
    }
}


