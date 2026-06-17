using System;
using EpubSharp.Format;

namespace EpubSharp.Extensions
{
    internal static class ByteArrayExt
    {
        public static byte[] TrimEncodingPreamble(this byte[] data)
        {
            Guard.NotNull(data);
            
            var preamble = Constants.DefaultEncoding.GetPreamble();
            ReadOnlySpan<byte> dataSpan = data;
            
            if (dataSpan.StartsWith(preamble))
            {
                return dataSpan.Slice(preamble.Length).ToArray();
            }

            return data;
        }
    }
}
