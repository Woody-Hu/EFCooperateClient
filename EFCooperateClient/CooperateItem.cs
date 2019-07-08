using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace EFCooperateClient
{
    public class CooperateItem
    {
        private readonly TimeSpan _questionableTimeSpan;

        private KeyValuePair<bool, DateTime> _expireKeyValuePair;

        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        internal KeyValuePair<bool, DateTime> ExpireKeyValuePair
        {
            get
            {
                try
                {
                    _lockSlim.EnterReadLock();
                    return _expireKeyValuePair;
                }
                finally
                {
                    _lockSlim.ExitReadLock();
                }
            }
            set
            {
                try
                {
                    _lockSlim.EnterWriteLock();
                    _expireKeyValuePair = value;
                }
                finally
                {
                    _lockSlim.ExitWriteLock();
                }
            }
        }

        public string Id { get; }

        public DateTime ExpireUtcDateTime
        {
            get
            {
                var expireKeyValuePair = ExpireKeyValuePair;
                return expireKeyValuePair.Value;
            }
        }

        public CooperateState State {
            get
            {
                var expireKeyValuePair = ExpireKeyValuePair;
                var now = DateTime.UtcNow;
                if (!expireKeyValuePair.Key || now >= expireKeyValuePair.Value)
                {
                    return CooperateState.NotGet;
                }

                if ((ExpireUtcDateTime - now) < _questionableTimeSpan)
                {
                    return CooperateState.Questionable;
                }

                return CooperateState.Get;
            }
        }

        internal CooperateItem(string id, TimeSpan questionableTimeSpan, KeyValuePair<bool, DateTime> expireKeyValuePair)
        {
            Id = id;
            ExpireKeyValuePair = expireKeyValuePair;
            _questionableTimeSpan = questionableTimeSpan;
        }
    }
}
