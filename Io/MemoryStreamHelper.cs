#region

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using HC.Core.Exceptions;

#endregion

namespace HC.Core.Io
{
    // save objects into the memory
    public class MemoryStreamHelper
    {
        public MemoryStream GetMemoryStream(object o)
        {
            var formatter = new BinaryFormatter();
            var ms = new MemoryStream();

            try
            {
                formatter.Serialize(ms, o);
                return ms;
            }
            catch (HCException e)
            {
                throw;
            }
        }
    }
}


