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
    private readonly ManagedZLib.ZLibStreamHandle _zlibStream;
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
        ManagedZLib.CompressionLevel zlibCompressionLevel;
        int memLevel;

        switch (compressionLevel)
        {
            // See the note in ZLibNative.CompressionLevel for the recommended combinations.

            case CompressionLevel.Optimal:
                zlibCompressionLevel = ManagedZLib.CompressionLevel.DefaultCompression;
                memLevel = ManagedZLib.Deflate_DefaultMemLevel;
                break;

            case CompressionLevel.Fastest:
                zlibCompressionLevel = ManagedZLib.CompressionLevel.BestSpeed;
                memLevel = ManagedZLib.Deflate_DefaultMemLevel;
                break;

            case CompressionLevel.NoCompression:
                zlibCompressionLevel = ManagedZLib.CompressionLevel.NoCompression;
                memLevel = ManagedZLib.Deflate_NoCompressionMemLevel;
                break;

            case CompressionLevel.SmallestSize:
                zlibCompressionLevel = ManagedZLib.CompressionLevel.BestCompression;
                memLevel = ManagedZLib.Deflate_DefaultMemLevel;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(compressionLevel));
        }

        ManagedZLib.CompressionStrategy strategy = ManagedZLib.CompressionStrategy.DefaultStrategy;

        Microsoft.ManagedZLib.ManagedZLib.ErrorCode errC;
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
            case Microsoft.ManagedZLib.ManagedZLib.ErrorCode.Ok:
                return;

            case Microsoft.ManagedZLib.ManagedZLib.ErrorCode.MemError:
                throw new ZLibException("SR.ZLibErrorNotEnoughMemory", "deflateInit2_", (int)errC, _zlibStream.GetErrorMessage());

            case Microsoft.ManagedZLib.ManagedZLib.ErrorCode.VersionError:
                throw new ZLibException("SR.ZLibErrorVersionMismatch", "deflateInit2_", (int)errC, _zlibStream.GetErrorMessage());

            case Microsoft.ManagedZLib.ManagedZLib.ErrorCode.StreamError:
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

    internal void SetInput(ReadOnlyMemory<byte> inputBuffer)
    {
        Debug.Assert(NeedsInput(), "We have something left in previous input!");
        if (0 == inputBuffer.Length)
        {
            return;
        }

        lock (SyncLock)
        {
            _inputBufferHandle = inputBuffer.Pin();

            _zlibStream.NextIn = (IntPtr)_inputBufferHandle.Pointer;
            _zlibStream.AvailIn = (uint)inputBuffer.Length;
        }
    }

    internal void SetInput(byte* inputBufferPtr, int count)
    {
        Debug.Assert(NeedsInput(), "We have something left in previous input!");
        Debug.Assert(inputBufferPtr != null);

        if (count == 0)
        {
            return;
        }

        lock (SyncLock)
        {
            _zlibStream.NextIn = (IntPtr)inputBufferPtr;
            _zlibStream.AvailIn = (uint)count;
        }
    }

    internal int GetDeflateOutput(byte[] outputBuffer)
    {
        Debug.Assert(null != outputBuffer, "Can't pass in a null output buffer!");
        Debug.Assert(!NeedsInput(), "GetDeflateOutput should only be called after providing input");

        try
        {
            int bytesRead;
            ReadDeflateOutput(outputBuffer, Microsoft.ManagedZLib.ManagedZLib.FlushCode.NoFlush, out bytesRead);
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

    private Microsoft.ManagedZLib.ManagedZLib.ErrorCode ReadDeflateOutput(byte[] outputBuffer, Microsoft.ManagedZLib.ManagedZLib.FlushCode flushCode, out int bytesRead)
    {
        Debug.Assert(outputBuffer?.Length > 0);

        lock (SyncLock)
        {
            fixed (byte* bufPtr = &outputBuffer[0])
            {
                _zlibStream.NextOut = (IntPtr)bufPtr;
                _zlibStream.AvailOut = (uint)outputBuffer.Length;

                Microsoft.ManagedZLib.ManagedZLib.ErrorCode errC = Deflate(flushCode);
                bytesRead = outputBuffer.Length - (int)_zlibStream.AvailOut;

                return errC;
            }
        }
    }

    internal bool Finish(byte[] outputBuffer, out int bytesRead)
    {
        Debug.Assert(null != outputBuffer, "Can't pass in a null output buffer!");
        Debug.Assert(outputBuffer.Length > 0, "Can't pass in an empty output buffer!");

        Microsoft.ManagedZLib.ManagedZLib.ErrorCode errC = ReadDeflateOutput(outputBuffer, Microsoft.ManagedZLib.ManagedZLib.FlushCode.Finish, out bytesRead);
        return errC == Microsoft.ManagedZLib.ManagedZLib.ErrorCode.StreamEnd;
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

        return ReadDeflateOutput(outputBuffer, Microsoft.ManagedZLib.ManagedZLib.FlushCode.SyncFlush, out bytesRead) == Microsoft.ManagedZLib.ManagedZLib.ErrorCode.Ok;
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

    private Microsoft.ManagedZLib.ManagedZLib.ErrorCode Deflate(Microsoft.ManagedZLib.ManagedZLib.FlushCode flushCode)
    {
        Microsoft.ManagedZLib.ManagedZLib.ErrorCode errC;
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
            case Microsoft.ManagedZLib.ManagedZLib.ErrorCode.Ok:
            case Microsoft.ManagedZLib.ManagedZLib.ErrorCode.StreamEnd:
                return errC;

            case Microsoft.ManagedZLib.ManagedZLib.ErrorCode.BufError:
                return errC;  // This is a recoverable error

            case Microsoft.ManagedZLib.ManagedZLib.ErrorCode.StreamError:
                throw new ZLibException("SR.ZLibErrorInconsistentStream", "deflate", (int)errC, _zlibStream.GetErrorMessage());

            default:
                throw new ZLibException("SR.ZLibErrorUnexpected", "deflate", (int)errC, _zlibStream.GetErrorMessage());
        }
    }
}
