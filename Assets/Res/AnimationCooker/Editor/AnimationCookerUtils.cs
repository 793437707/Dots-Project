// This is a static class that contains a bunch of functions that can be used to bake vertex animations.
// It is broken into a few main functions:
//   1) cook - runs through the animations frame-by-frame and creates the textures for a prefab
//   2) save-cooked-files - saves textures, materials, prefabs, and generated scripts.
//   3) calculate possible frame rates - this is a function to compute the best possible frame rates for the possible texture sizes.
// If you change the format (for example, add a header byte or something), the increment CookerFormatVersionNumber
//--------------------------------------------------------------------------------------------------//

#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor; // used for AssetDatabase
using UnityEngine;
using Unity.Mathematics;


public struct MeshStats
{
    public long vertexCount;
    public int boneMeshCount;
}

public struct ClipStats
{
    public float smallestFps;
    public float totalClipLength;
}

public struct FrameStats
{
    public int frameCount;
    public long pointCount;
}

// a buffer of these structs is passed to the compute shader
// IMPORTANT - note that this must correspond EXACTLY with PixelInfo in VtxAnimTextureGen.compute.
// (the sizes must be the same for allocation purposes)
public struct PixelInfo
{
    public float4 position;
    public float4 normal;
    public float4 tangent;
}

// holds the result of a save-cooked-files operation
public struct SaveResult
{
    public string message;
    public string subFolderPath;
}

// Holds the complete list of all clips as well as options for each one
// such as an overridable name and a bit to enable/disable the clip during bake.
// This struct must be marked as serialiable because it resides in BakeOptions.
[System.Serializable]
public struct ClipOption
{
    public string name;
    public bool enable;
    public AnimationClip clip;

    public void SetEnable(bool e) { enable = e; }
    public void SetName(string str) { name = str; }
    public void SetClip(AnimationClip c) { clip = c; }
}

public enum VtxAnimTexType { Position, PositionAndNormal, PositionNormalAndTangent }

// Options that get passed to the bake function.
// It must be marked as serializable so it can be used as a serializable member of a GUI.
[System.Serializable]
public struct BakeOptions
{
    public VtxAnimTexType outputType;
    public TexSampleFormat format; // the format chosen by the user 

    // Tthis holds options and clip information for each clip.
    // Normally, i would have used a List<ClipOption>, but
    // the unity editor won't serialize a List<> after a domain reload
    // (like when you change code and go back to the editor)
    public ClipOption[] clipOpts;

    public bool enableBoneAdjust;
    public bool ignoreBoneMeshes;
    public bool enableResetPositionBeforeBake;
    public bool enableResetRotationBeforeBake;
    public bool enableResetScaleBeforeBake;
    public bool enableEnumDeclaration;
    public bool enableLogFile;

    public string MakeReport()
    {
        string ret = "";
        ret += enableEnumDeclaration ? "\nEnum Declaration: enabled" : "\nEnum Declaration: disabled";
        ret += enableResetRotationBeforeBake ? "\nReset Rotation Before Bake: enabled" : "\nReset Rotation Before Bake: disabled";
        ret += enableResetPositionBeforeBake ? "\nReset Position Before Bake: enabled" : "\nReset Position Before Bake: disabled";
        ret += enableResetScaleBeforeBake ? "\nReset Scale Before Bake: enabled" : "\nReset Scale Before Bake: disabled";
        ret += ignoreBoneMeshes ? "\nIgnore Bone Meshes: enabled" : "\nIgnore Bone Meshes: disabled";
        ret += enableBoneAdjust ? "\nBone Adjust: enabled" : "\nBone Adjust: disabled";
        ret += format.MakeReport(outputType);
        return ret;
    }

    public int CalculateEnabledClipCount()
    {
        int total = 0;
        for (int i = 0; i < clipOpts.Length; i++) {
            if (clipOpts[i].enable) { total++; }
        }
        return total;
    }
}

// holds the result of a bake operation
// contains any necessary variables that the save-baked-files function might need.
public struct BakeResult
{
    public string modelName;
    public string message;
    public RenderTexture positionRenderTex;
    public RenderTexture normalRenderTex;
    public RenderTexture tangentRenderTex;
    public List<AnimDbEntry> collection;
    public int actualSampleCount;
    public long actualPointCount;

    public SkinnedMeshRenderer skin;
    public Mesh fixedMesh;
    public GameObject originalprefab;
    public BakeOptions opts; // opts used during the bake
}

// struct used in calculating possible frame-rates
[System.Serializable]
public struct TexSampleFormat
{
    public int frameRate;
    public int height; // texture height
    public int width; // texture width

    public int CalculateBytes(VtxAnimTexType type)
    {
        int singleTexSize = height * width * 8;
        switch (type) {
            case VtxAnimTexType.PositionAndNormal: return singleTexSize * 2;
            case VtxAnimTexType.PositionNormalAndTangent: return singleTexSize * 3;
        }
        return singleTexSize; // default - position only
    }

    public string MakeReport(VtxAnimTexType type)
    {
        return $"\n{frameRate}fps, {width}x{height}, {(int)((float)CalculateBytes(type) * 0.001f)}KB";
    }
}

// keeps track of min and max position values
// (mainly used in packing/unpacking rgba values)
struct PointRange
{
    public float minPos;
    public float maxPos;
    public float minNml;
    public float maxNml;
    public float minTan;
    public float maxTan;
    public static PointRange Default => new PointRange { minPos = float.MaxValue, maxPos = float.MinValue, minNml = float.MaxValue, maxNml = float.MinValue, minTan = float.MaxValue, maxTan = float.MinValue };

    public void UpdateNml(float3 nml) { UpdateVal(nml, ref minNml, ref maxNml); }
    public void UpdateTan(float4 tan) { UpdateVal(tan.xyz, ref minTan, ref maxTan); }
    public void UpdatePos(float3 pos) { UpdateVal(pos, ref minPos, ref maxPos); }

