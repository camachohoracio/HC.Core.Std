using HC.Core.Io.KnownObjects.KnownTypes;

namespace HC.Core.Comunication.RequestResponseBased.Server.RequestHub
{
    [IsAKnownTypeAttr]
    public enum EnumRequestType
    {
        None, // it should never be none
        GuiTask,
        RequestGuiCache,
        DataProvider,
        PingModel,
        Calc,
        PingConnection,
    }
}



