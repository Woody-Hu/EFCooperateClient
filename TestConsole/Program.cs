using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EFCooperateClient;
using Microsoft.EntityFrameworkCore;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            DbContextOptionsBuilder<CooperateContext> optionsBuilder = new DbContextOptionsBuilder<CooperateContext>();
            //optionsBuilder.UseNpgsql("Host=localhost;Database=Cooperate;Username=postgres;Password=123456;Persist Security Info=True");
            if (File.Exists("test.db"))
            {
                File.Delete("test.db");
            }

            optionsBuilder.UseSqlite("Data Source=test.db");

            CooperateRequest request = new CooperateRequest(new HashSet<string>() { "job1", "job2", "job3", "job4", "job5", "job6", "job7", "job8" }, TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(100), 2);
            var mangerDic = new ConcurrentDictionary<string, CooperateManger>();
            var tokenSourcesDic = new ConcurrentDictionary<string, CancellationTokenSource>();
            Parallel.For(1, 5, (k) =>
            {
                CancellationTokenSource source = new CancellationTokenSource();
                tokenSourcesDic.GetOrAdd(k.ToString(), source);
                mangerDic.GetOrAdd(k.ToString(), new CooperateManger(request, optionsBuilder.Options, source.Token));
            });

            var useIndex = 0;
            while (!mangerDic.Values.All(k=>k.Initialed))
            {
                if (useIndex % 100 == 0)
                {
                    Console.WriteLine("wait initial");
                }

                useIndex++;
            }

            useIndex = 0;
            for (int i = 1; i < 5; i++)
            {
                var manger = mangerDic[i.ToString()];
                var ids = manger.GetNowAvailableIds();
                var sb = new StringBuilder();
                sb.Append($"{i} : count {ids.Count} :");
                foreach (var oneId in ids)
                {
                    sb.Append($"{oneId},");
                }

                if (useIndex == 0 && ids.Count > 0)
                {
                    useIndex = i;
                }

                Console.WriteLine(sb.ToString());
            }

            //mock stop one machine
            Console.WriteLine($"cancel {useIndex}");
            tokenSourcesDic[useIndex.ToString()].Cancel();
            Thread.Sleep(2000);
            Console.WriteLine("wait 2000");

            for (int i = 1; i < 5; i++)
            {
                var manger = mangerDic[i.ToString()];
                var ids = manger.GetNowAvailableIds();
                var sb = new StringBuilder();
                sb.Append($"{i} : count {ids.Count} :");
                foreach (var oneId in ids)
                {
                    sb.Append($"{oneId},");
                }

                if (useIndex == 0 && ids.Count > 0)
                {
                    useIndex = i;
                }

                Console.WriteLine(sb.ToString());
            }

            Console.ReadKey();
            Console.WriteLine("Hello World!");
        }
    }
}
