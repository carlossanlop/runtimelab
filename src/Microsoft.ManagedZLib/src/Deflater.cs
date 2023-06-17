// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;

namespace Microsoft.ManagedZLib;

/// <summary>
/// Provides a wrapper around the ZLib compression API.
/// </summary>
internal sealed class Deflater : IDisposable
{
    private readonly ZLibStreamHandle _zlibStream;
    private MemoryHandle _inputBufferHandle;
    private bool _isDisposed;
    private const int minWindowBits = -15;  // WindowBits must be between -8..-15 to write no header, 8..15 for a
    private const int maxWindowBits = 31;   // zlib header, or 24..31 for a GZip header

    // Note, DeflateStream or the deflater do not try to be thread safe.
    // The lock is just used to make writing to unmanaged structures atomic to make sure
    // that they do not get inconsistent fields that may lead to an unmanaged memory violation.
    // To prevent *managed* buffer corruption or other weird behaviour users need to synchronise
    // on the stream explicitly.
    private object SyncLock => this;

    internal Deflater(CompressionLevel compressionLevel, int windowBits)
    {
        Debug.Assert(windowBits >= minWindowBits && windowBits <= maxWindowBits);
        ZLibCompressionLevel zlibCompressionLevel;
        int memLevel;

        switch (compressionLevel)
        {
            // See the note in ZLibNative.CompressionLevel for the recommended combinations.

            case CompressionLevel.Optimal:
                zlibCompressionLevel = ZLibCompressionLevel.DefaultCompression;
                memLevel = ManagedZLib.Deflate_DefaultMemLevel;
                break;

            case CompressionLevel.Fastest:
                zlibCompressionLevel = ZLibCompressionLevel.BestSpeed;
                memLevel = ManagedZLib.Deflate_DefaultMemLevel;
                break;

            case CompressionLevel.NoCompression:
                zlibCompressionLevel = ZLibCompressionLevel.NoCompression;
                memLevel = ManagedZLib.Deflate_NoCompressionMemLevel;
                break;

            case CompressionLevel.SmallestSize:
                zlibCompressionLevel = ZLibCompressionLevel.BestCompression;
                memLevel = ManagedZLib.Deflate_DefaultMemLevel;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(compressionLevel));
        }

        ZLibCompressionStrategy strategy = ZLibCompressionStrategy.DefaultStrategy;

        ZLibErrorCode errC;
        try
        {
            errC = ManagedZLib.CreateZLibStreamForDeflate(out _zlibStream, zlibCompressionLevel,
                                                         windowBits, memLevel, strategy);
        }
        catch (Exception cause)
        {
            throw new ZLibException("SR.ZLibErrorDLLLoadError", cause);
        }

