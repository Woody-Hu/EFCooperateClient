using System;

namespace EFCooperateClient
{
    public class CooperateEntity
    {
        public string Id { get; set; }

        public DateTime LastModifyDateTime { get; set; }

        public DateTime ExpireDateTime { get; set; }
    }
}
