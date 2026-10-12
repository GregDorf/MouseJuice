using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;

public class TextureAtlasSlicer : EditorWindow
{
    private TextureImporter importer;
    private TextAsset xmlAsset;

    private SpriteAlignment alignment = SpriteAlignment.Center;
    private Vector2 customPivot = new Vector2(0.5f, 0.5f);

    [MenuItem("CONTEXT/TextureImporter/Slice using TextureAtlas XML", true)]
    private static bool Validate(TextureImporter importer)
    {
        return importer != null && importer.textureType == TextureImporterType.Sprite;
    }

    [MenuItem("CONTEXT/TextureImporter/Slice using TextureAtlas XML")]
    private static void Open(MenuCommand command)
    {
        var importer = command.context as TextureImporter;
        if (importer == null) return;

        var window = GetWindow<TextureAtlasSlicer>(true, "TextureAtlas Slicer", true);
        window.importer = importer;
        window.minSize = new Vector2(420, 170);
        window.Show();
    }

    private void OnGUI()
    {
        xmlAsset = (TextAsset)EditorGUILayout.ObjectField("TextureAtlas XML", xmlAsset, typeof(TextAsset), false);
        alignment = (SpriteAlignment)EditorGUILayout.EnumPopup("Pivot", alignment);

        if (alignment == SpriteAlignment.Custom)
        {
            customPivot = EditorGUILayout.Vector2Field("Custom Pivot", customPivot);
        }

        GUILayout.FlexibleSpace();

        if (xmlAsset != null && GUILayout.Button("Slice"))
        {
            Slice();
            Close();
        }
    }

    private void Slice()
    {
        var texturePath = importer.assetPath;
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

        var doc = new XmlDocument();
        doc.LoadXml(xmlAsset.text);

        var nodes = doc.SelectNodes("TextureAtlas/SubTexture");
        var metas = new List<SpriteMetaData>();

        foreach (XmlNode node in nodes)
        {
            var name = node.Attributes["name"].Value;
            var x = int.Parse(node.Attributes["x"].Value);
            var y = int.Parse(node.Attributes["y"].Value);
            var w = int.Parse(node.Attributes["width"].Value);
            var h = int.Parse(node.Attributes["height"].Value);

            var rect = new Rect(x, texture.height - (y + h), w, h);

            metas.Add(new SpriteMetaData
            {
                name = Path.GetFileNameWithoutExtension(name),
                rect = rect,
                alignment = (int)alignment,
                pivot = alignment == SpriteAlignment.Custom ? customPivot : Vector2.zero
            });
        }

        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritesheet = metas.ToArray();

        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
    }
}