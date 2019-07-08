using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EFCooperateClient
{
    public sealed class CooperateManger : IDisposable
    {
        private readonly Task _backgroundUpdateTask;

        private readonly CancellationToken _cancellationToken;

        private readonly Dictionary<string, CooperateItem> _cooperateItems;

        private readonly Dictionary<string, CooperateEntity> _cooperateEntities;

        private readonly CooperateRequest _cooperateRequest;

        private readonly ConcurrentBag<Exception> _exceptionsInBackground = new ConcurrentBag<Exception>();

        private readonly DbContextOptions<CooperateContext> _options;

        private bool _initialed = false;

        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public bool Initialed
        {
            get
            {
                try
                {
                    _lockSlim.EnterReadLock();
                    return _initialed;
                }
                finally
                {
                    _lockSlim.ExitReadLock();
                }
            }
            private set
            {
                try
                {
                    _lockSlim.EnterWriteLock();
                    _initialed = value;
                }
                finally
                {
                    _lockSlim.ExitWriteLock();
                }
            }
        }


        public CooperateManger(CooperateRequest cooperateRequest, DbContextOptions<CooperateContext> options,
            CancellationToken cancellationToken)
        {
            if (cooperateRequest == null || options == null || cancellationToken == null)
            {
                throw new ArgumentException();
            }

            _cancellationToken = cancellationToken;
            _cooperateRequest = cooperateRequest;
            _options = options;
            _cooperateItems = new Dictionary<string, CooperateItem>();
            _cooperateEntities = new Dictionary<string, CooperateEntity>();

            foreach (var oneId in _cooperateRequest.Ids)
            {
                _cooperateItems.Add(oneId,
                    new CooperateItem(oneId, cooperateRequest.QuestionableTimeSpan,
                        new KeyValuePair<bool, DateTime>(false, DateTime.MinValue)));
            }

            _backgroundUpdateTask = Task.Run(async () =>
            {
                await InitCooperateRequestAsync(_cancellationToken);
                Initialed = true;
                await UpdateCooperateAsync(_cancellationToken);
            });
        }

        public ImmutableDictionary<string, CooperateItem> GetCooperateItems()
        {
            return _cooperateItems.ToImmutableDictionary();
        }

        public CooperateItem GetCooperateItem(string id)
        {
            return _cooperateItems.GetValueOrDefault(id);
        }

        public ImmutableList<string> GetNowAvailableIds()
        {
            var ids = _cooperateItems.Where(k => k.Value.State == CooperateState.Get).Select(k => k.Key);
            return ids.ToImmutableList();
        }

        public bool Canceled()
        {
            return _cancellationToken.IsCancellationRequested;
        }

        private async Task InitCooperateRequestAsync(CancellationToken cancellationToken)
        {
            try
            {
                var ids = _cooperateRequest.Ids;
                var random = new Random();
                var orderedIds = ids.OrderBy(k => random.Next(ids.Count));
                try
                {
                    using (var cooperateContext = new CooperateContext(_options))
                    {
                        await cooperateContext.Database.EnsureCreatedAsync(cancellationToken);
                    }

                }
                catch
                {
                    // ignored
                }

                foreach (var oneId in orderedIds)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    await GetOrAddOneCooperateItem(cancellationToken, oneId);
                    await Task.Delay(random.Next(20), cancellationToken);
                }
            }
            catch (Exception e)
            {
                _exceptionsInBackground.Add(e);
            }
        }

        private async Task UpdateCooperateAsync(CancellationToken cancellationToken)
        {
            SpinWait waiter = new SpinWait();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var questionableIds = _cooperateItems.Where(k => k.Value.State == CooperateState.Questionable)
                        .Select(k => k.Key);

                    foreach (var oneId in questionableIds)
                    {
                        await UpdateCooperateEntity(cancellationToken, oneId, now);
                    }

                    var expireIds = _cooperateItems.Where(k => now > k.Value.ExpireUtcDateTime).Select(k => k.Key);
                    foreach (var oneId in expireIds)
                    {
                        await UpdateCooperateEntity(cancellationToken, oneId, now);
                    }
                }
                catch (Exception e)
                {
                    _exceptionsInBackground.Add(e);
                }

                waiter.SpinOnce();
            }
        }

        private async Task UpdateCooperateEntity(CancellationToken cancellationToken, string oneId, DateTime now)
        {
            using (var cooperateContext = new CooperateContext(_options))
            {
                try
                {
                    var entity = _cooperateEntities[oneId];
                    var backEntity = new CooperateEntity
                    {
                        Id = entity.Id,
                        LastModifyDateTime = entity.LastModifyDateTime,
                        ExpireDateTime = entity.ExpireDateTime
                    };

                    var nowEntity = await cooperateContext.CooperateEntities.FindAsync(oneId);
                    if (nowEntity.LastModifyDateTime > backEntity.LastModifyDateTime)
                    {
                        _cooperateEntities[oneId] = nowEntity;
                        var item = _cooperateItems[oneId];
                        item.ExpireKeyValuePair = new KeyValuePair<bool, DateTime>(false, nowEntity.ExpireDateTime);
                        return;
                    }
                    else
                    {
                        nowEntity.LastModifyDateTime = now;
                        nowEntity.ExpireDateTime = now + _cooperateRequest.AliveTimeSpan;

                        cooperateContext.Update(nowEntity);
                        await cooperateContext.SaveChangesAsync(cancellationToken);
                        _cooperateEntities[oneId] = nowEntity;
                        var item = _cooperateItems[oneId];
                        item.ExpireKeyValuePair = new KeyValuePair<bool, DateTime>(true, nowEntity.ExpireDateTime);
                    }

                }
                catch (DbUpdateConcurrencyException)
                {
                    var entity = await cooperateContext.CooperateEntities.FindAsync(oneId);
                    _cooperateEntities[oneId] = entity;
                    var item = _cooperateItems[oneId];
                    item.ExpireKeyValuePair = new KeyValuePair<bool, DateTime>(false, entity.ExpireDateTime);
                }
            }
        }

        private async Task GetOrAddOneCooperateItem(CancellationToken cancellationToken, string oneId)
        {
            using (var cooperateContext = new CooperateContext(_options))
            {
                try
                {
                    var findRes = await cooperateContext.CooperateEntities.FindAsync(oneId);
                    if (findRes != null)
                    {
                        var item = _cooperateItems[oneId];
                        item.ExpireKeyValuePair = new KeyValuePair<bool, DateTime>(false, findRes.ExpireDateTime);
                        _cooperateEntities[oneId] = findRes;
                    }
                    else
                    {
                        var now = DateTime.UtcNow;
                        var entity = new CooperateEntity
                        {
                            Id = oneId,
                            LastModifyDateTime = now,
                            ExpireDateTime = now + _cooperateRequest.AliveTimeSpan
                        };
                        var res = await cooperateContext.CooperateEntities.AddAsync(entity, cancellationToken);
                        await cooperateContext.SaveChangesAsync(cancellationToken);

                        _cooperateEntities[oneId] = entity;
                        var item = _cooperateItems[oneId];
                        item.ExpireKeyValuePair = new KeyValuePair<bool, DateTime>(true, entity.ExpireDateTime);
                    }
                }
                catch (DbUpdateException)
                {
                    var findRes = await cooperateContext.CooperateEntities.FindAsync(oneId);
                    if (findRes != null)
                    {
                        var item = _cooperateItems[oneId];
                        item.ExpireKeyValuePair = new KeyValuePair<bool, DateTime>(false, findRes.ExpireDateTime);
                        _cooperateEntities[oneId] = findRes;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public void Dispose()
        {
            _backgroundUpdateTask?.Dispose();
        }
        
    }
}

