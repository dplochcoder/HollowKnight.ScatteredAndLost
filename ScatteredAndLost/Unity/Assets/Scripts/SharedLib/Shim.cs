using System;
using UnityEngine;

namespace HK8YPlando.Scripts.SharedLib
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
    public class Shim : Attribute
    {
        public readonly Type baseType;

        public Shim(Type baseType = null)
        {
            this.baseType = baseType ?? typeof(MonoBehaviour);
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ShimField : Attribute
    {
        public readonly string DefaultValue;

        public ShimField(string defaultValue = null) => DefaultValue = defaultValue;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ShimMethod : Attribute { }
}