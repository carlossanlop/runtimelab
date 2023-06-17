// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.ManagedZLib;

/// <summary>
/// <p><strong>From the ZLib manual:</strong></p>
/// <p><code>CompressionStrategy</code> is used to tune the compression algorithm.<br />
/// Use the value <code>DefaultStrategy</code> for normal data, <code>Filtered</code> for data produced by a filter (or predictor),
/// <code>HuffmanOnly</code> to force Huffman encoding only (no string match), or <code>Rle</code> to limit match distances to one
/// (run-length encoding). Filtered data consists mostly of small values with a somewhat random distribution. In this case, the
/// compression algorithm is tuned to compress them better. The effect of <code>Filtered</code> is to force more Huffman coding and]
/// less string matching; it is somewhat intermediate between <code>DefaultStrategy</code> and <code>HuffmanOnly</code>.
/// <code>Rle</code> is designed to be almost as fast as <code>HuffmanOnly</code>, but give better compression for PNG image data.
/// The strategy parameter only affects the compression ratio but not the correctness of the compressed output even if it is not set
/// appropriately. <code>Fixed</code> prevents the use of dynamic Huffman codes, allowing for a simpler decoder for special applications.</p>
///
/// <p><strong>For .NET Framework use:</strong></p>
/// <p>We have investigated compression scenarios for a bunch of different frequently occurring compression data and found that in all
/// cases we investigated so far, <code>DefaultStrategy</code> provided best results</p>
/// <p>See also: How to choose a compression level (in comments to <code>CompressionLevel</code>.</p>
/// </summary>
public enum ZLibCompressionStrategy : int
{
    DefaultStrategy = 0
}
