using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EFCooperateClient
{
    public sealed class CooperateManger:IDisposable
    {
        private readonly Task _backgroundUpdateTask;

        private readonly CancellationToken _cancellationToken;

        private readonly Dictionary<string, CooperateItem> _cooperateItems;

        private readonly CooperateRequest _cooperateRequest;

        private readonly CooperateContext _cooperateContext;

        public CooperateManger(CooperateRequest cooperateRequest, DbContextOptions<CooperateContext> options)
        {
            if (cooperateRequest == null || options == null)
            {
                throw new ArgumentException();
            }

            _cooperateContext = new CooperateContext(options);
            _cooperateItems = new Dictionary<string, CooperateItem>();
        }

        public ImmutableDictionary<string, CooperateItem> GetCooperateItems()
        {
            throw new NotImplementedException();
        }

        public CooperateItem GetCooperateItem(string Id)
        {
            throw new NotImplementedException();
        }

        public ImmutableList<string> GetNowAvailableIds()
        {
            throw new NotImplementedException();

        }

        public void Dispose()
        {
            _backgroundUpdateTask?.Dispose();
            _cooperateContext?.Dispose();
        }
    }
}
