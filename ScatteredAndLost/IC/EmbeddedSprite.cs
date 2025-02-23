using ItemChanger.Internal;

namespace HK8YPlando.IC;

public class EmbeddedSprite : ItemChanger.EmbeddedSprite
{
    private static readonly SpriteManager manager = new(typeof(EmbeddedSprite).Assembly, "HK8YPlando.Resources.Sprites.");

    public EmbeddedSprite(string key) => this.key = key.Replace("/", ".");

    public override SpriteManager SpriteManager => manager;
}
