# XenoAtom.Allocators [![ci](https://github.com/XenoAtom/XenoAtom.Allocators/actions/workflows/ci.yml/badge.svg)](https://github.com/XenoAtom/XenoAtom.Allocators/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/nuget/v/XenoAtom.Allocators.svg)](https://www.nuget.org/packages/XenoAtom.Allocators/)

<img align="right" width="160px" height="160px" src="https://raw.githubusercontent.com/XenoAtom/XenoAtom.Allocators/main/img/XenoAtom.Allocators.png">

This library provides fast, lightweight and low-level memory allocators for .NET.

> **Note**: This library is still in early development and is not yet ready for production use.

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

## 🪪 License

This software is released under the [BSD-2-Clause license](https://opensource.org/licenses/BSD-2-Clause). 

## 🤗 Author

Alexandre Mutel aka [XenoAtom](https://xoofx.github.io).
