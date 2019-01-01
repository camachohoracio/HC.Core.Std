#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

#endregion

namespace HC.Core.DynamicCompilation.ExpressionClasses
{
    public static class TypeExtractor<T>
    {
        private static readonly List<Type> types = new List<Type>();
        private static readonly List<string> names = new List<string>();

        public static Type[] Types
        {
            get { return types.ToArray(); }
        }

        public static string[] Names
        {
            get { return names.ToArray(); }
        }

        public static Action<T, Action<string, object>> Generate(
            IEnumerable<Type> l)
        {
            return GenerateLambda(l);
        }

        public static Action<T, Action<string, object>> GenerateLambda(
            IEnumerable<Type> l)
        {
            names.Clear();
            types.Clear();
            var item = Expression.Parameter(typeof (T), "item");
            var handler =
                Expression.Parameter(typeof (Action<string, object>),
                                     "handler");

            var expressions = new List<Expression>();

            if (typeof (T).IsNullable())
            {
                var equalsNull = Expression.Equal(item,
                                                  Expression.Constant(null));
                var nullCheck = Expression.IfThen(
                    equalsNull,
                    ExpressionEx.Throw<ArgumentNullException>());
                expressions.Add(nullCheck);
            }

            foreach (PropertyInfo propertyInfo in typeof (T).GetProperties(BindingFlags.Public |
                                                                           BindingFlags.Instance))
            {
                if (propertyInfo.CanRead == false) continue;
                if (propertyInfo.GetGetMethod().GetParameters().Length != 0) continue;
                var t = propertyInfo.PropertyType;
                if (t.IsEnum)
                    t = typeof (string);
                if (l != null && !l.Contains(t)) continue;
                types.Add(t);
                names.Add(propertyInfo.Name);
                var name = Expression.Constant(propertyInfo.Name);

                var ex = Expression.Property(item, propertyInfo).ConvertTo<object>();
                if (propertyInfo.PropertyType.IsEnum)
                {
                    ex = Expression.Call(ex, "ToString", null);
                }
                var callHandler = Expression.Invoke(handler, name, ex);
                expressions.Add(callHandler);
            }

            var body = Expression.Block(expressions);
            var lambda =
                Expression.Lambda<Action<T, Action<string, object>>>(body, item, handler);
            return lambda.Compile();
        }
    }
}


