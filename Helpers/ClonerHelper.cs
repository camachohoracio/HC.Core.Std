#region

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using HC.Core.Exceptions;

#endregion

namespace HC.Core.Helpers
{
    public class ClonerHelper
    {
        //
        // MAKE A DEEP COPY OF AN OBJECT
        // An exception will be thrown if an attempt to copy a non-serialisable object is made.
        //
        public static T Clone<T>(T obj)
        {
            Object objCopy;
            try
            {
                var type = typeof (T);
                if (type.IsValueType)
                {
                    return obj;
                }
                if (obj == null)
                {
                    return default(T);
                }
                ICloneable cloneable;
                if ((cloneable = (obj as ICloneable)) != null)
                {
                    return (T)cloneable.Clone();
                }
                //
                // clone object
                //
                var bf = new BinaryFormatter();
                var fs = new MemoryStream();
                bf.Serialize(fs, obj);
                fs.Position = 0;
                objCopy = bf.Deserialize(fs);
            }
            catch (IOException e)
            {
                PrintToScreen.WriteLine(e.StackTrace);
                throw;
            }
            catch (HCException cnfe)
            {
                PrintToScreen.WriteLine(cnfe.StackTrace);
                throw cnfe;
            }
            return (T) objCopy;
        }
    }
}


