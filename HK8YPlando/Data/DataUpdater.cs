using HK8YPlando.Scripts.SharedLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using JsonUtil = PurenailCore.SystemUtil.JsonUtil<HK8YPlando.HK8YPlandoMod>;

namespace HK8YPlando.Data;

internal record DebugData
{
    public string LocalAssetBundlesPath = "";
}

public static class DataUpdater
{
    public static string InferGitRoot(string path)
    {
        var info = Directory.GetParent(path);
        while (info != null)
        {
            if (Directory.Exists(Path.Combine(info.FullName, ".git")))
            {
                return info.FullName;
            }
            info = Directory.GetParent(info.FullName);
        }

        return path;
    }

    public static void Run()
    {
        var root = InferGitRoot(Directory.GetCurrentDirectory());

        // Debug data
        DebugData debugData = new() { LocalAssetBundlesPath = $"{root}/HK8YPlando/Unity/Assets/AssetBundles" };
        JsonUtil.RewriteJsonFile(debugData, $"{root}/HK8YPlando/Resources/Data/debug.json");

        // Code generation.
        var deferredShimsDir = DeferredGenerateUnityShims(root);
        var shimsDir = deferredShimsDir();

        // TODO: Make this a separate project if we can figure out how to rebuild UnityScriptShims first.
        CopyDlls();
    }

    public static void CopyDlls()
    {
        var root = InferGitRoot(Directory.GetCurrentDirectory());

        CopyDll(root, "UnityScriptShims/bin/Debug/net472/HK8YPlando.dll", "HK8YPlando/Unity/Assets/Assemblies/HK8YPlando.dll");
    }

    private static void CopyDll(string root, string src, string dst)
    {
        var inputDll = Path.Combine(root, src);
        var outputDll = Path.Combine(root, dst);
        if (File.Exists(outputDll)) File.Delete(outputDll);
        File.Copy(inputDll, outputDll);
    }

    private static Func<string> DeferredGenerateDirectory(string dir, Action<string> generator)
    {
        string gen = dir;
        string gen2 = $"{dir}.tmp";
        if (Directory.Exists(gen2)) Directory.Delete(gen2, true);
        Directory.CreateDirectory(gen2);

        generator(gen2);

        // On success, swap the dirs.
        return () =>
        {
            if (Directory.Exists(gen)) Directory.Delete(gen, true);
            Directory.Move(gen2, gen);
            return gen;
        };
    }

    private static void GenerateUnityShimsImpl(string root)
    {
        typeof(DataUpdater).Assembly.GetTypes().Where(t => t.IsDefined(typeof(Shim), false)).ForEach(type => GenerateShimFile(type, root));
    }

    private static Func<string> DeferredGenerateUnityShims(string root)
    {
        var path = $"{root}/UnityScriptShims/Scripts/Generated";
        return DeferredGenerateDirectory(path, GenerateUnityShimsImpl);
    }

    private static void GenerateShimFile(Type type, string dir)
    {
        string ns = type.Namespace;
        string origNs = ns;
        if (ns == "HK8YPlando.Scripts") ns = "";
        else if (ns.ConsumePrefix("HK8YPlando.Scripts.", out var trimmed)) ns = trimmed;

        string pathDir = ns.Length == 0 ? $"{dir}" : $"{dir}/{ns.Replace('.', '/')}";
        string path = $"{pathDir}/{type.Name}.cs";

        var baseType = type.GetCustomAttribute<Shim>()?.baseType ?? typeof(MonoBehaviour);
        string header;
        List<string> fieldStrs = [];
        List<string> attrStrs = [];
        if (type.IsEnum)
        {
            header = $"enum {type.Name}";
            foreach (var v in type.GetEnumValues()) fieldStrs.Add($"{v},");
        }
        else
        {
            foreach (var rc in type.GetCustomAttributes<RequireComponent>())
            {
                if (rc.m_Type0 != null) attrStrs.Add(RequireComponentStr(origNs, rc.m_Type0));
                if (rc.m_Type1 != null) attrStrs.Add(RequireComponentStr(origNs, rc.m_Type1));
                if (rc.m_Type2 != null) attrStrs.Add(RequireComponentStr(origNs, rc.m_Type2));
            }

            header = $"class {type.Name} : {PrintType(origNs, baseType)}";
            foreach (var f in type.GetFields().Where(f => f.IsDefined(typeof(ShimField), true)))
            {
                List<string> fattrStrs = [];
                foreach (var h in f.GetCustomAttributes<HeaderAttribute>()) fattrStrs.Add($"[UnityEngine.Header(\"{h.header}\")]");

                var fAttr = f.GetCustomAttribute<ShimField>();
                var defaultValue = fAttr.DefaultValue;
                string dv = defaultValue != null ? $" = {defaultValue}" : "";
                fieldStrs.Add($"{JoinIndented(fattrStrs, 8)}public {PrintType(origNs, f.FieldType)} {f.Name}{dv};");
            }

            foreach (var m in type.GetMethods().Where(m => m.IsDefined(typeof(ShimMethod), true)))
            {
                if (m.ReturnType != typeof(void)) throw new ArgumentException($"Method {m.Name} must return void");

                var paramsStr = string.Join(", ", m.GetParameters().Select(p => $"{PrintType(origNs, p.ParameterType)} {p.Name}"));
                fieldStrs.Add($"public void {m.Name}({paramsStr}) {{ }}");
            }
        }

        var content = $@"namespace {origNs}
{{
    {JoinIndented(attrStrs, 4)}public {header}
    {{
        {JoinIndented(fieldStrs, 8)}
    }}
}}";

        WriteSourceCode(path, content);
    }

    private static string RequireComponentStr(string ns, Type type) => $"[UnityEngine.RequireComponent(typeof({PrintType(ns, type)}))]";

    private static void WriteSourceCode(string path, string content)
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        File.WriteAllText(path, content.Replace("\r\n", "\n").Replace("\n", "\r\n"));
    }

    private static string Pad(string src, int indent)
    {
        var splits = src.Split('\n');
        for (int i = 1; i < splits.Length; i++) splits[i] = $"{new string(' ', indent)}{splits[i]}";
        return string.Join("", splits);
    }

    private static string JoinIndented(List<string> list, int indent) => string.Join("", list.Select(s => $"{Pad(s, indent)}\n{new string(' ', indent)}"));

    private static string PrintType(string ns, Type t)
    {
        string s = PrintTypeImpl(ns, t);

        if (s.ConsumePrefix($"{ns}.", out string trimmed)) return trimmed;
        else return s;
    }

    private static string PrintTypeImpl(string ns, Type t)
    {
        if (!t.IsGenericType)
        {
            if (t == typeof(bool)) return "bool";
            if (t == typeof(int)) return "int";
            if (t == typeof(float)) return "float";
            if (t == typeof(string)) return "string";

            return t.FullName;
        }

        string baseName = t.FullName;
        baseName = baseName.Substring(0, baseName.IndexOf('`'));
        List<string> types = t.GenericTypeArguments.Select(t => PrintType(ns, t)).ToList();
        return $"{baseName}<{string.Join(", ", types)}>";
    }
}