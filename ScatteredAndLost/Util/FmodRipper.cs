using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HK8YPlando.Util;

internal static class FmodRipper
{
    // up to you how best to get this path.
    const string CELESTE_MUSIC_PATH = @"D:\Program Files\Steam\steamapps\common\Celeste\Content\FMOD\Desktop\music.bank";

    // Usage (pseudocode): ExtractMusic("path/to/ScatteredAndLost/Music", {
    //  "mus_rmx_01_forsakencity_intro": "music1intro",
    //  "mus_rmx_01_forsakencity_loop": "music1loop",
    //  "mus_rmx_05_mirrortemple": "music2"
    // });
    public static async Task<string> ExtractMusic(string outputFolderPath, Dictionary<string, string> sampleNameMapping)
    {
        Directory.CreateDirectory(outputFolderPath);

        byte[]? bankData = await TryReadFsb(CELESTE_MUSIC_PATH);

        if (bankData == null)
        {
            return "Unable to read FSB data.";
        }

        if (FsbLoader.TryLoadFsbFromByteArray(bankData, out FmodSoundBank? bank))
        {
            foreach (FmodSample sample in bank!.Samples) 
            {
                if (sample.Name != null && sampleNameMapping.TryGetValue(sample.Name, out string mappedName) 
                    && sample.RebuildAsStandardFileFormat(out byte[]? data, out string? ext))
                {
                    string fileName = $"{mappedName}.{ext}";
                    string filePath = Path.Combine(outputFolderPath, fileName);

                    using FileStream sampleFile = File.Create(filePath);
                    await sampleFile.WriteAsync(data, 0, data!.Length);
                }
            }
            return "Successfully extracted Celeste assets.";
        }
        return "Failed to load bank.";
    }

    private static int Search(byte[] src, byte[] pattern)
    {
        int maxStart = src.Length - pattern.Length + 1;
        for (int i = 0; i < maxStart; i++)
        {
            if (src[i] == pattern[0])
            {
                for (int j = 1; j < pattern.Length; j++)
                {
                    if (src[i + j] != pattern[j])
                    {
                        break;
                    }
                    if (j == pattern.Length - 1)
                    {
                        return i;
                    }
                }
            }
        }
        return -1;
    }

    private static async Task<byte[]?> TryReadFsb(string filePath)
    {
        using Stream fs = File.OpenRead(filePath);
        using MemoryStream bankStream = new();
        await fs.CopyToAsync(bankStream);

        bankStream.Position = 0;
        int headerIdx = Search(bankStream.GetBuffer(), Encoding.ASCII.GetBytes("SNDH"));
        bankStream.Position = 0;

        if (headerIdx == -1)
        {
            return null;
        }

        BinaryReader reader = new(bankStream);
        MemoryStream fsbStream = new();
        bankStream.Position = headerIdx + 12;
        int nextOffset = reader.ReadInt32();
        reader.ReadInt32();
        bankStream.Position = nextOffset;
        await bankStream.CopyToAsync(fsbStream);

        return fsbStream.ToArray();
    }
}
