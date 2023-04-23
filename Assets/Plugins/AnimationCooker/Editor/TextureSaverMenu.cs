// this function came from https://github.com/sugi-cho/Animation-Texture-Baker
// it contains a static function for saving a texture as a png and it shows up in the AnimationCooker menu.
// warning - if you attempt to save save a png from an float buffer, it will silently fail
// warning - if you attempt to save save an exr from a non-float buffer, it will silently fail
//--------------------------------------------------------------------------------------------------//

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

public class TextureSaverMenu

{
    [MenuItem("AnimationCooker/Convert Selected Texture To PNG")]
    public static void SaveSelectionAsPng()
    {
        var tex = (Texture2D)Selection.activeObject;
        if (tex == null) {
            EditorUtility.DisplayDialog("Convert Selected Texture To PNG", "You must have a texture selected first.", "OK");
			return;
		}
        var pngData = tex.EncodeToPNG();
        string path = Application.dataPath + "/tex.png";
        System.IO.File.WriteAllBytes(path, pngData);
        System.Diagnostics.Process.Start(@path);
    }

    [MenuItem("AnimationCooker/Convert Selected Texture To EXR")]
    public static void SaveSelectionAsExr()
    {
        var tex = (Texture2D)Selection.activeObject;
        if (tex == null) {
            EditorUtility.DisplayDialog("Convert Selected Texture To EXR", "You must have a texture selected first.", "OK");
            return;
        }
        var exrData = tex.EncodeToEXR(Texture2D.EXRFlags.None);
        string path = Application.dataPath + "/tex.exr";
        System.IO.File.WriteAllBytes(path, exrData);
        System.Diagnostics.Process.Start(@path);
    }
}

#endif