using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HK8YPlando.Scripts.InternalLib;

public class MonobehaviourPatcher<M> where M : MonoBehaviour
{
    private record Field
    {
        public FieldInfo fi;
        public object value;
    }

    private readonly Lazy<List<Field>> fields;

    public MonobehaviourPatcher(Func<M> prefab, params string[] fieldNames)
    {
        fields = new(() =>
        {
            List<Field> list = [];

            var obj = prefab();
            var type = obj.GetType();
            foreach (var name in fieldNames)
            {
                var fi = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                list.Add(new()
                {
                    fi = fi,
                    value = fi.GetValue(obj)
                });
            }
            return list;
        });
    }

    public void Patch(M component) => fields.Get().ForEach(f => f.fi.SetValue(component, f.value));
}