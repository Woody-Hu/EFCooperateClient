using System;
using System.Collections.Generic;
using System.Text;

namespace EFCooperateClient
{
    public class CooperateRequest
    {
        public ISet<string> Ids { get; }

        public TimeSpan AliveTimeSpan { get; }

        public TimeSpan QuestionableTimeSpan { get; }

        public int MaxGetCount { get; }

        public CooperateRequest(ISet<string> ids, TimeSpan aliveTimeSpan, TimeSpan questionableTimeSpan, int maxGetCount)
        {
            Ids = ids;
            AliveTimeSpan = aliveTimeSpan;
            MaxGetCount = maxGetCount;
            QuestionableTimeSpan = questionableTimeSpan;
        }
    }
}
