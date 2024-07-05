// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.
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
        //for (int i = 0; i < 100_000; i++)
        //{
        //    bench.Tlsf();
        //}
        //bench.Cleanup();

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