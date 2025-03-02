using HK8YPlando.Scripts.Framework;
using ItemChanger;
using ItemChanger.Containers;
using UnityEngine;

namespace HK8YPlando.Rando;

internal class SuperSoulTotemContainer : SoulTotemContainer
{
    public const string ContainerName = "SuperSoulTotem";

    static SuperSoulTotemContainer() => DefineContainer<SuperSoulTotemContainer>();

    public override string Name => ContainerName;

    public override GameObject GetNewContainer(ContainerInfo info)
    {
        var obj = base.GetNewContainer(info);
        SuperSoulTotem.EnhanceTotem(obj);
        return obj;
    }
}
