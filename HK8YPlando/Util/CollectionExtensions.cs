using System.Collections.Generic;

namespace HK8YPlando.Util;

internal static class CollectionExtensions
{
    public static T? MaybeMoveNext<T>(this IEnumerator<T> iter) where T : class => iter.MoveNext() ? iter.Current : null;
}
