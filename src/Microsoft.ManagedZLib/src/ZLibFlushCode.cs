// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.ManagedZLib;

public enum ZLibFlushCode : int
{
    NoFlush = 0,
    SyncFlush = 2,
    Finish = 4,
    Block = 5
}
