using System;

namespace EpubSharp.Misc
{
    internal class Href
    {
        public readonly string Path;
        public readonly string HashLocation;

        public Href(string href)
        {
            Guard.NotNullOrWhiteSpace(href);

            ReadOnlySpan<char> span = href.AsSpan();
            var contentSourceAnchorCharIndex = span.IndexOf('#');
            
            if (contentSourceAnchorCharIndex == -1)
            {
                Path = href;
                HashLocation = string.Empty;
            }
            else
            {
                Path = span.Slice(0, contentSourceAnchorCharIndex).ToString();
                HashLocation = span.Slice(contentSourceAnchorCharIndex + 1).ToString();
            }
        }
    }
}
