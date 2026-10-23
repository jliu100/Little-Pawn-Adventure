using UnityEditor;
using UnityEngine;

public sealed class RescueHeroImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/LittlePaws/Resources/LittlePaws/Rescue/Heroes/"))return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
        settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
        importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        // Readability is needed once to derive consistent tight bounds for both designs.
        importer.isReadable=true;
    }
}
