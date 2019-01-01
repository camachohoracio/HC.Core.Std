#region

using System;

#endregion

namespace HC.Core.Io.Serialization.Readers
{
    public interface ISerializerReader
    {
        #region Properties

        int BytesRemaining { get; }
        int Position { get; }

        #endregion

        #region Properties

        Single[] ReadSingleArray();
        string[] ReadStringArray();
        DateTime[] ReadDateTimeArray();
        short[] ReadInt16Array();
        long[] ReadInt64Array();
        byte[] ReadByteArray();
        int[] ReadInt32Array();
        bool[] ReadBooleanArray();
        double[] ReadDblArray();
        Type[] ReadTypeArray();

        #endregion

        byte ReadByte();
        short ReadInt16();
        int ReadInt32();
        long ReadInt64();
        float ReadSingle();
        double ReadDouble();
        bool ReadBoolean();
        DateTime ReadDateTime();
        Type ReadType();
        string ReadString();
        object ReadObject();
        TimeSpan ReadTimeSpan();
    }
}


