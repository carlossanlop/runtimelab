// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using static Microsoft.ManagedZLib.ManagedZLib;

namespace Microsoft.ManagedZLib;

/**
 * Do not remove the nested typing of types inside of <code>System.IO.Compression.ZLibNative</code>.
 * This was done on purpose to:
 *
 * - Achieve the right encapsulation in a situation where <code>ZLibNative</code> may be compiled division-wide
 *   into different assemblies that wish to consume <code>System.IO.Compression.Native</code>. Since <code>internal</code>
 *   scope is effectively like <code>public</code> scope when compiling <code>ZLibNative</code> into a higher
 *   level assembly, we need a combination of inner types and <code>private</code>-scope members to achieve
 *   the right encapsulation.
 *
 * - Achieve late dynamic loading of <code>System.IO.Compression.Native.dll</code> at the right time.
 *   The native assembly will not be loaded unless it is actually used since the loading is performed by a static
 *   constructor of an inner type that is not directly referenced by user code.
 *
 *   In Dev12 we would like to create a proper feature for loading native assemblies from user-specified
 *   directories in order to PInvoke into them. This would preferably happen in the native interop/PInvoke
 *   layer; if not we can add a Framework level feature.
 */

/// <summary>
/// The <code>ZLibStreamHandle</code> could be a <code>CriticalFinalizerObject</code> rather than a
/// <code>SafeHandleMinusOneIsInvalid</code>. This would save an <code>IntPtr</code> field since
/// <code>ZLibStreamHandle</code> does not actually use its <code>handle</code> field.
/// Instead it uses a <code>private ZStream zStream</code> field which is the actual handle data
/// structure requiring critical finalization.
/// However, we would like to take advantage if the better debugability offered by the fact that a
/// <em>releaseHandleFailed MDA</em> is raised if the <code>ReleaseHandle</code> method returns
/// <code>false</code>, which can for instance happen if the underlying ZLib <code>XxxxEnd</code>
/// routines return an failure error code.
/// </summary>
public sealed class ZLibStreamHandle : SafeHandle
{
    public enum State { NotInitialized, InitializedForDeflate, InitializedForInflate, Disposed }

    private ZStream _zStream;

    private volatile State _initializationState;


    public ZLibStreamHandle()
        : base(new IntPtr(-1), true)
    {
        _initializationState = State.NotInitialized;
        SetHandle(IntPtr.Zero);
        _zStream = new();
    }

    public override bool IsInvalid
    {
        get { return handle == new IntPtr(-1); }
    }

    public State InitializationState
    {
        get { return _initializationState; }
    }


    protected override bool ReleaseHandle() =>
        InitializationState switch
        {
            State.NotInitialized => true,
            State.InitializedForDeflate => (DeflateEnd() == ZLibErrorCode.Ok),
            State.InitializedForInflate => (InflateEnd() == ZLibErrorCode.Ok),
            State.Disposed => true,
            _ => false,  // This should never happen. Did we forget one of the State enum values in the switch?
        };

    public IntPtr NextIn
    {
        get { return _zStream.nextIn; }
        set { _zStream.nextIn = value; }
    }

    public uint AvailIn
    {
        get { return _zStream.availIn; }
        set { _zStream.availIn = value; }
    }

    public IntPtr NextOut
    {
        get { return _zStream.nextOut; }
        set { _zStream.nextOut = value; }
    }

    public uint AvailOut
    {
        get { return _zStream.availOut; }
        set { _zStream.availOut = value; }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(InitializationState == State.Disposed, this);
    }


    private void EnsureState(State requiredState)
    {
        if (InitializationState != requiredState)
            throw new InvalidOperationException("InitializationState != " + requiredState.ToString());
    }


    public ZLibErrorCode DeflateInit2_(ZLibCompressionLevel level, int windowBits, int memLevel, ZLibCompressionStrategy strategy)
    {
        EnsureNotDisposed();
        EnsureState(State.NotInitialized);

        ZLibErrorCode errC = InteropZLib.DeflateInit2_(_zStream, level, ZLibCompressionMethod.Deflated, windowBits, memLevel, strategy);
        _initializationState = State.InitializedForDeflate;

        return errC;
    }


    public ZLibErrorCode Deflate(ZLibFlushCode flush)
    {
        EnsureNotDisposed();
        EnsureState(State.InitializedForDeflate);

        return InteropZLib.Deflate(_zStream, flush);
    }


    public ZLibErrorCode DeflateEnd()
    {
        EnsureNotDisposed();
        EnsureState(State.InitializedForDeflate);

        ZLibErrorCode errC = InteropZLib.DeflateEnd(_zStream);
        _initializationState = State.Disposed;
        return errC;
    }


    public ZLibErrorCode InflateInit2_(int windowBits)
    {
        EnsureNotDisposed();
        EnsureState(State.NotInitialized);

        ZLibErrorCode errC = InteropZLib.InflateInit2_(_zStream, windowBits);
        _initializationState = State.InitializedForInflate;
        return errC;
    }


    public ZLibErrorCode Inflate(ZLibFlushCode flush)
    {
        EnsureNotDisposed();
        EnsureState(State.InitializedForInflate);

        return InteropZLib.Inflate(_zStream, flush);
    }


    public ZLibErrorCode InflateEnd()
    {
        EnsureNotDisposed();
        EnsureState(State.InitializedForInflate);

        ZLibErrorCode errC = InteropZLib.InflateEnd(_zStream);
        _initializationState = State.Disposed;

        return errC;
    }

    // This can work even after XxflateEnd().
    public string GetErrorMessage() => _zStream.msg != ZNullPtr ? Marshal.PtrToStringUTF8(_zStream.msg)! : string.Empty;
}
