#region

using System;
using System.Linq.Expressions;

#endregion

namespace HC.Core.DynamicCompilation.ExpressionClasses
{
    public static class ExpressionExtensions
    {
        /// <summary>
        ///   Converts an expression to the specified type
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "expression"></param>
        /// <returns></returns>
        public static Expression ConvertTo<T>(this Expression expression)
        {
            return expression.Type == typeof (T)
                       ? expression
                       : Expression.Convert(expression, typeof (T));
        }

        /// <summary>
        ///   Converts an expression to the specified type
        /// </summary>
        /// <param name = "expression"></param>
        /// <param name = "type"></param>
        /// <returns></returns>
        public static Expression ConvertTo(this Expression expression,
                                           Type type)
        {
            return expression.Type == type
                       ? expression
                       : Expression.Convert(expression, type);
        }
    }
}


