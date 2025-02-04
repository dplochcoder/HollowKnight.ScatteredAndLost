using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HK8YPlando.Scripts.SharedLib
{
    public static class GameObjectExtensions
    {
        public static void SetVelocityX(this Rigidbody2D self, float x)
        {
            var v = self.velocity;
            v.x = x;
            self.velocity = v;
        }

        public static void SetVelocityY(this Rigidbody2D self, float y)
        {
            var v = self.velocity;
            v.y = y;
            self.velocity = v;
        }

        public static GameObject FindChild(this GameObject self, string name)
        {
            foreach (var child in self.Children()) if (child.name == name) return child;
            return null;
        }

        public static void SetParent(this GameObject self, GameObject parent) => self.transform.SetParent(parent.transform);

        public static void Unparent(this GameObject self) => self.transform.parent = null;

        public static GameObject Parent(this GameObject self) => self.transform.parent?.gameObject;

        public static bool Contains(this BoxCollider2D self, Vector2 vec, float xBuffer = 0, float yBuffer = 0)
        {
            var b = self.bounds;
            var x1 = b.min.x - xBuffer;
            var y1 = b.min.y - yBuffer;
            var x2 = b.max.x + xBuffer;
            var y2 = b.max.y + yBuffer;

            return vec.x >= x1 && vec.x <= x2 && vec.y >= y1 && vec.y <= y2;
        }

        public static T GetOrAddComponent<T>(this GameObject self) where T : Component => self.GetComponent<T>() ?? self.AddComponent<T>();

        public static IEnumerable<T> FindComponentsRecursive<T>(this Scene self) where T : Component
        {
            foreach (var rootObj in self.GetRootGameObjects())
                foreach (var component in rootObj.FindComponentsRecursive<T>())
                    yield return component;
        }

        public static IEnumerable<T> FindComponentsRecursive<T>(this GameObject self) where T : Component
        {
            foreach (var component in self.GetComponents<T>()) yield return component;
            for (int i = 0; i < self.transform.childCount; i++)
                foreach (var component in self.transform.GetChild(i).gameObject.FindComponentsRecursive<T>())
                    yield return component;
        }

        public static IEnumerable<GameObject> Children(this GameObject self)
        {
            foreach (Transform child in self.transform) yield return child.gameObject;
        }

        public static void DestroyChildrenImmediate(this GameObject self, Func<GameObject, bool> filter = null)
        {
            var children = new List<GameObject>(self.Children());
            foreach (var child in children) if (filter == null || filter(child)) UnityEngine.Object.DestroyImmediate(child, true);
        }
    }
}
