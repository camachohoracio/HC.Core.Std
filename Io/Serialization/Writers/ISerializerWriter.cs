#region

using System;

#endregion

namespace HC.Core.Io.Serialization.Writers
{
    public interface ISerializerWriter
    {
        void Write(object objValue);
        void Write(Enum enumValue);
        void Write(Type type);
        void Write(byte value);
        void Write(short value);
        void Write(int value);
        void Write(long value);
        void Write(double value);
        void Write(float value);
        void Write(bool value);
        void Write(DateTime value);
        void Write(string value);
        void Write(short[] value);
        void Write(float[] value);
        void Write(DateTime[] value);
        void Write(Type[] value);
        void Write(string[] value);
        void Write(long[] value);
        void Write(double[] value);
        void Write(bool[] value);
        void Write(int[] value);
        void Write(byte[] value);
        void Write(TimeSpan value);


        byte[] GetBytes();
        void WriteRaw(object value);
    }
}


