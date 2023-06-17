// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.ManagedZLib;

public enum ZLibErrorCode : int
{
    Ok = 0,
    StreamEnd = 1,
    StreamError = -2,
    DataError = -3,
    MemError = -4,
    BufError = -5,
    VersionError = -6
}