        switch (errC)
        {
            case ZLibErrorCode.Ok:
                return;

            case ZLibErrorCode.MemError:
                throw new ZLibException("SR.ZLibErrorNotEnoughMemory", "deflateInit2_", (int)errC, _zlibStream.GetErrorMessage());

            case ZLibErrorCode.VersionError:
                throw new ZLibException("SR.ZLibErrorVersionMismatch", "deflateInit2_", (int)errC, _zlibStream.GetErrorMessage());

            case ZLibErrorCode.StreamError:
                throw new ZLibException("SR.ZLibErrorIncorrectInitParameters", "deflateInit2_", (int)errC, _zlibStream.GetErrorMessage());

            default:
                throw new ZLibException("SR.ZLibErrorUnexpected", "deflateInit2_", (int)errC, _zlibStream.GetErrorMessage());
        }
    }

    ~Deflater()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
                _zlibStream.Dispose();

            DeallocateInputBufferHandle();
            _isDisposed = true;
        }
    }

    public bool NeedsInput() => 0 == _zlibStream.AvailIn;

    internal void SetInput(ReadOnlySpan<byte> inputBuffer)
    {
        Debug.Assert(NeedsInput(), "We have something left in previous input!");
        if (0 == inputBuffer.Length)
        {
            return;
        }

        lock (SyncLock)
        {
            //_inputBufferHandle = inputBuffer.Pin();

            //_zlibStream.NextIn = (IntPtr)_inputBufferHandle.Pointer;
            //_zlibStream.AvailIn = (uint)inputBuffer.Length;
        }
    }

    //internal void SetInput(byte* inputBufferPtr, int count)
    //{
    //    Debug.Assert(NeedsInput(), "We have something left in previous input!");
    //    Debug.Assert(inputBufferPtr != null);

    //    if (count == 0)
    //    {
    //        return;
    //    }

    //    lock (SyncLock)
    //    {
    //        _zlibStream.NextIn = (IntPtr)inputBufferPtr;
    //        _zlibStream.AvailIn = (uint)count;
    //    }
    //}

    internal int GetDeflateOutput(byte[] outputBuffer)
    {
        Debug.Assert(null != outputBuffer, "Can't pass in a null output buffer!");
        Debug.Assert(!NeedsInput(), "GetDeflateOutput should only be called after providing input");

        try
        {
            int bytesRead;
            ReadDeflateOutput(outputBuffer, ZLibFlushCode.NoFlush, out bytesRead);
            return bytesRead;
        }
        finally
        {
            // Before returning, make sure to release input buffer if necessary:
            if (0 == _zlibStream.AvailIn)
            {
                DeallocateInputBufferHandle();
            }
        }
    }

    private ZLibErrorCode ReadDeflateOutput(byte[] outputBuffer, ZLibFlushCode flushCode, out int bytesRead)
    {
        Debug.Assert(outputBuffer?.Length > 0);

        lock (SyncLock)
        {
            //fixed (byte* bufPtr = &outputBuffer[0])
            //{
            //    _zlibStream.NextOut = (IntPtr)bufPtr;
            //    _zlibStream.AvailOut = (uint)outputBuffer.Length;

            //    ZLibErrorCode errC = Deflate(flushCode);
            //    bytesRead = outputBuffer.Length - (int)_zlibStream.AvailOut;

            //    return errC;
            //}
        }
        // TEMP
        bytesRead = 0;
        return ZLibErrorCode.Ok;
    }

    internal bool Finish(byte[] outputBuffer, out int bytesRead)
    {
        Debug.Assert(null != outputBuffer, "Can't pass in a null output buffer!");
        Debug.Assert(outputBuffer.Length > 0, "Can't pass in an empty output buffer!");

        ZLibErrorCode errC = ReadDeflateOutput(outputBuffer, ZLibFlushCode.Finish, out bytesRead);
        return errC == ZLibErrorCode.StreamEnd;
    }

    /// <summary>
    /// Returns true if there was something to flush. Otherwise False.
    /// </summary>
    internal bool Flush(byte[] outputBuffer, out int bytesRead)
    {
        Debug.Assert(null != outputBuffer, "Can't pass in a null output buffer!");
        Debug.Assert(outputBuffer.Length > 0, "Can't pass in an empty output buffer!");
        Debug.Assert(NeedsInput(), "We have something left in previous input!");


        // Note: we require that NeedsInput() == true, i.e. that 0 == _zlibStream.AvailIn.
        // If there is still input left we should never be getting here; instead we
        // should be calling GetDeflateOutput.

        return ReadDeflateOutput(outputBuffer, ZLibFlushCode.SyncFlush, out bytesRead) == ZLibErrorCode.Ok;
    }

    private void DeallocateInputBufferHandle()
    {
        lock (SyncLock)
        {
            _zlibStream.AvailIn = 0;
            _zlibStream.NextIn = ManagedZLib.ZNullPtr;
            _inputBufferHandle.Dispose();
        }
    }

    private ZLibErrorCode Deflate(ZLibFlushCode flushCode)
    {
        ZLibErrorCode errC;
        try
        {
            errC = _zlibStream.Deflate(flushCode);
        }
        catch (Exception cause)
        {
            throw new ZLibException("SR.ZLibErrorDLLLoadError", cause);
        }

        switch (errC)
        {
            case ZLibErrorCode.Ok:
            case ZLibErrorCode.StreamEnd:
                return errC;

            case ZLibErrorCode.BufError:
                return errC;  // This is a recoverable error

            case ZLibErrorCode.StreamError:
                throw new ZLibException("SR.ZLibErrorInconsistentStream", "deflate", (int)errC, _zlibStream.GetErrorMessage());

            default:
                throw new ZLibException("SR.ZLibErrorUnexpected", "deflate", (int)errC, _zlibStream.GetErrorMessage());
        }
    }
}
