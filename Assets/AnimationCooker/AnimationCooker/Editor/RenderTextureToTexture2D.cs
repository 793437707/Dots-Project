// This class was taken from https://github.com/sugi-cho/Animation-Texture-Baker
// It has static functions that can be used to convert a RenderTexture to a Texture2D
// These functions are only used in debugging.
//--------------------------------------------------------------------------------------------------//

using UnityEngine;

public class RenderTextureToTexture2D : MonoBehaviour
{

    public static Texture2D Convert(RenderTexture rt, bool enableLinear)
    {
        TextureFormat format;
        switch (rt.format) {
            case RenderTextureFormat.ARGBFloat: format = TextureFormat.RGBAFloat; break;
            case RenderTextureFormat.ARGBHalf: format = TextureFormat.RGBAHalf; break;
            case RenderTextureFormat.ARGBInt: format = TextureFormat.RGBA32; break;
            case RenderTextureFormat.ARGB32: format = TextureFormat.ARGB32; break;
            default: format = TextureFormat.ARGB32; Debug.LogWarning("Unsuported RenderTextureFormat."); break;
        }
        return Convert(rt, format, enableLinear);
    }

    static Texture2D Convert(RenderTexture rt, TextureFormat format, bool enableLinear)
    {
        var tex2d = new Texture2D(rt.width, rt.height, format, false, enableLinear);
        var rect = Rect.MinMaxRect(0f, 0f, tex2d.width, tex2d.height);
        RenderTexture.active = rt;
        tex2d.ReadPixels(rect, 0, 0);
        RenderTexture.active = null;
        return tex2d;
    }
}
