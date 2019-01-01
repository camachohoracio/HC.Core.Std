#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HC.Core.Exceptions;
using HC.Core.Io.Serialization;
using NUnit.Framework;

#endregion

namespace HC.Core.Zip
{
    public static class ZipTests
    {
        [Test]
        public static void DoTest()
        {
            var dblList = new List<double>();
            var rng = new Random();
            const int intListLenght = 100000;
            for (int i = 0; i < intListLenght; i++)
            {
                dblList.Add(rng.NextDouble());
            }

            MemoryStream memoryStream = MemoryZipper.ZipInMemory(dblList);
            var memoryStream2 = new MemoryStream(memoryStream.GetBuffer());

            const string strFileName = @"c:\serializeTest";
            Serializer.Serialize(strFileName, memoryStream);
            var memoryStream3 = Serializer.DeserializeFile<MemoryStream>(strFileName);

            var unzippedDblList = (List<double>) MemoryZipper.UnZipMemory(memoryStream2);
            var unzippedDblList2 = (List<double>)MemoryZipper.UnZipMemory(memoryStream3);

            if(unzippedDblList.Count != intListLenght)
            {
                throw new HCException();
            }
            Debugger.Break();
        }
    }
}


