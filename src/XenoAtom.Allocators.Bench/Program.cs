using System.Diagnostics;
using BenchmarkDotNet.Running;

namespace XenoAtom.Allocators.Bench;

internal class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<BenchAllocator>(null, args);

        //var bench = new BenchAllocator();
        //bench.Setup();

        //var clock = Stopwatch.StartNew();
        //for (int i = 0; i < 1_000_000; i++)
        //{
        //    bench.Tlsf();
        //}
        //clock.Stop();
        //Console.WriteLine($"Tlsf: {clock.ElapsedMilliseconds}");
        //clock.Restart();

        //for (int i = 0; i < 1_000_000; i++)
        //{
        //    bench.Libc();
        //}
        //clock.Stop();
        //Console.WriteLine($"Libc: {clock.ElapsedMilliseconds}");
    }
}