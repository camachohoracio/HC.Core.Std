#region

using System;

#endregion

namespace HC.Core.DynamicCompilation.ExpressionClasses
{
    public static class BindingTypes
    {
        public static Type[] Supported =
            new[]
                {
                    typeof (int),
                    typeof (double),
                    typeof (string),
                    typeof (DateTime),
                    typeof (bool),
                    typeof (TimeSpan),
                    typeof (uint),
                };
    }
}


