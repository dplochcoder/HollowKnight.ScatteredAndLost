using System;

namespace HK8YPlando.Scripts.InternalLib;

internal class Lazy<T>
{
    private Func<T> supplier;
    private T? value;

    public Lazy(Func<T> supplier) { this.supplier = supplier; }

    public T Get()
    {
        value ??= supplier();
        return value;
    }
}