# XenoAtom.Allocators [![ci](https://github.com/XenoAtom/XenoAtom.Allocators/actions/workflows/ci.yml/badge.svg)](https://github.com/XenoAtom/XenoAtom.Allocators/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/nuget/v/XenoAtom.Allocators.svg)](https://www.nuget.org/packages/XenoAtom.Allocators/)

<img align="right" width="160px" height="160px" src="https://raw.githubusercontent.com/XenoAtom/XenoAtom.Allocators/main/img/XenoAtom.Allocators.png">

This library provides fast, lightweight and low-level memory allocators for .NET.

## ✨ Features

- Implementation of a [TLSF (Two-Level Segregated Fit) allocator](http://www.gii.upv.es/tlsf/) with the following features
  - Implementation of the paper [TLSF: a NewDynamic Memory Allocator for Real-Time Systems](http://www.gii.upv.es/tlsf/files/papers/ecrts04_tlsf.pdf)
  - Provides an agnostic backend chunk allocator architecture (native memory, managed memory, custom e.g GPU memory...)
  - 4GB of addressable maximum allocation size.
  - Minimum allocation size/alignment of 64 bytes (32 bytes overhead per allocation)
  - Configurable power-of-two alignment per allocator instance.
- NativeAOT compatible.
- Support for `net8.0`+

## 📖 User Guide

For more details on how to use XenoAtom.Allocators, please visit the [user guide](https://github.com/XenoAtom/XenoAtom.Allocators/blob/main/doc/readme.md).

## 📊 Benchmarks

The benchmark is available in the [XenoAtom.Allocators.Bench](src/XenoAtom.Allocators.Bench/Program.cs).

It is comparing the performance of the TLSF allocator against the standard `malloc`/`free` from the C runtime library.

The benchmark consists of making 2048 allocations and frees within a range of random sizes between `64, 96, 150, 200, 400, 1024, 4096` bytes.

```
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3810/23H2/2023Update/SunValley3)
AMD Ryzen 9 7950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.300
  [Host]     : .NET 8.0.6 (8.0.624.26715), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.6 (8.0.624.26715), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```

| Method | Mean      | Error    | StdDev   | Ratio | RatioSD |
|------- |----------:|---------:|---------:|------:|--------:|
| Tlsf   |  82.27 us | 1.285 us | 1.202 us |  0.78 |    0.02 |
| Libc   | 105.34 us | 1.898 us | 2.110 us |  1.00 |    0.00 |

The benchmark shows that the **TLSF allocator is 20%+ faster** than the standard `malloc`/`free` from the C runtime library.


## 🪪 License

This software is released under the [BSD-2-Clause license](https://opensource.org/licenses/BSD-2-Clause). 

## 🤗 Author

Alexandre Mutel aka [XenoAtom](https://xoofx.github.io).
