using System.IO;
using LittlePaws;
using UnityEditor;
using UnityEngine;

/// <summary>Exports the actual runtime illustrations as reviewable design previews.</summary>
public static class RescueArtChecks
{
    [MenuItem("Little Paws/Export Rescue character designs")]
    public static void Export()
    {
        string folder = Path.GetFullPath("Exports/RescueDesigns");
        Directory.CreateDirectory(folder);
        Save(folder, "rooster-hero", RescueArt.Hero(false));
        Save(folder, "hen-hero", RescueArt.Hero(true));
        Save(folder, "roach-swordsman", RescueArt.Roach());
        Save(folder, "woodland-background", RescueArt.Forest());
        Save(folder, "dirt-path", RescueArt.Dirt());
        Debug.Log("Rescue the Egg character designs exported to " + folder);
    }
    static void Save(string folder, string name, Sprite sprite)
    {
        File.WriteAllBytes(Path.Combine(folder, name + ".png"), sprite.texture.EncodeToPNG());
    }
}
