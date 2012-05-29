using UnityEditor;
using UnityEngine;

public class DistFieldFontGenerator : EditorWindow
{
    static int DistanceFieldScaleFactor = 4;
    static DistanceField.TextureChannel InputTextureChannel = DistanceField.TextureChannel.RED;
    static Object bmFontSrc;

    [MenuItem("BitMango/Font/Generate DistField Font")]
    static void ShowWindow()
    {
        EditorWindow.GetWindow( typeof(DistFieldFontGenerator) );
    }

    void OnGUI()
    {
        bmFontSrc = EditorGUILayout.ObjectField("BMFont Source", bmFontSrc, typeof(Object), false);
        DistanceFieldScaleFactor = EditorGUILayout.IntSlider("Scale Factor", DistanceFieldScaleFactor, 1, 8);
        DistanceField.SearchRadius = EditorGUILayout.IntSlider("Search Radius", DistanceField.SearchRadius, 1, 50);

        if(GUILayout.Button("Generate")){
            if (bmFontSrc == null) {
                EditorUtility.DisplayDialog("Error", "Select BMFont file first.", "Ok");
            }
            else{
                string path = AssetDatabase.GetAssetPath(bmFontSrc.GetInstanceID());
                if (path.ToLower().EndsWith(".fnt")) {
                    Generate(path);
                }
                else {
                    EditorUtility.DisplayDialog("Unknown File Extension", "Only .fnt files are supported.", "Ok");
                }
            }
        }
    }

    static void Generate(string path)
    {
        BMFont.IntFontInfo fntInfo = BMFont.ParseFromPath(path);
        if(fntInfo == null) return;
            
        //Process Texture
        string imagePath = System.IO.Path.GetDirectoryName(path) + "/" + fntInfo.texName;
        Texture2D inputTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(imagePath, typeof(Texture2D));

        //Make sure font texture is readable
        TextureImporter inputTextureImp = (TextureImporter)TextureImporter.GetAtPath(imagePath);
        inputTextureImp.textureType = TextureImporterType.Advanced;
        inputTextureImp.isReadable = true;
        inputTextureImp.maxTextureSize = 4096;
        AssetDatabase.ImportAsset(imagePath, ImportAssetOptions.ForceSynchronousImport);

        Texture2D distanceField = DistanceField.CreateDistanceFieldTexture(inputTexture, InputTextureChannel, inputTexture.width / DistanceFieldScaleFactor);

        //Save distance field as png
        byte[] pngData = distanceField.EncodeToPNG();
        string outputPath = imagePath.Substring(0, imagePath.LastIndexOf('.')) + "_dist.png";
        System.IO.File.WriteAllBytes(outputPath, pngData);
        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);

        //Set correct texture format
        TextureImporter texImp = (TextureImporter)TextureImporter.GetAtPath(outputPath);
        texImp.textureType = TextureImporterType.GUI;
        texImp.isReadable = true;
        texImp.textureFormat = TextureImporterFormat.Alpha8;
        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);

        string newTexName = fntInfo.texName.Substring(0, fntInfo.texName.LastIndexOf(".")) + "_dist.png";
        fntInfo.texName = newTexName;
        fntInfo.scaleW /= DistanceFieldScaleFactor;
        fntInfo.scaleH /= DistanceFieldScaleFactor;
        fntInfo.lineHeight /= DistanceFieldScaleFactor;

        foreach(BMFont.IntChar c in fntInfo.chars){
            c.x /= DistanceFieldScaleFactor;
            c.y /= DistanceFieldScaleFactor;
            c.width /= DistanceFieldScaleFactor;
            c.height /= DistanceFieldScaleFactor;
            c.xoffset /= DistanceFieldScaleFactor;
            c.yoffset /= DistanceFieldScaleFactor;
            c.xadvance /= DistanceFieldScaleFactor;
        }

        outputPath = path.Substring(0, path.LastIndexOf('.')) + "_dist.fnt";
        BMFont.SaveToXML(fntInfo, outputPath);
    }
}