    void UpdateVal(float3 val, ref float min, ref float max)
    {
        if (val.x < min) { min = val.x; }
        if (val.y < min) { min = val.y; }
        if (val.z < min) { min = val.z; }
        if (val.x > max) { max = val.x; }
        if (val.y > max) { max = val.y; }
        if (val.z > max) { max = val.z; }
    }
}

public struct BoneInfo
{
    public float scale;
    public Vector3 offset;
}

public static class AnimationCookerUtils
{
    const byte CookerFormatVersionNumber = 4;
    static readonly int[] Powers = { 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 16384 };

    static BoneInfo CalculateBoneInfo(Mesh mesh, Transform skinTransform, List<MeshFilter> boneMeshes, bool ignoreBoneMeshes, Mesh newMesh)
    {
        // find the boundaries for the mesh and any submeshes
        Bounds bounds = new Bounds();
        for (int j = 0; j < mesh.vertexCount; j++) {
            var point = skinTransform.TransformPoint(mesh.vertices[j]);
            if (j == 0) { bounds.center = point; }
            bounds.Encapsulate(point);
        }
        if (!ignoreBoneMeshes) {
            foreach (var filter in boneMeshes) {
                var boneMesh = filter.sharedMesh;
                for (int j = 0; j < boneMesh.vertexCount; j++) {
                    var point = filter.transform.TransformPoint(boneMesh.vertices[j]);
                    bounds.Encapsulate(point);
                }
            }
        }
        BoneInfo boneInf;
        boneInf.scale = newMesh.bounds.size.y / bounds.size.y;
        boneInf.offset.y = 0 - bounds.min.y;
        boneInf.offset.x = 0;
        boneInf.offset.z = 0;
        return boneInf;
    }

