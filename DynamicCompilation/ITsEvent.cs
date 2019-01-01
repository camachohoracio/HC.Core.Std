#region

using System;
using HC.Core.Io.KnownObjects.KnownTypes;
using HC.Core.Io.Serialization.Interfaces;

#endregion

namespace HC.Core.DynamicCompilation
{
    [IsAKnownTypeAttr]
    public interface ITsEvent : ISerializable, IDisposable
    {
        DateTime Time { get; set; }

        //[XmlIgnore]
        //[Browsable(false)]
        //TsDataRequest TsDataRequest { get; set; }

        string ToCsvString();
        object GetHardPropertyValue(string strFieldName);
    }
}




