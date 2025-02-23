using System.Collections.Generic;

namespace HK8YPlando.Util;

internal static class CollectionExtensions
{
    public static T? MaybeMoveNext<T>(this IEnumerator<T> iter) where T : class => iter.MoveNext() ? iter.Current : null;

    public static T Random<T>(this List<T> list) => list[UnityEngine.Random.Range(0, list.Count)];
}
