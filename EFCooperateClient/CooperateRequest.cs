using System;
using System.Collections.Generic;
using System.Text;

namespace EFCooperateClient
{
    public class CooperateRequest
    {
        public ISet<string> Ids { get; }

        public TimeSpan AliveTimeSpan { get; }

        public int MaxGetCount { get; }

        public CooperateRequest(ISet<string> ids, TimeSpan aliveTimeSpan, int maxGetCount)
        {
            Ids = ids;
            AliveTimeSpan = aliveTimeSpan;
            MaxGetCount = maxGetCount;
        }
    }
}
