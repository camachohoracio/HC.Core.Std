using System;
using System.Reflection;

namespace HC.Core.Io
{
    public class CasterCache
    {
        public static object DoCast(object obj, Type type)
        {
            MethodInfo method = typeof(CasterCache).GetMethod("Cast");
            MethodInfo generic = method.MakeGenericMethod(type);
            return generic.Invoke(null, new[] {obj});
        }

        public static T Cast<T>(object obj)
        {
            return (T)obj;
        }
    }
}