    public static BakeResult Bake(GameObject prefab, ComputeShader computeShader, in BakeOptions opts)
    {
        DisplayProgress("Cooking up some textures. Ommmm nom nom nom.", "0 of 0 clips finished.", 0);

        BakeResult result = default;
        int enabledClipCount = opts.CalculateEnabledClipCount();

        var skin = FindSkinnedMeshRenderer(prefab);
        if (skin == null) { result.message = "Skin not found."; return result; }
        List<MeshFilter> boneMeshes = FindMeshesInBones(prefab);

        // save the old position and rotation because we don't want to change the original.
        // Note that because Quaternion and Vector3 are structs, we can use "=" notation to copy.
        Quaternion oldSkinRotation = skin.transform.rotation;
        Vector3 oldSkinPosition = skin.transform.position;
        Vector3 oldSkinScale = skin.transform.localScale;

        Vector3 invScale;
        invScale.x = 1f / prefab.transform.localScale.x;
        invScale.y = 1f / prefab.transform.localScale.y;
        invScale.z = 1f / prefab.transform.localScale.z;

        // set the transform pos, rot, and scale of the skin transform to the origin
        Quaternion rotation = opts.enableResetRotationBeforeBake ? prefab.transform.rotation : oldSkinRotation;
        Vector3 position = opts.enableResetPositionBeforeBake ? prefab.transform.position : oldSkinPosition;
        Vector3 scale = opts.enableResetScaleBeforeBake ? invScale : oldSkinScale;

        skin.transform.SetPositionAndRotation(position, rotation);
        skin.transform.localScale = scale;

        // this will create a brand new mesh, but with the points transformed according
        Mesh adjustedMesh = CopyAndAdjustMesh(skin.sharedMesh, boneMeshes, skin.transform, opts);
        //Mesh adjustedMesh = CopyAndAdjustMesh(skin.sharedMesh, boneMeshes, skin.transform, opts, prefab.transform);

        // now reset the transform for sure (no matter what is checked) before filling the pixel array.
        skin.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        skin.transform.localScale = invScale;

        // preallocate the pixel array (it's faster this way)
        // we must make a prediction in order to know the necessary sample count.
        MeshStats meshStats = CalculateMeshStats(prefab, opts.ignoreBoneMeshes);
        FrameStats frameStats = CalculateFrameStats(opts.clipOpts, opts.format, meshStats.vertexCount);

        // now that the predicted frame count is known, the pixels array can be allocated
        // width is added to include the header line - the first line in the texture (bottom line in opengl)
        PixelInfo[] pixels = new PixelInfo[frameStats.pointCount];

        // keep track of the current position in vertexInfos.
        int pixelIndex = opts.format.width; // skip over the first row - it will be filled at the end.
        AnimDbEntry clipItem = default;
        byte clipIndex = 0;
        float totalLength = 0f;
        int totalSampleCount = 0; // accumulates a total frame count
        clipItem.interval = 1f / opts.format.frameRate;
        result.collection = new List<AnimDbEntry>(); // accumulates info about each clip
        PointRange vertStats = PointRange.Default;
        Mesh bakedMesh = new Mesh(); // holds the result of the baked mesh

        bool useNormal = (opts.outputType == VtxAnimTexType.PositionAndNormal) || (opts.outputType == VtxAnimTexType.PositionNormalAndTangent);
        bool useTangent = opts.outputType == VtxAnimTexType.PositionNormalAndTangent;

        BoneInfo boneInfo = new BoneInfo { offset = Vector3.zero, scale = 0 };
        bool needsBoneAdjustCalc = false;

        foreach (ClipOption clipOpt in opts.clipOpts) {
            if (clipOpt.enable) {
                DisplayProgress("Cooking up some textures. Ommmm nom nom nom.", $"{clipIndex} of {enabledClipCount} clips finished.", ((float)clipIndex) / ((float)enabledClipCount));

                // clip frame count is length * frameRate * texStats.sampleMultiplier
                short clipFrameCount = (short)Mathf.FloorToInt(clipOpt.clip.length * opts.format.frameRate);

                // for every sampled frame in this clip
                for (int f = 0; f < clipFrameCount; f++) {
                    clipOpt.clip.SampleAnimation(prefab.gameObject, clipItem.interval * f);
                    skin.BakeMesh(bakedMesh); // result is store in mesh

                    // only calculate bone info if it hasn't been done
                    if (opts.enableBoneAdjust && needsBoneAdjustCalc) {
                        boneInfo = CalculateBoneInfo(bakedMesh, skin.transform, boneMeshes, opts.ignoreBoneMeshes, adjustedMesh);
                        UnityEngine.Debug.Log($"adjusting bones");
                    }

                    for (int i = 0; i < bakedMesh.vertexCount; i++) {
                        AddPixel(pixels, ref vertStats, bakedMesh, skin.transform, pixelIndex, i, useNormal, useTangent, boneInfo, opts.enableBoneAdjust);
                        pixelIndex++;
                    }

                    if (!opts.ignoreBoneMeshes) {
                        foreach (var filter in boneMeshes) {
                            for (int i = filter.sharedMesh.vertexCount; i < filter.sharedMesh.vertexCount; i++) {
                                AddPixel(pixels, ref vertStats, filter.sharedMesh, filter.transform, pixelIndex, i, useNormal, useTangent, boneInfo, opts.enableBoneAdjust);
                                pixelIndex++;
                            }
                        }
                    }
                }

                // fill clip item
                clipItem.frameCount = clipFrameCount;
                clipItem.clipName = clipOpt.name;
                clipItem.modelName = prefab.name;
                clipItem.beginFrame = (short)totalSampleCount;
                clipItem.endFrame = (short)(clipItem.beginFrame + clipFrameCount - 1);
                clipItem.clipIndex = clipIndex;
                totalSampleCount += clipFrameCount;
                float clipLength = clipFrameCount * clipItem.interval;
                totalLength += clipLength;

                // fetch the model index
                AnimationDatabase db = AnimationDatabase.GetDb();
                int modelIndex = db.GetModelIndex(prefab.name);
                if (modelIndex < 0) { modelIndex = db.GetModelCount(); }
                clipItem.modelIndex = (byte)modelIndex;

                if (clipItem.clipIndex != 0) { result.message += "\n"; }
                result.message += string.Format("  {0} {1} : {2:0.####}s {3}f [{4}..{5}]", clipIndex, clipOpt.name, clipLength, clipFrameCount, clipItem.beginFrame, clipItem.endFrame);
                result.collection.Add(clipItem);

                clipIndex++;
            } // clip is enabled
        } // for each clip

        // no that we're done with setting pixels and building the mesh, we no longer need modified transforms.
        // we must set these values back to their original values because they belong to the prefab that was passed in.
        skin.transform.SetPositionAndRotation(oldSkinPosition, oldSkinRotation);
        skin.transform.localScale = oldSkinScale;

        result.message += $"\nExpected frame count: {frameStats.frameCount}, Actual frame count: {totalSampleCount}";
        result.message += $"\nExpected point count: {frameStats.pointCount}, Actual point count: {pixels.Length}";

        float actualHeight = Mathf.Ceil(pixels.Length / opts.format.width) + 1;
        result.message += $"\nFill ratio: {(actualHeight / opts.format.height) * 100f}%.";

        // The above loop fills the pixel values, but the values are unencoded.
        // Because the values are being stored in an RGBA texture, we need to encode each vertex position to maximize precision.
        // This can't be performed in the loop above because we need to know the min and max values, (which were being calculated above)
        // This could be moved to the compute shader to make it run faster, but for now it's easiest to just do it right here.
        // Starting at format.width will skip the first line.
        for (int i = opts.format.width; i < pixels.Length; i++) {
            pixels[i].position = PackingUtils.PackThree10BitFloatsToARGB(pixels[i].position.xyz, vertStats.minPos, vertStats.maxPos);
            pixels[i].normal = PackingUtils.PackThree10BitFloatsToARGB(pixels[i].normal.xyz, vertStats.minNml, vertStats.maxNml);
            pixels[i].tangent = PackingUtils.PackTangentToARGB(pixels[i].tangent, vertStats.minTan, vertStats.maxTan);
        }

        // fill values in the top row
        FillHeaderLine(pixels, result.collection, opts.format, vertStats, (uint)adjustedMesh.vertexCount);

        // append total clip info
        result.message += "\n" + enabledClipCount + " clips" + ", " + string.Format("{0:0.####}fps", opts.format.frameRate) + ", " + string.Format("{0:0.####}s", totalLength) + ", " + totalSampleCount + "f";

        // It is VERY important to set the color space to linear! I wasted 16+ hours trying to figure 
        // out why the values I was encoding were not decoding properly (thanks bgolus!)
        RenderTexture positionRenderTex = new RenderTexture(opts.format.width, opts.format.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        SetupRenderTexture(positionRenderTex);

        RenderTexture normalRenderTex = null, tangentRenderTex = null;
        if (useNormal) {
            normalRenderTex = new RenderTexture(opts.format.width, opts.format.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            SetupRenderTexture(normalRenderTex);
        }
        if (useTangent) {
            tangentRenderTex = new RenderTexture(opts.format.width, opts.format.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            SetupRenderTexture(tangentRenderTex);
        }

        // setup the compute buffer and run it (its task is to write all the vertexes to the texture)
        ComputeBuffer buffer = new ComputeBuffer(pixels.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PixelInfo)));
        buffer.SetData(pixels);
        int kernel = computeShader.FindKernel("CSMain");
        computeShader.GetKernelThreadGroupSizes(kernel, out uint x, out uint y, out uint z);
        computeShader.SetInt("TexWidth", (int)opts.format.width);
        computeShader.SetBuffer(kernel, "Info", buffer);
        computeShader.SetTexture(kernel, "OutPosition", positionRenderTex);
        if (normalRenderTex != null) { computeShader.SetTexture(kernel, "OutNormal", normalRenderTex); }
        if (tangentRenderTex != null) { computeShader.SetTexture(kernel, "OutTangent", tangentRenderTex); }
        // when dispatching, we don't need to cover the whole texture - just the pixels that will be written to.
        // the vertexes will get blasted to pixels in the textures from left to right, bottom to top
        int height = Mathf.CeilToInt(pixels.Length / opts.format.width) + 1; // add one for the extra header line
        computeShader.Dispatch(kernel, opts.format.width / (int)x + 1, height + 1 / (int)y + 1, 1);
        buffer.Release();

        // store the rest of the result
        result.skin = skin;
        result.modelName = prefab.name;
        result.positionRenderTex = positionRenderTex;
        result.normalRenderTex = normalRenderTex;
        result.tangentRenderTex = tangentRenderTex;
        result.fixedMesh = adjustedMesh;
        result.actualSampleCount = totalSampleCount;
        result.actualPointCount = pixels.Length;
        result.opts = opts;
        result.originalprefab = prefab;

        ClearProgress();

        return result;
    }

    // This function can only be called from the Unity Editor
    // It does the following:
    //   - Creates a .posTex.asset file for the the position texture
    //   - Creates a .material.asset file using the specified shader
    //   - Creates a .mesh.asset file with the new mesh
    //   - Creates a .prefab file that uses the new material and the new mesh
    public static SaveResult SaveBakedFiles(BakeResult bakeResult, string outBakeFolder, string outScriptFolder, Shader playShader, Unity.Scenes.SubScene subScene)
    {
        SaveResult result = default;

        string prefabName = bakeResult.originalprefab.name;

        string subFolderPath = Path.Combine("Assets/", outBakeFolder, prefabName);
        string outScriptPath = Path.Combine("Assets/", outScriptFolder);
        // create the child folder and any parent folders that don't exist already.
        if (Directory.Exists(subFolderPath)) { Directory.Delete(subFolderPath, true); }
        Directory.CreateDirectory(subFolderPath);

        // Create the material file
        Material material = new Material(playShader);

        // This doesn't work the way you might expect. for some reason, if you call this copy function
        // and then set the material parameters (_PosMap, _NmlMap, _CurTime, etc) immediately after,
        // and then save the material to disk, they will be ignored. I don't know why.
        // The way to make it work was to call copy, save the material to disk, and then set the
        // custom parameters (_PosMap, etc).
        material.CopyPropertiesFromMaterial(bakeResult.skin.sharedMaterial);

        // Make sure to use linear instead of sRGB!!!
        // OMG. I was having so many issues with the first and last frames.
        // I wasted over 12 hours trying to figure out what was wrong and it turned
        // out that all I needed was to change filter mode from bilinear to point in the texture properties.
        Texture2D posTex = RenderTextureToTexture2D.Convert(bakeResult.positionRenderTex, true);
        posTex.filterMode = FilterMode.Point;
        // disable wrap-mode... i don't think this matters much, but it can't hurt
        posTex.wrapMode = TextureWrapMode.Clamp;
        posTex.anisoLevel = 0;
        Graphics.CopyTexture(bakeResult.positionRenderTex, posTex);
        AssetDatabase.CreateAsset(posTex, Path.Combine(subFolderPath, prefabName + ".posTex.asset"));

        Texture2D nmlTex = null;
        Texture2D tanTex = null;

        if (bakeResult.normalRenderTex != null) {
            nmlTex = RenderTextureToTexture2D.Convert(bakeResult.normalRenderTex, true);
            nmlTex.filterMode = FilterMode.Point;
            nmlTex.wrapMode = TextureWrapMode.Clamp;
            nmlTex.anisoLevel = 0;
            Graphics.CopyTexture(bakeResult.normalRenderTex, nmlTex);
            AssetDatabase.CreateAsset(nmlTex, Path.Combine(subFolderPath, prefabName + ".nmlTex.asset"));
        }
        if (bakeResult.tangentRenderTex != null) {
            tanTex = RenderTextureToTexture2D.Convert(bakeResult.tangentRenderTex, true);
            tanTex.filterMode = FilterMode.Point;
            tanTex.wrapMode = TextureWrapMode.Clamp;
            tanTex.anisoLevel = 0;
            Graphics.CopyTexture(bakeResult.tangentRenderTex, tanTex);
            AssetDatabase.CreateAsset(tanTex, Path.Combine(subFolderPath, prefabName + ".tanTex.asset"));
        }

        // create the fixed mesh
        AssetDatabase.CreateAsset(bakeResult.fixedMesh, Path.Combine(subFolderPath, prefabName + ".mesh.asset"));

        //We must save textures and the mesh before creating the material or else the material won't be able to point at them.
        AssetDatabase.SaveAssets();
      
        // Finish setting up the material and save it
        material.name = prefabName + ".material";
        AssetDatabase.CreateAsset(material, Path.Combine(subFolderPath, material.name + ".asset"));
        //AssetDatabase.SaveAssets(); // save again for the material.

        // make an alias for the animation clip database
        AnimationDatabase db = AnimationDatabase.GetDb();

        // add new clips to the current animation clip database and then save it to file
        Dictionary<string, AnimDbEntry> clips = new Dictionary<string, AnimDbEntry>();
        for (int i = 0; i < bakeResult.collection.Count; i++) {
            AnimDbEntry item = bakeResult.collection[i];
            clips.Add(item.clipName.ToString(), item);
        }
        db.SetModelClips(bakeResult.modelName, clips);
        string animFileName = "AnimDb.cs";
        AnimationDbUtils.SaveDatabase(db, outScriptPath, animFileName, bakeResult.opts.enableEnumDeclaration);
        result.message += "\nDatabase saved to: " + outScriptPath + "/" + animFileName;

        GameObject oldBakedPrefab = GameObject.Find(prefabName + "_Baked");
        if (oldBakedPrefab != null) { GameObject.DestroyImmediate(oldBakedPrefab); }

        // create a temporary gameobject and set its material to the material that we just created
        GameObject tempGameObj = new GameObject(prefabName + "_Baked");
        tempGameObj.AddComponent<MeshRenderer>().sharedMaterial = material;
        tempGameObj.AddComponent<MeshFilter>().sharedMesh = bakeResult.fixedMesh;
        //tempGameObj.AddComponent<Unity.Entities.ConvertToEntity>();
        AnimationClipAuthoring authoring = tempGameObj.AddComponent<AnimationClipAuthoring>();
        authoring.SetAnimationModel(prefabName);

        if (db.FindClipThatContains(prefabName, "Idle", out AnimDbEntry idleClip)) {
            authoring.SetDefaultAnimation(idleClip.clipName.ToString());
        }

        // save that temporary gameobject to a new prefab on disk
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempGameObj, Path.Combine(subFolderPath, tempGameObj.name + ".prefab"));

        // if a subscene is specified, instantiate the new prefab and add it to the subscene
        if (subScene != null) {
            if (subScene.IsLoaded) {
                GameObject instantiatedGameObj = PrefabUtility.InstantiatePrefab(savedPrefab) as GameObject;
                // TODO - The subscene needs to be opened if it is closed before adding an object to the subscene,
                // but i wasted 4 hours trying to figure out how to do it and there doesn't seem to be a function to open it.

                //UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instantiatedGameObj, subScene.EditingScene);
                //Unity.Entities.World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<
                //UnityEditor.SceneManagement.EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(subScene.gameObject));
                //UnityEditor.SceneManagement.EditorSceneManager.OpenScene(subScene.SceneAsset);
                //UnityEngine.Debug.Log($"{subScene.EditableScenePath}");

                UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(instantiatedGameObj, subScene.EditingScene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(subScene.EditingScene);
            } else {
                result.message += $"\n***Couldn't put baked obj in {subScene.name} (it's not opened).";
                UnityEngine.Debug.Log($"\n***Couldn't put baked obj in {subScene.name} (it's not opened).");
            }
        }
        // delete the temporary gameobject.
        GameObject.DestroyImmediate(tempGameObj);
        result.message += "\nOutput saved to: " + subFolderPath;

        material.SetFloat("_CurTime", 0f);
        material.SetFloat("_ClipIdx", 0f);
        material.SetTexture("_PosMap", posTex);
        Material prefabMat = savedPrefab.GetComponent<MeshRenderer>().sharedMaterial;
        if (nmlTex != null) {
            prefabMat.SetTexture("_NmlMap", nmlTex);
            prefabMat.EnableKeyword("USE_VA_NORMAL_MAP");
            prefabMat.SetFloat("_UseNormalMap", 1f);
        }
        if (tanTex != null) {
            prefabMat.SetTexture("_TanMap", tanTex);
            prefabMat.EnableKeyword("USE_VA_TANGENT_MAP");
            prefabMat.SetFloat("_UseTangentMap", 1f);
        }

        // This is the second save - meant to save the material.
        // This needs to happen here (below the material setting functions above),
        // otherwise, the saved material won't have the textures and parameters set correctly.
        AssetDatabase.SaveAssets();

        string rootBoneName = $"\nRoot Bone name: {bakeResult.skin.rootBone.name}";

        string optsString = bakeResult.opts.MakeReport() + "\n";
        if (bakeResult.opts.enableLogFile) {
            File.WriteAllText(Path.Combine(subFolderPath, prefabName + ".log"), "Shader: " + playShader.name + result.message + rootBoneName + optsString + bakeResult.message);
        }

        // Warning - this function is asynchronous!
        // The class that called SaveCookedFiles() will have to do some voodoo
        // to determine when the import finished if it wants to do something with
        // the database file that was just created.
        if (bakeResult.opts.enableEnumDeclaration) { AssetDatabase.ImportAsset(Path.Combine(outScriptPath, "AnimDb.cs")); }
        AssetDatabase.Refresh();

        result.subFolderPath = subFolderPath;
        return result;
    }

    public static SkinnedMeshRenderer FindSkinnedMeshRenderer(GameObject go)
    {
        SkinnedMeshRenderer ret = go.GetComponent<SkinnedMeshRenderer>();
        if (ret == null) { ret = go.GetComponentInChildren<SkinnedMeshRenderer>(); }
        return ret;
    }

    public static List<MeshFilter> FindMeshesInBones(GameObject prefab)
    {
        List<MeshFilter> filters = new List<MeshFilter>();
        RecursivelyFindMeshesInBones(filters, prefab.transform);
        return filters;
    }

    public static ClipStats CalculateSelectedClipStats(ClipOption[] clipOpts)
    {
        ClipStats stats;
        stats.smallestFps = int.MaxValue;
        stats.totalClipLength = 0f;
        for (int i = 0; i < clipOpts.Length; i++) {
            ClipOption clipOpt = clipOpts[i];
            if (clipOpt.enable) {
                if (clipOpt.clip == null) { UnityEngine.Debug.Log($"clip {i} is null"); }
                if (clipOpt.clip.frameRate < stats.smallestFps) { stats.smallestFps = clipOpt.clip.frameRate; }
                stats.totalClipLength += clipOpt.clip.length;
            }
        }
        return stats;
    }

    public static MeshStats CalculateMeshStats(GameObject prefab, bool ignoreBoneMeshes)
    {
        return CalculateMeshStats(FindSkinnedMeshRenderer(prefab), FindMeshesInBones(prefab), ignoreBoneMeshes);
    }

    public static MeshStats CalculateMeshStats(SkinnedMeshRenderer skin, List<MeshFilter> boneMeshes, bool ignoreBoneMeshes)
    {
        MeshStats stats;
        stats.vertexCount = 0;
        stats.boneMeshCount = 0;
        stats.vertexCount = (skin != null) ? skin.sharedMesh.vertexCount : 0;
        if (!ignoreBoneMeshes) {
            for (int i = 0; i < boneMeshes.Count; i++) {
                stats.vertexCount += boneMeshes[i].sharedMesh.vertexCount;
            }
            stats.boneMeshCount = boneMeshes.Count;
        }
        return stats;
    }

    // using the given format and clips, calculate the frame stats
    // (this can be a prediction of how many frames or points will be needed)
    public static FrameStats CalculateFrameStats(ClipOption[] clipOpts, TexSampleFormat fmt, long vertexCount)
    {
        FrameStats stats;
        stats.frameCount = 0;
        for (int i = 0; i < clipOpts.Length; i++) {
            ClipOption clipOpt = clipOpts[i];
            if (clipOpt.enable) {
                stats.frameCount += (short)Mathf.FloorToInt(clipOpt.clip.length * fmt.frameRate);
            }
        }
        stats.pointCount = (vertexCount * stats.frameCount) + fmt.width;
        return stats;
    }

    public static string MakePredictionString(BakeOptions opts, MeshStats meshStats, ClipStats clipStats, FrameStats frameStats)
    {
        if (opts.format.width <= 0) { return ""; }
        string ret = $"{opts.CalculateEnabledClipCount()} clips, {meshStats.vertexCount} vertices, {(int)((float)opts.format.CalculateBytes(opts.outputType) * 0.001)} KB";
        ret += "\nSampled: " + string.Format("{0:0.####}", clipStats.totalClipLength) + "s, " + frameStats.frameCount + "f at " + opts.format.frameRate + "fps";
        ret += "\nTexture resolution: " + opts.format.width + "x" + opts.format.height;
        ret += "\nBone mesh count: " + meshStats.boneMeshCount;

        float expectedHeight = Mathf.Ceil(meshStats.vertexCount * frameStats.frameCount / opts.format.width) + 1;
        float expectedFillPercent = (expectedHeight / opts.format.height) * 100f;
        ret += $"\nEstimated fill ratio: {expectedFillPercent}%.";
        return ret;
    }

    // function to fetch all the clips from m_prefab
    // it will attempt to fetch them from either an Animator or an Animation
    public static AnimationClip[] GetClips(GameObject prefab, ref string msg)
    {
        // first attempt to fetch an animator
        Animator animator = FindAnimator(prefab);
        string animatorStr = "";
        if (animator == null) {
            animatorStr = "An animator was not found.";
        } else if (animator.runtimeAnimatorController == null){
            animatorStr = "A runtime animator controller was not found";
        } else if (animator.runtimeAnimatorController.animationClips.Length <= 0) {
            animatorStr = "A runtime animator controller was found but it has no animation clips.";
        } else {
            return animator.runtimeAnimatorController.animationClips;
        }

        // if there is no animator, try to fetch an animation
        Animation animation = FindAnimation(prefab);
        if (animation == null) {
            msg += "There is no Animation on the prefab or its children. " + animatorStr;
            return null;
        }
        if (animation.GetClipCount() <= 0) {
            msg += "Animation was found, but it contains no clips. " + animatorStr;
            return null;
        }
        int clipCount = animation.GetClipCount();
        AnimationClip[] clips = new AnimationClip[animation.GetClipCount()];
        int i = 0;
        foreach (AnimationState state in animation) {
            if (state.clip != null) {
                clips[i] = state.clip;
                i++;
            }
        }
        if (i != clipCount) {
            msg += $"Animation found, but {clipCount - i} out of {clipCount} clips are invalid. " + animatorStr;
            return null;
        }
        return clips;
    }

    // smallestFps --> the smallest out of any of the clips (can be obtained via the prediction)
    public static List<TexSampleFormat> DiscoverPossibleFormats(long vertexCount, float totalClipLength, float smallestFps)
    {
        List<TexSampleFormat> formats = new List<TexSampleFormat>(); // the return value

        // note that there might actually be less points than what is calculated below.
        // when we actually do the sampling, we will need to take an integer number of samples,
        // and the number of seconds for each clip is a decimal value,
        // so we could have clip lengths that look something like:
        // 5.0 + 2.5 + 3.2 + 1.9 + 3.4 + 2.3 + 4.8 --> 23.1 seconds.
        // If the desired frame rate is 7fps, then that makes 23.1s * 7fps --> 161.7
        // 7fps has an interval of 1/7.
        // If we calculate the number of times we need to loop and take a sample for each frame,
        // we get a table like this, where the last two numbers use floor() and ceiling():
        // 5.0s * 7fps --> 35.0 --> 35 | 35
        // 2.5s * 7fps --> 17.5 --> 17 | 18
        // 3.2s * 7fps --> 22.4 --> 22 | 23
        // 1.9s * 7fps --> 13.3 --> 13 | 14
        // 3.4s * 7fps --> 23.8 --> 23 | 24
        // 2.3s * 7fps --> 16.1 --> 16 | 17
        // 4.8s * 7fps --> 33.6 --> 33 | 34
        // floor() --> 159 samples | ceiling() --> 165 samples
        // calculated --> 161.7 (from 23.1s * 7)
        // So its apparent that we can't know the exact number of samples that will result without knowing the desired fps
        // One way to handle this would be to loop for all fps (1..60) and calculate the sample counts, but that would get messy.
        // The current way it's being hancled is by using the floor of the calculated number (161) as a worst case guess,
        // and using floor() during the actual sampling. The estimate will likely be over by a small amount, but it should never be under.

        // this value does not include header because it's accounted for in availablePointCount below
        float worstCasePointCount = Mathf.FloorToInt(totalClipLength * vertexCount);

        // test each power for a width
        for (int i = 0; i < Powers.Length; i++) {
            int width = Powers[i];
            for (int j = 0; j < Powers.Length; j++) {
                int height = Powers[j];

                // only look at situations where height is smaller than or the same size as width
                // (we don't care about textures like 128x8192)
                if (height <= width) {

                    // example with slots needed at 36532.5 (7.5 seconds * 4871 vertexes)
                    // ((128 * 128) - 128) / 36532.5 --> 0.4449 [0] (ignore)
                    // ((256 * 128) - 256) / 36532.5 --> 0.8899 [0] (ignore)
                    // ((256 * 256) - 256) / 36532.5 --> 1.7869 [1]
                    // ((512 * 128) - 512) / 36532.5 --> 1.7798 [1]
                    // ((512 * 256) - 512) / 36532.5 --> 3.5738 [3] ** add me
                    // ((512 * 512) - 512) / 36532.5 --> 7.1616 [7] ** add me
                    // ((1024 * 128) - 1024) / 36532.5 --> 3.5597 [3]
                    // ((1024 * 256) - 1024) / 36532.5 --> 7.1476 [7]
                    // ((1024 * 512) - 1024) / 36532.5 --> 14.3232 [14] ** add me
                    // ((1024 * 1024) - 1024) / 36532.5 --> 28.6745 [28] ** add me
                    // ((2048 * 128) - 2048) / 36532.5 --> 7.1195 [7]
                    // ((2048 * 256) - 2048) / 36532.5 --> 14.2952 [14]
                    // ((2048 * 512) - 2048) / 36532.5 --> 28.6464 [28]
                    // ((2048 * 1024) - 2048) / 36532.5 --> 57.3490 [57] ** add me
                    // ((2048 * 2048) - 2048) / 36532.5 --> 114.7541 [114] (clamp to 60) ** add me
                    // exit --> no need to go past our max frame rate (everything after will just be repeat)

                    // the max number of data points we can possibly hold in this texture
                    int availablePointCount = (width * height) - width; // subtracting width leaves a top row for the header.

                    // calculate the highest frame rate that could possibly fit into the texture
                    int maxRate = Mathf.FloorToInt(availablePointCount / worstCasePointCount);

                    // ignore frame rates less than zero
                    if (maxRate > 0) {
                        // clamp the max frame rate to the highest value that the clips will handle
                        bool maxReached = false;
                        if (maxRate >= smallestFps) {
                            maxRate = (int)smallestFps; // clamp it
                            maxReached = true;
                        }

                        // only add the rate if it doesn't already exist
                        // in theory a hash set would speed this up, but there are so few values usually that it's probably not worth the hassle.
                        if (!RateExists(maxRate, formats)) {
                            formats.Add(new TexSampleFormat { width = width, height = height, frameRate = maxRate });
                        }

                        // if the max frame rate has been reached, then we're done - nothing after this interests us
                        if (maxReached) { return formats;}
                    }
                }
            }
        }
        return formats;
    }


    //#############################################################################################################################
    //############################################ PRIVATE FUNCTIONS BELOW ########################################################
    //#############################################################################################################################

    static void AddPixel(PixelInfo[] pixels, ref PointRange vertStats, Mesh mesh, Transform xform, int pixIdx, int vertIdx, bool useNormal, bool useTangent, BoneInfo boneInfo, bool enableBoneAdjust)
    {
        Vector3 pos;
        if (enableBoneAdjust) {
            pos = (xform.TransformPoint(mesh.vertices[vertIdx]) + boneInfo.offset);
        } else {
            pos = xform.TransformPoint(mesh.vertices[vertIdx]);
        }

        // note - the position can't be encoded here because here we are calculating vert stats
        pixels[pixIdx].position = new float4(pos.x, pos.y, pos.z, 0f);
        vertStats.UpdatePos(pos);

        // note - the normal can't be encoded here because here we are calculating vert stats
        if (useNormal) {
            Vector3 nml = mesh.normals[vertIdx];
            pixels[pixIdx].normal = new float4(nml.x, nml.y, nml.z, 0f);
            vertStats.UpdateNml(nml);
        }
        if (useTangent) {
            Vector4 tan = mesh.tangents[vertIdx];
            pixels[pixIdx].tangent = tan;
            vertStats.UpdateTan(tan);
        }
    }

    // recursive function to find all MeshFilters
    static void RecursivelyFindMeshesInBones(List<MeshFilter> filters, Transform bone)
    {
        foreach (Transform child in bone) { RecursivelyFindMeshesInBones(filters, child); }
        var filter = bone.GetComponent<MeshFilter>();
        if (filter != null) { filters.Add(filter); }
    }

    static bool RateExists(int rate, List<TexSampleFormat> formats)
    {
        for (int r = 0; r < formats.Count; r++) {
            if (formats[r].frameRate == rate) { return true; }
        }
        return false;
    }


    // The very first line is the header row that holds all fixed information and any info about each clip.
    // This will allow the shader to quickly fetch the begin/end frame and the user will only need to set the clip index.
    // The index of the clip will correspond to x values in the texture plus an offset.
    // I'm going to assume that there will always be more vertices than animation clips, so we will only need one line.
    // Each position vertex is an either ARGBHalf (64 bit), or ARGB32 (32 bit) - which is a user choice.
    // The normals texture will also have a header line, but it will be blank/unused so that the normals texture won't be a requirement.
    // The pixels are arranged as follows:
    //   Pixel 0: Version number, Frame rate, unused, width-pow2 (4x8bit)
    //   Pixel 1: bounding-box min/max (2x16bit)
    //   Pixel 2: the vertex count (1x32bit)
    //   Pixel 3..clip-count: begin/end frame for each clip (2x16bit)
    //   Remaining Pixels: unfilled
    static void FillHeaderLine(PixelInfo[] points, List<AnimDbEntry> collection, TexSampleFormat fmt, PointRange vertStats, uint vertexCount)
    {
        int pointIndex = 0; // holds the current index in vertexInfos.

        // the first pixel ALWAYS contains the version number - that way we can make changes to the header and detect the version number
        // The first pixel looks like:
        //   [x (r): version number][y (g): frame rate][z (b): enable float texture][w (a): width-pow2]
        points[pointIndex].position = PackingUtils.PackFourBytesToRGBA(CookerFormatVersionNumber, (byte)fmt.frameRate, 0, (byte)Mathf.Log(fmt.width, 2f));
        pointIndex++;

        // put the position range into the second pixel. x will be min, y will be max.
        points[pointIndex].position = PackingUtils.PackTwo16bitFloatsToRGBA(vertStats.minPos, vertStats.maxPos);
        pointIndex++;

        // put the normal range into the third pixel. x will be min, y will be max.
        points[pointIndex].position = PackingUtils.PackTwo16bitFloatsToRGBA(vertStats.minNml, vertStats.maxNml);
        pointIndex++;

        // put the tangent range into the third pixel. x will be min, y will be max.
        points[pointIndex].position = PackingUtils.PackTwo16bitFloatsToRGBA(vertStats.minTan, vertStats.maxTan);
        pointIndex++;

        // set the fifth pixel to the number of vertex points (total for skinned mesh + all bone meshes)
        points[pointIndex].position = PackingUtils.PackUintToRGBA((uint)(vertexCount));
        pointIndex++;

        // add the clip begin/end frame values - one entry per clip
        // todo: technically we could run out of room if there were tons of clips and/or the width was really skinny,
        // but min width is 128 and i would be very surprised to find a model with 125 animation clips.
        foreach (AnimDbEntry clip in collection) {
            PixelInfo pix = default;
            pix.position = PackingUtils.PackTwo16bitFloatsToRGBA(clip.beginFrame, clip.endFrame);
            points[pointIndex] = pix;
            pointIndex++;
        }

        // we don't have to do anything with the remaining vertices... 
        // they're already allocated and will default to all zeros.
    }

    //static Mesh CopyAndAdjustMesh(Mesh srcMesh, List<MeshFilter> boneMeshes, Transform xForm, in BakeOptions opts, Transform goXform)
    static Mesh CopyAndAdjustMesh(Mesh srcMesh, List<MeshFilter> boneMeshes, Transform xForm, in BakeOptions opts)
    {
        List<Vector3> vertices = new List<Vector3>(srcMesh.vertexCount);
        // copy the vertexes, transforming each one from local to world space during the process
        foreach (var vertex in srcMesh.vertices) {
            vertices.Add(xForm.TransformPoint(vertex));
        }

        // copy values into a brand new mesh.
        // note that using ToArray() forces a deep copy
        Mesh newMesh = new Mesh();
        newMesh.subMeshCount = srcMesh.subMeshCount;
        newMesh.SetVertices(vertices);
        for (int i = 0; i < srcMesh.subMeshCount; i++) { newMesh.SetTriangles(srcMesh.GetTriangles(i).ToArray(), i); }
        int offset = vertices.Count;
        newMesh.uv = srcMesh.uv.ToArray();
        newMesh.normals = srcMesh.normals.ToArray();
        newMesh.tangents = srcMesh.tangents.ToArray();
        newMesh.colors = srcMesh.colors.ToArray();

        foreach (var filter in boneMeshes) {
            Mesh boneMesh = filter.sharedMesh;
            List<Vector3> newVerts = newMesh.vertices.ToList();
            List<Vector2> newUv = newMesh.uv.ToList();
            List<Vector3> newNormals = newMesh.normals.ToList();
            List<Vector4> newTangents = newMesh.tangents.ToList();
            List<Color> newColors = newMesh.colors.ToList();
            List<int> newTris = newMesh.triangles.ToList();

            for (int i = 0; i < boneMesh.vertexCount; i++) {
                newVerts.Add(filter.transform.TransformPoint(boneMesh.vertices[i]));
            }
            newMesh.vertices = newVerts.ToArray();

            var boneTris = boneMesh.triangles.ToList();
            for (int i = 0; i < boneTris.Count; i++) { boneTris[i] = boneTris[i] + offset; }
            newTris.AddRange(boneTris);
            newMesh.SetTriangles(newTris, 0);

            newUv.AddRange(boneMesh.uv);
            newNormals.AddRange(boneMesh.normals);
            newTangents.AddRange(boneMesh.tangents);
            newColors.AddRange(boneMesh.colors);

            newMesh.uv = newUv.ToArray();
            newMesh.normals = newNormals.ToArray();
            newMesh.tangents = newTangents.ToArray();
            if (srcMesh.colors.Length > 0) { newMesh.colors = newColors.ToArray(); }

            offset += boneMesh.vertexCount;
        }
        newMesh.RecalculateBounds();
        newMesh.MarkDynamic();
        return newMesh;
    }

    static void SetupRenderTexture(RenderTexture rendTex)
    {
        rendTex.enableRandomWrite = true;
        rendTex.Create();
        RenderTexture.active = rendTex;
        GL.Clear(true, true, Color.clear);
    }

    static void DisplayProgress(string title, string info, float progress)
    {
        EditorUtility.DisplayProgressBar(title, info, progress);
    }

    static void ClearProgress()
    {
        EditorUtility.ClearProgressBar();
    }

    // I tried to make this a template function, but unity would complain about accessing a component that didn't exist
    static Animator FindAnimator(GameObject go)
    {
        Animator ret = go.GetComponent<Animator>();
        if (ret != null) { return ret; }
        return go.GetComponentInChildren<Animator>(true);
    }

    static Animation FindAnimation(GameObject go)
    {
        Animation ret = go.GetComponent<Animation>();
        if (ret != null) { return ret; }
        return go.GetComponentInChildren<Animation>(true);
    }
}

#endif