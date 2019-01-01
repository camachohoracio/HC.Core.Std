#region

using System;
using System.Collections.Generic;
using System.Threading;

#endregion

namespace HC.Core.Distributed.Tests
{
    public static class TestMethodCalc
    {
        public static object TestMethod(List<double> paramsList)
        {
            Console.WriteLine(typeof (TestMethodCalc).Name + " is doing work");
            const int intByteSize = (int) (2*1024f*1024f);
            Thread.Sleep(5000);
            Console.WriteLine(typeof (TestMethodCalc).Name + " is finish with work");
            return new byte[intByteSize];
        }
    }
}