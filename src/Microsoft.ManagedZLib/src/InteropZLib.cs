// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.ManagedZLib;

internal static class InteropZLib
{
    //[LibraryImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_DeflateInit2_")]
    internal static ZLibErrorCode DeflateInit2_(
        ZStream zStream,
        ZLibCompressionLevel level,
        ZLibCompressionMethod method,
        int windowBits,
        int memLevel,
        ZLibCompressionStrategy strategy)
    {
        return ZLibErrorCode.Ok;
    }

    //[LibraryImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_Deflate")]
    internal static ZLibErrorCode Deflate(ZStream zStream, ZLibFlushCode flush)
    {
        return ZLibErrorCode.Ok;
    }

    //[LibraryImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_DeflateEnd")]
    internal static ZLibErrorCode DeflateEnd(ZStream zStream)
    {
        return ZLibErrorCode.Ok;
    }

    //[LibraryImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_InflateInit2_")]
    internal static ZLibErrorCode InflateInit2_(ZStream zStream, int windowBits)
    {
        return ZLibErrorCode.Ok;
    }

    //[LibraryImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_Inflate")]
    internal static ZLibErrorCode Inflate(ZStream zStream, ZLibFlushCode flush)
    {
        return ZLibErrorCode.Ok;
    }

    //[LibraryImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_InflateEnd")]
    internal static ZLibErrorCode InflateEnd(ZStream zStream)
    {
        return ZLibErrorCode.Ok;
    }

    //[LibraryImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_Crc32")]
    internal static uint Crc32(uint crc, Span<byte> buffer)
    {
        return 0;
    }
}
