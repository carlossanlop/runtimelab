// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.ManagedZLib;

internal static class Interop
{
    internal static class ZLib
    {
        //[LibraryImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_DeflateInit2_")]
        internal static unsafe partial ManagedZLib.ErrorCode DeflateInit2_(
            ZStream* stream,
            ManagedZLib.CompressionLevel level,
            ManagedZLib.CompressionMethod method,
            int windowBits,
            int memLevel,
            ManagedZLib.CompressionStrategy strategy);

        //[LibraryImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_Deflate")]
        internal static unsafe partial ManagedZLib.ErrorCode Deflate(ZStream* stream, ManagedZLib.FlushCode flush);

        //[LibraryImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_DeflateEnd")]
        internal static unsafe partial ManagedZLib.ErrorCode DeflateEnd(ZStream* stream);

        //[LibraryImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_InflateInit2_")]
        internal static unsafe partial ManagedZLib.ErrorCode InflateInit2_(ZStream* stream, int windowBits);

        //[LibraryImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_Inflate")]
        internal static unsafe partial ManagedZLib.ErrorCode Inflate(ZStream* stream, ManagedZLib.FlushCode flush);

        //[LibraryImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_InflateEnd")]
        internal static unsafe partial ManagedZLib.ErrorCode InflateEnd(ZStream* stream);

        //[LibraryImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_Crc32")]
        internal static unsafe partial uint crc32(uint crc, byte* buffer, int len);
    }
}
