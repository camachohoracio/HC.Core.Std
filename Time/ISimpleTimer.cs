using System;

namespace HC.Core.Time
{
    public interface ISimpleTimer : IDisposable
    {
        int TimeSpan { get; }
        string Id { get; }
    }
}


