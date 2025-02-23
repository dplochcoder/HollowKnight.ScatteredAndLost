namespace HK8YPlando.Scripts.InternalLib;

// Simple class for captures that wraps a primitive.
internal class Wrapped<T>
{
    internal T Value;

    internal Wrapped(T value) => Value = value;
}