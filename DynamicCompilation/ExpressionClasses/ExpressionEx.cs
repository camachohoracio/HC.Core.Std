#region

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

#endregion

namespace HC.Core.DynamicCompilation.ExpressionClasses
{
    public static class ExpressionEx
    {
        private static readonly MethodInfo StringConcatMethod =
            typeof (string).GetMethod("Concat",
                                      TypeArray.Make<string, string>());

        public static Expression Throw<T>() where T : Exception
        {
            var toThrow = Expression.Throw(Expression.New(
                typeof (T)));
            return toThrow;
        }

        public static Expression Throw<T>(string message) where T : Exception
        {
            var ctor = typeof (T).GetConstructor(
                TypeArray.Make<string>());
            var toThrow = Expression.Throw(
                Expression.New(ctor, Expression.Constant(message)));
            return toThrow;
        }

        /// <summary>
        ///   Looks for a property or field with the specified name
        /// </summary>
        /// <param name = "type"></param>
        /// <param name = "name"></param>
        /// <param name = "flags"></param>
        /// <returns>An expression that provides access to the property or field</returns>
        public static MemberExpression PropertyOrField(
            Type type,
            string name,
            BindingFlags flags)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (name == null) throw new ArgumentNullException("name");

            MemberExpression x;
            var fieldInfo = type.GetField(name, flags);

            if (fieldInfo != null)
            {
                x = Expression.Field(null, fieldInfo);
            }
            else
            {
                var propertyInfo = type.GetProperty(name, flags);
                if (propertyInfo == null)
                    throw new ArgumentException(
                        @"Not a property or field: " +
                        name);
                x = Expression.Property(null, propertyInfo);
            }
            return x;
        }

        /// <summary>
        ///   Looks for a property or field with the specified name
        /// </summary>
        /// <param name = "type"></param>
        /// <param name = "name"></param>
        /// <param name = "flags"></param>
        /// <param name = "expression"></param>
        /// <returns></returns>
        public static bool PropertyOrField(
            Type type,
            string name,
            BindingFlags flags,
            out MemberExpression expression)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (name == null) throw new ArgumentNullException("name");

            var found = false;
            expression = null;

            var fieldInfo = type.GetField(name, flags);

            if (fieldInfo != null)
            {
                expression = Expression.Field(null, fieldInfo);
                found = true;
            }
            else
            {
                var propertyInfo = type.GetProperty(name, flags);
                if (propertyInfo != null)
                {
                    expression = Expression.Property(null, propertyInfo);
                    found = true;
                }
            }
            return found;
        }

        /// <summary>
        ///   Looks for a property or field with the specified name
        /// </summary>
        /// <param name = "type"></param>
        /// <param name = "name"></param>
        /// <param name = "flags"></param>
        /// <param name = "expression"></param>
        /// <returns></returns>
        public static MemberExpression PropertyOrField(
            Expression instance,
            string name,
            BindingFlags flags)
        {
            if (instance == null) throw new ArgumentNullException("Instance");
            if (name == null) throw new ArgumentNullException("name");

            var type = instance.Type;
            MemberExpression x;
            var fieldInfo = type.GetField(name, flags);

            if (fieldInfo != null)
            {
                x = Expression.Field(null, fieldInfo);
            }
            else
            {
                var propertyInfo = type.GetProperty(name, flags);
                if (propertyInfo == null)
                    throw new ArgumentException(
                        "Not a property or field: " + name);
                x = Expression.Property(null, propertyInfo);
            }
            return x;
        }

        public static bool TryPropertyOrField(
            Expression instance,
            string name,
            BindingFlags flags,
            out MemberExpression expression)
        {
            if (instance == null) throw new ArgumentNullException("instance");
            if (name == null) throw new ArgumentNullException("name");

            var type = instance.Type;

            var found = false;
            expression = null;

            var fieldInfo = type.GetField(name, flags);

            if (fieldInfo != null)
            {
                expression = Expression.Field(null, fieldInfo);
                found = true;
            }
            else
            {
                var propertyInfo = type.GetProperty(name, flags);
                if (propertyInfo != null)
                {
                    expression = Expression.Property(null, propertyInfo);
                    found = true;
                }
            }
            return found;
        }

        /// <summary>
        ///   Concatenates two strings together
        /// </summary>
        /// <param name = "lhs"></param>
        /// <param name = "rhs"></param>
        /// <returns></returns>
        public static Expression StringConcat(
            Expression lhs,
            Expression rhs)
        {
            if (lhs == null) throw new ArgumentException("lhs");
            if (rhs == null) throw new ArgumentException("rhs");
            return Expression.Call(StringConcatMethod, lhs, rhs);
        }

        /// <summary>
        ///   Creates a parameter
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <returns></returns>
        public static ParameterExpression Parameter<T>()
        {
            return Parameter<T>(null);
        }

        /// <summary>
        ///   Creates a parameter
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <returns></returns>
        public static ParameterExpression Parameter<T>(string name)
        {
            return Expression.Parameter(typeof (T), name);
        }

        /// <summary>
        ///   Generates a list of constants
        /// </summary>
        /// <param name = "values"></param>
        /// <returns></returns>
        public static List<ConstantExpression> Constants(params object[] values)
        {
            var constants = new List<ConstantExpression>(values.Length);

            foreach (object value in values)
            {
                constants.Add(Expression.Constant(value));
            }
            return constants;
        }

        public static List<Expression> Values(params object[] values)
        {
            var v = new List<Expression>(values.Length);

            foreach (object value in values)
            {
                var e = value as Expression;
                if (e != null)
                {
                    v.Add(e);
                }
                else
                {
                    v.Add(Expression.Constant(value));
                }
            }
            return v;
        }
    }
}


