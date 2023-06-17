// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.ManagedZLib;

/// <summary>
/// This class provides declaration for constants and PInvokes as well as some basic tools for exposing the
/// native System.IO.Compression.Native.dll (effectively, ZLib) library to managed code.
///
/// See also: How to choose a compression level (in comments to <code>CompressionLevel</code>.
/// </summary>
internal static class ManagedZLib
{
    // This is the NULL pointer for using with ZLib pointers;
    // we prefer it to IntPtr.Zero to mimic the definition of Z_NULL in zlib.h:
    internal static readonly IntPtr ZNullPtr = IntPtr.Zero;

    /// <summary>
    /// <p><strong>From the ZLib manual:</strong></p>
    /// <p>ZLib's <code>windowBits</code> parameter is the base two logarithm of the window size (the size of the history buffer).
    /// It should be in the range 8..15 for this version of the library. Larger values of this parameter result in better compression
    /// at the expense of memory usage. The default value is 15 if deflateInit is used instead.<br /></p>
    /// <strong>Note</strong>:
    /// <code>windowBits</code> can also be -8..-15 for raw deflate. In this case, -windowBits determines the window size.
    /// <code>Deflate</code> will then generate raw deflate data with no ZLib header or trailer, and will not compute an adler32 check value.<br />
    /// <p>See also: How to choose a compression level (in comments to <code>CompressionLevel</code>.</p>
    /// </summary>
    public const int Deflate_DefaultWindowBits = -15; // Legal values are 8..15 and -8..-15. 15 is the window size,
                                                      // negative val causes deflate to produce raw deflate data (no zlib header).

    /// <summary>
    /// <p><strong>From the ZLib manual:</strong></p>
    /// <p>ZLib's <code>windowBits</code> parameter is the base two logarithm of the window size (the size of the history buffer).
    /// It should be in the range 8..15 for this version of the library. Larger values of this parameter result in better compression
    /// at the expense of memory usage. The default value is 15 if deflateInit is used instead.<br /></p>
    /// </summary>
    public const int ZLib_DefaultWindowBits = 15;

    /// <summary>
    /// <p>Zlib's <code>windowBits</code> parameter is the base two logarithm of the window size (the size of the history buffer).
    /// For GZip header encoding, <code>windowBits</code> should be equal to a value between 8..15 (to specify Window Size) added to
    /// 16. The range of values for GZip encoding is therefore 24..31.
    /// <strong>Note</strong>:
    /// The GZip header will have no file name, no extra data, no comment, no modification time (set to zero), no header crc, and
    /// the operating system will be set based on the OS that the ZLib library was compiled to. <code>ZStream.adler</code>
    /// is a crc32 instead of an adler32.</p>
    /// </summary>
    public const int GZip_DefaultWindowBits = 31;

    /// <summary>
    /// <p><strong>From the ZLib manual:</strong></p>
    /// <p>The <code>memLevel</code> parameter specifies how much memory should be allocated for the internal compression state.
    /// <code>memLevel</code> = 1 uses minimum memory but is slow and reduces compression ratio; <code>memLevel</code> = 9 uses maximum
    /// memory for optimal speed. The default value is 8.</p>
    /// <p>See also: How to choose a compression level (in comments to <code>CompressionLevel</code>.</p>
    /// </summary>
    public const int Deflate_DefaultMemLevel = 8;     // Memory usage by deflate. Legal range: [1..9]. 8 is ZLib default.
                                                      // More is faster and better compression with more memory usage.
    public const int Deflate_NoCompressionMemLevel = 7;

    public const byte GZip_Header_ID1 = 31;
    public const byte GZip_Header_ID2 = 139;

    public static ZLibErrorCode CreateZLibStreamForDeflate(out ZLibStreamHandle zLibStreamHandle, ZLibCompressionLevel level,
        int windowBits, int memLevel, ZLibCompressionStrategy strategy)
    {
        zLibStreamHandle = new ZLibStreamHandle();
        return zLibStreamHandle.DeflateInit2_(level, windowBits, memLevel, strategy);
    }


    public static ZLibErrorCode CreateZLibStreamForInflate(out ZLibStreamHandle zLibStreamHandle, int windowBits)
    {
        zLibStreamHandle = new ZLibStreamHandle();
        return zLibStreamHandle.InflateInit2_(windowBits);
    }
}
