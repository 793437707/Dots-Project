// This class creates a window in the unity editor used for baking textures
//--------------------------------------------------------------------------------------------------//

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AnimationKitchenWindow : EditorWindow
{
    GameObject m_prefab; // GUI - set by user - the prefab with animation clips and a skinned mesh renderer that will be baked
    Unity.Scenes.SubScene m_subScene; // GUI - set by the user - subscene where baked prefabs are placed
    string m_outputFolder; // GUI - the folder where the generated stuff should be placed.
    string m_generatedScriptOutFolder;
    string m_predictionText = ""; // GUI - displays the text with info about the predicted output 
    string m_lastBakeText = ""; // GUI - displays the result of the most recent bake operation
    string m_warningText = "";

    int m_selectedFormatIndex = 0; // GUI - currently selected index in m_frameRates and m_frameRateOptions
    string[] m_formatStrings; // GUI - array that holds all the possible frame-rate/texture size combinations
    List<TexSampleFormat> m_formats; // GUI - array that holds all the possible frame-rates

    [SerializeReference]
    Shader m_playbackShader; // GUI - the playback shader that the operator wants to be attached to the output material and prefab. 

    bool m_bakeButtonEnabled = false; // GUI when set to false, the bake button will be disabled
    bool m_formatComboEnabled = false; // GUI when set to true, the frame-rate combo will be disabled.

    // these variables are used in detecting when the editor finishes recompiling
    // (we force a recompile because baking auto-generates AnimDb.cs)
    static bool m_justRecompiled = false;
    bool m_isWaitingForRecompile = false;

    BakeOptions m_opts;

    bool m_isInitialized = false;

    // static constructor
    static AnimationKitchenWindow()
    {
        m_justRecompiled = true;
    }

    // Add menu named "Custom Window" to the Window menu
    [MenuItem("AnimationCooker/Animation Kitchen")]
    static void Initialize()
    {
        // Get existing open window or if none, make a new one:
        AnimationKitchenWindow window = (AnimationKitchenWindow)EditorWindow.GetWindow(typeof(AnimationKitchenWindow), false, "Animation Kitchen");
        window.Show();
    }

    void LoadSpecificSettings(string key)
    {
        // prefab specific settings
        m_opts.format = default; // this will get filled later
        m_opts.enableResetPositionBeforeBake = EditorPrefs.GetBool(key + "EnableResetPositionBeforeBake", false);
        m_opts.enableResetRotationBeforeBake = EditorPrefs.GetBool(key + "EnableResetRotationBeforeBake", false);
        m_opts.enableResetScaleBeforeBake = EditorPrefs.GetBool(key + "EnableResetScaleBeforeBake", false);
        m_opts.ignoreBoneMeshes = EditorPrefs.GetBool(key + "IgnoreBoneMeshes", false);
        m_opts.enableBoneAdjust = EditorPrefs.GetBool(key + "EnableBoneAdjust", false);
        int clipCount = EditorPrefs.GetInt(key + "ClipOptionCount", 0);
        if (clipCount > 0) {
            m_opts.clipOpts = new ClipOption[clipCount];
            for (int i = 0; i < clipCount; i++) {
                m_opts.clipOpts[i].enable = EditorPrefs.GetBool(key + "EnableClip" + i, true);
                m_opts.clipOpts[i].name = EditorPrefs.GetString(key + "ClipName" + i, "");
            }
        } else {
            m_opts.clipOpts = null;
        }

        m_opts.format.width = EditorPrefs.GetInt(key + "FormatWidth", 0);
        m_opts.format.height = EditorPrefs.GetInt(key + "FormatHeight", 0);
        m_opts.format.frameRate = EditorPrefs.GetInt(key + "FormatFps", 0);
    }

    void LoadSettings()
    {
        string projectName = GetProjectName();

        // common settings
        m_opts.outputType = (VtxAnimTexType)EditorPrefs.GetInt(projectName + "OutputType", (int)VtxAnimTexType.Position);
        string playbackShaderName = EditorPrefs.GetString(projectName + "PlaybackShaderName", "AnimationCooker/VtxAnimUnlit");
        m_playbackShader = Shader.Find(playbackShaderName);
        m_outputFolder = EditorPrefs.GetString(projectName + "OutputFolder", "ExampleScene/Baked");
        m_generatedScriptOutFolder = EditorPrefs.GetString(projectName + "GeneratedScriptOutFolder", "ExampleScene/Scripts/Generated");
        m_opts.enableLogFile = EditorPrefs.GetBool(projectName + "EnableLogFile", true);
        m_opts.enableEnumDeclaration = EditorPrefs.GetBool(projectName + "EnableEnumDeclaration", true);

        // subscene
        string subSceneName = EditorPrefs.GetString(projectName + "SubsceneName", "");
        if (subSceneName.Length > 0) {
            var subScenes = GameObject.FindObjectsOfType<Unity.Scenes.SubScene>();
            for (int i = 0; i < subScenes.Length; i++) {
                if (subScenes[i].name == subSceneName) {
                    m_subScene = subScenes[i];
                }
            }
        }

        // prefab settings (all other setting will depend on whether or not a prefab can be loaded)
        bool isPrefabValid = EditorPrefs.GetBool(projectName + "IsPrefabValid", false);
        if (isPrefabValid) {
            if (EditorPrefs.GetBool(projectName + "IsPrefabInScene", false)) {
                string gameObjectName = EditorPrefs.GetString(projectName + "GameObjectName", "");
                if (gameObjectName.Length > 0) { m_prefab = GameObject.Find(gameObjectName); }
            } else {
                string prefabAssetPath = EditorPrefs.GetString(projectName + "PrefabAssetPath", "");
                if (prefabAssetPath.Length > 0) { m_prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath); }
            }
        }

        if (m_prefab != null) {
            if (RefreshPrefab()) {
                // refreshing the predition will fill m_formatStrings (even though the format is still unknown)
                RefreshPrediction();
            }
        }
    }

    void SaveSpecificSettings() { SaveSpecificSettings(m_prefab.name); }

    void SaveSpecificSettings(string key)
    {
        EditorPrefs.SetBool(key + "EnableResetPositionBeforeBake", m_opts.enableResetPositionBeforeBake);
        EditorPrefs.SetBool(key + "EnableResetRotationBeforeBake", m_opts.enableResetRotationBeforeBake);
        EditorPrefs.SetBool(key + "EnableResetScaleBeforeBake", m_opts.enableResetScaleBeforeBake);
        EditorPrefs.SetBool(key + "EnableEnumDeclaration", m_opts.enableEnumDeclaration);
        EditorPrefs.SetBool(key + "EnableLogFile", m_opts.enableLogFile);
        EditorPrefs.SetBool(key + "IgnoreBoneMeshes", m_opts.ignoreBoneMeshes);
        EditorPrefs.SetBool(key + "EnableBoneAdjust", m_opts.enableBoneAdjust);

        if (m_opts.clipOpts != null) {
            EditorPrefs.SetInt(key + "ClipOptionCount", m_opts.clipOpts.Length);
            for (int i = 0; i < m_opts.clipOpts.Length; i++) {
                ClipOption clipOpt = m_opts.clipOpts[i];
                EditorPrefs.SetBool(key + "EnableClip" + i, clipOpt.enable);
                EditorPrefs.SetString(key + "ClipName" + i, clipOpt.name);
            }
        }

        EditorPrefs.SetInt(key + "FormatWidth", m_opts.format.width);
        EditorPrefs.SetInt(key + "FormatHeight", m_opts.format.height);
        EditorPrefs.SetInt(key + "FormatFps", m_opts.format.frameRate);
    }

    void SaveSettings()
    {
        string projectName = GetProjectName();

        // common settings
        EditorPrefs.SetInt(projectName + "OutputType", (int)m_opts.outputType);
        EditorPrefs.SetString(projectName + "OutputFolder", m_outputFolder);
        EditorPrefs.SetString(projectName + "GeneratedScriptOutFolder", m_generatedScriptOutFolder);
        EditorPrefs.SetString(projectName + "PlaybackShaderName", m_playbackShader.name);
        EditorPrefs.SetString(projectName + "SubsceneName", m_subScene.name);

        if (m_prefab != null) {
            GameObject go = GameObject.Find(m_prefab.name);
            if (go == m_prefab) {
                EditorPrefs.SetBool(projectName + "IsPrefabValid", true);
                EditorPrefs.SetBool(projectName + "IsPrefabInScene", true);
                EditorPrefs.SetString(projectName + "GameObjectName", m_prefab.name);
                SaveSpecificSettings();
            } else {
                EditorPrefs.SetBool(projectName + "IsPrefabValid", true);
                EditorPrefs.SetBool(projectName + "IsPrefabInScene", false);
                EditorPrefs.SetString(projectName + "PrefabAssetPath", AssetDatabase.GetAssetPath(m_prefab));
                SaveSpecificSettings();
            }
        } else {
            EditorPrefs.SetBool(projectName + "IsPrefabValid", false);
        }
    }

    // whenever the window is first displayed, we'll restore previous settings
    // it sucks because whenever the asset database refreshes (like after baking), this function get called and a bunch of stuff gets reset.
    // but only some things get reset... it's a clusterphuck.
    void OnEnable()
    {
        if (m_isInitialized) { return; }
        LoadSettings(); // should only ever get called once
        m_isInitialized = true;
    }

    private void OnDestroy()
    {
        SaveSettings();
    }

    // this gets called whenver the GUI needs to be refreshed.
    // it is where we draw all of the controls.
    void OnGUI()
    {
        // playback shader
        EditorGUI.BeginChangeCheck();
        m_playbackShader = EditorGUILayout.ObjectField(new GUIContent("Playback Shader", "The shader that will be used for the material that gets generated. (default AnimationCooker/VtxAnimUnlit)"), m_playbackShader, typeof(Shader), true) as Shader;
        if (EditorGUI.EndChangeCheck()) { OnPrefabChanged(); }

        // output folder
        m_outputFolder = EditorGUILayout.TextField(new GUIContent("Bake Output Folder", "The directory where all the generated assets will be placed (default ExampleScene/Baked)"), m_outputFolder);

        // generated script output folder
        m_generatedScriptOutFolder = EditorGUILayout.TextField(new GUIContent("Script Output Folder", "The directory where generated scripts will be placed (default ExampleScene/Scripts/Generated)"), m_generatedScriptOutFolder);

        // subscene
        m_subScene = EditorGUILayout.ObjectField(new GUIContent("Subscene Object", "Optional - subcene under which baked prefabs will be placed after baking. Note - subscene must be opened because it can't be opened programmatically. (default null)"), m_subScene, typeof(Unity.Scenes.SubScene), true) as Unity.Scenes.SubScene;

        // prefab field
        EditorGUI.BeginChangeCheck();
        m_prefab = EditorGUILayout.ObjectField(new GUIContent("Prefab", "The prefab that contains the animations - you can drag-and-drop it here."), m_prefab, typeof(GameObject), true) as GameObject;
        if (EditorGUI.EndChangeCheck()) { OnPrefabChanged(); }

        // format combo-box
        if (m_formatStrings != null) {
            GUI.enabled = m_formatComboEnabled; // disable if there are any problems
            EditorGUI.BeginChangeCheck();
            m_selectedFormatIndex = EditorGUILayout.Popup(new GUIContent("Format", "The frame rate and format that you wish to bake at."), m_selectedFormatIndex, m_formatStrings);
            if (EditorGUI.EndChangeCheck()) { OnFormatChanged(); }
            GUI.enabled = true;
        }

        // list of all animation clips
        GUILayout.Space(8);

        // draw all the clips and checkboxes for them
        // be careful - sometimes m_opts.clipOpts can be null
        if ((m_prefab != null) && (m_opts.clipOpts != null)) {
            // Select all/none animation clips 
            EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select All")) { OnBtnToggleAllClips(true); }
                if (GUILayout.Button("Deselect All")) { OnBtnToggleAllClips(false); }
                if (GUILayout.Button("Reset Names")) { OnBtnResetNames(); }
            EditorGUILayout.EndHorizontal();
            int idx = 0;

            foreach (ClipOption clipOpt in m_opts.clipOpts) {
                EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    m_opts.clipOpts[idx].SetName(EditorGUILayout.TextField(new GUIContent("", "tooltip"), m_opts.clipOpts[idx].name, GUILayout.MinWidth(100)));
                    if (clipOpt.clip != null) {
                        if (EditorGUI.EndChangeCheck()) { SaveSpecificSettings(); }
                        EditorGUILayout.LabelField(string.Format(" [{0:0.##}s, {1}f]", clipOpt.clip.length, (int)(clipOpt.clip.length * clipOpt.clip.frameRate)), GUILayout.MinWidth(50));
                        EditorGUI.BeginChangeCheck();
                    }
                    m_opts.clipOpts[idx].SetEnable(EditorGUILayout.Toggle(new GUIContent("", "Check to include in the baked output."), m_opts.clipOpts[idx].enable));
                    if (EditorGUI.EndChangeCheck()) { OnClipStatusChanged(idx); }
                EditorGUILayout.EndHorizontal();
                idx++;
            }
        }
        GUILayout.Space(8);

        // various check boxes
        EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            m_opts.enableResetPositionBeforeBake = EditorGUILayout.Toggle(new GUIContent("Reset Position B4 Bake", "Some models require their positions to be reset to vertices to match their meshes. (default false)"), m_opts.enableResetPositionBeforeBake);
            m_opts.enableResetRotationBeforeBake = EditorGUILayout.Toggle(new GUIContent("Reset Rotation B4 Bake", "Some models require their rotations to be reset to get their vertices to match their meshes. (default false)"), m_opts.enableResetRotationBeforeBake);
            if (EditorGUI.EndChangeCheck()) { SaveSpecificSettings(); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            m_opts.enableResetScaleBeforeBake = EditorGUILayout.Toggle(new GUIContent("Reset Scale B4 Bake", "Some models require their scales to be reset to get their vertices to match their meshes. (default false)"), m_opts.enableResetScaleBeforeBake);
            if (EditorGUI.EndChangeCheck()) { SaveSpecificSettings(); }
            m_opts.enableEnumDeclaration = EditorGUILayout.Toggle(new GUIContent("Declare Clip Enums", "If this is enabled, the output database will attempt to create enums for clip types. (default true)"), m_opts.enableEnumDeclaration, GUILayout.MinWidth(200));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            m_opts.ignoreBoneMeshes = EditorGUILayout.Toggle(new GUIContent("Ignore Bone Meshes", "If true, child bone meshes will be ignored. (default false)"), m_opts.ignoreBoneMeshes);
            if (EditorGUI.EndChangeCheck()) { RefreshPredictionAndSaveSpecificSettings(); }
            m_opts.enableLogFile = EditorGUILayout.Toggle(new GUIContent("Enable Log File", "Outputs a log file to the output folder. (default true)"), m_opts.enableLogFile);
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        m_opts.enableBoneAdjust = EditorGUILayout.Toggle(new GUIContent("Enable Bone Adjust", "If true, bone offset/scale based on bounds will be performed. (default false)"), m_opts.enableBoneAdjust);
        if (EditorGUI.EndChangeCheck()) { SaveSpecificSettings(); }

        EditorGUI.BeginChangeCheck();
        m_opts.outputType = (VtxAnimTexType)EditorGUILayout.EnumPopup("Generated Textures: ", m_opts.outputType);
        if (EditorGUI.EndChangeCheck()) { RefreshPredictionAndSaveSpecificSettings(); }
        
        // bake button
        GUI.enabled = m_bakeButtonEnabled;
        if (GUILayout.Button(new GUIContent("Bake","Press this button when you are ready to bake an animation."))) { OnBtnBake(); }
        GUI.enabled = true;

        // prediction text area
        EditorGUILayout.PrefixLabel("Prediction:");
        EditorGUILayout.TextArea(m_predictionText, EditorStyles.textArea);

        // result text area
        EditorGUILayout.PrefixLabel("Last Bake Result:");
        EditorGUILayout.TextArea(m_lastBakeText, EditorStyles.textArea);

        // warning text area
        EditorGUILayout.PrefixLabel("Warnings:");
        EditorGUILayout.TextArea(m_warningText, EditorStyles.textArea);
    }

    void OnBtnResetNames()
    {
        if (m_opts.clipOpts == null) { return; }
        for (int i = 0; i < m_opts.clipOpts.Length; i++) {
            m_opts.clipOpts[i].name = m_opts.clipOpts[i].clip.name;
        }
    }

    // called when the user clicks the toggle-all-clips box
    void OnBtnToggleAllClips(bool enable)
    {
        // toggle all clips
        for (int i = 0; i < m_opts.clipOpts.Length; i++) { m_opts.clipOpts[i].SetEnable(enable); }
        // ensure that at least one item is selected if this is a disable-all command
        if (enable == false) { m_opts.clipOpts[0].SetEnable(true); }
        RefreshPredictionAndSaveSpecificSettings();
    }

    // called whenever someone checks one of the boxes next to a clip
    void OnClipStatusChanged(int index)
    {
        // Important! Force at least one clip to be selected.
        // If there is no selection, then we can't make any predictions,
        // which will prevent us from refreshing the combo box.
        EnsureAtLeastOneClip();
        RefreshPredictionAndSaveSpecificSettings();
    }

    // called whenever the user selects a different format from the format combo-box
    void OnFormatChanged()
    {
        // since the format changed, set the new format in the options
        if ((m_formats != null) && (m_selectedFormatIndex < m_formats.Count)) { m_opts.format = m_formats[m_selectedFormatIndex]; }
        RefreshPredictionAndSaveSpecificSettings();
    }

    // called whenever the user changes the prefab
    // whenever the user changes the prefab, we'll recalculate all the possible frame-rates and texture sizes
    // and use that info to repopulate the frame rate combo-box
    void OnPrefabChanged()
    {
        if (!RefreshPrefab()) { return; }
        RefreshPrediction();
        SaveSettings();
    }

    bool RefreshPrefab()
    {
        m_formatComboEnabled = m_bakeButtonEnabled = false;
        m_warningText = "";

        // validate a whole bunch of things.

        // prefab cannot be null
        if (m_prefab == null) {
            m_warningText = "Prefab is null.";
            return false; // buttons will be disabled
        }

        // there must be an animator or animation component on it that has some clips.
        // this is the one and only place that the clips get fetched
        // (here when the prefab is refreshed).
        AnimationClip[] allClips = AnimationCookerUtils.GetClips(m_prefab, ref m_warningText);
        
        // set the clips to null (that way, if fetching all clips fails, then no clips will be displayed)
        m_opts.clipOpts = null;
        if ((allClips == null) || (m_warningText.Length > 0)) { return false; } // buttons will stay disabled

        LoadSpecificSettings(m_prefab.name); // recreates m_opts.clipOpts and fills it. also fills m_opts.format with last used format

        // save a copy of the loaded clip options
        ClipOption[] loadedOpts = null;
        if (m_opts.clipOpts != null) {
            loadedOpts = new ClipOption[m_opts.clipOpts.Length];
            m_opts.clipOpts.CopyTo(loadedOpts, 0);
        }

        // make sure m_opts is the same size as allclips
        m_opts.clipOpts = new ClipOption[allClips.Length];

        // synchronize all clips with clip options, using old values if they exist
        for (int i = 0; i < allClips.Length; i++) {
            if ((loadedOpts != null) && (i < loadedOpts.Length)) {
                // use old clip opts (just loaded in from saved settings)
                m_opts.clipOpts[i] = new ClipOption { clip = allClips[i], name = loadedOpts[i].name, enable = loadedOpts[i].enable };
            } else {
                // make a new entry with default settings
                m_opts.clipOpts[i] = new ClipOption { clip = allClips[i], name = allClips[i].name, enable = true };
            }
        }

        // verify that there is a skinned mesh renderer on this prefab
        if (AnimationCookerUtils.FindSkinnedMeshRenderer(m_prefab) == null) {
            m_warningText = $"A skinned mesh renderer was not found on prefab: {m_prefab.name}";
            return false;
        }

        // re-enable buttons
        m_formatComboEnabled = m_bakeButtonEnabled = true;
        m_warningText = "Ready to bake.";
        return true;
    }

    void EnsureAtLeastOneClip()
    {
        bool hasAtLeastOneClip = false;
        for (int i = 0; i < m_opts.clipOpts.Length; i++) {
            if (m_opts.clipOpts[i].enable) { hasAtLeastOneClip = true; break; }
        }
        if (!hasAtLeastOneClip) { m_opts.clipOpts[0].SetEnable(true); }
    }

    // I had to do some voodoo here to catch when recompile finished. 
    void Update()
    {
        if (m_justRecompiled && m_isWaitingForRecompile) {
            m_isWaitingForRecompile = false;
            OnRecompileFinished();
        }
        m_justRecompiled = false;
    }

    // This gets called whenever recompiling finishes
    void OnRecompileFinished()
    {
        m_lastBakeText += "\nAsset recompilation has finished.";
    }

    // Called whenever the user presses the "Bake" button in the inspector
    // Our main steps here are:
    //    1) fetch the compute shader
    //    2) perform the bake operation
    //    3) perform the save-bake-files operation
    // Note that because the save-bake-files function generates some C# code, the last step of the save-bake-files operation 
    // requires refreshing script assemblies, which is an asynchronous function that takes several seconds.
    // In theory, if the user were to hit play immediately after baking, the last thing they baked might not be entered into
    // the datbase of animation clips so the animations for that model would be broken.  Bake() updates the database, but
    // the database is a static, so once play is hit, I think the static would get reset. However, I don't know if the editor
    // will allow play to be pressed in the middle of a refresh, so perhaps there wouldn't be an issue after-all.
    void OnBtnBake()
    {
        string computeShaderPath = "VtxAnimTextureGenPos";
        if (m_opts.outputType == VtxAnimTexType.PositionAndNormal) {
            computeShaderPath = "VtxAnimTextureGenPosNml";
        } else if (m_opts.outputType == VtxAnimTexType.PositionNormalAndTangent) {
            computeShaderPath = "VtxAnimTextureGenPosNmlTan";
        }
        ComputeShader generatorShader = (ComputeShader)Resources.Load(computeShaderPath);

        if (generatorShader == null) {
            m_lastBakeText = "Unable to find compute shader: " + computeShaderPath;
            return;
        }

        m_isWaitingForRecompile = true;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        m_lastBakeText = $"{System.DateTime.Now.ToString(new System.Globalization.CultureInfo("en-US"))}\n";
        BakeResult bakeResult = AnimationCookerUtils.Bake(m_prefab, generatorShader, m_opts);
        m_lastBakeText += bakeResult.message + "\nBaking completed in " + string.Format("{0:0.####}", watch.Elapsed.TotalSeconds) + " seconds.";

        if (m_playbackShader == null) {
            m_lastBakeText += "\n!!! You didn't set the play shader. Output files were not saved!!!";
            return;
        }

        SaveResult saveResult = AnimationCookerUtils.SaveBakedFiles(bakeResult, m_outputFolder, m_generatedScriptOutFolder, m_playbackShader, m_subScene);
        if (saveResult.message != "") { m_lastBakeText += saveResult.message; }
        // Note that everything isn't completely finished when the code reaches this point - the saved bake files triggered a refresh.
    }

    void RefreshPredictionAndSaveSpecificSettings()
    {
        RefreshPrediction();
        SaveSpecificSettings(m_prefab.name);
    }

    // Whenever something changes that could affect the prediction, this function should be called.
    // It will recalculate stats with the new settings and update the prediction string as well as the format combo box
    void RefreshPrediction()
    {
        if (m_prefab == null) { return; }

        // first calculate some stats that we'll need later
        ClipStats clipStats = AnimationCookerUtils.CalculateSelectedClipStats(m_opts.clipOpts);
        MeshStats meshStats = AnimationCookerUtils.CalculateMeshStats(m_prefab, m_opts.ignoreBoneMeshes);

        // now find out what possible formats we can possibly make using the selected clip stats
        // note that this might yield a different set of formats compared what existed before
        m_formats = AnimationCookerUtils.DiscoverPossibleFormats(meshStats.vertexCount, clipStats.totalClipLength, clipStats.smallestFps);
        if (m_formats.Count <= 0) { 
            m_predictionText = "No possible formats found.";
            return;
        }

        if ((m_opts.format.frameRate <= 0) || (m_opts.format.frameRate > 100)) {
            m_opts.format.frameRate = 1;
        }

        // since the settings may have an effect on the format choices that can be selected,
        // we may have to choose a new format - prefereably one that is the closest to the previous one.
        m_selectedFormatIndex = SelectClosestFormat(m_opts.format.frameRate);
        m_opts.format = m_formats[m_selectedFormatIndex];

        // using the NEW format, calculate the new frame stats
        FrameStats frameStats = AnimationCookerUtils.CalculateFrameStats(m_opts.clipOpts, m_opts.format, meshStats.vertexCount);

        // now summarize all the new stats we just calculated into the prediction string
        m_predictionText = AnimationCookerUtils.MakePredictionString(m_opts, meshStats, clipStats, frameStats);
        if (m_opts.format.width > 2048) { m_predictionText += "\nTexture width is > 2048, mobile devices will be sad."; }
        if (m_opts.format.height > 2048) { m_predictionText += "\nTexture height is > 2048, mobile devices will be sad."; }
        // this estimate is based on a 6 core i7 9750H laptop with a mobile 2060 rtx gpu... it won't be very accurate if the machine differs a lot in specs
        m_predictionText += "\nEstimated bake time: " + (int)(0.0000366 * frameStats.pointCount) + "s";

        // update the format combo box to reflect the new options and selection
        // (because m_selectedFormatIndex was set above, the correct item will be selected)
        RefreshFormatComboBox();
    }

    // returns the index of the closest format to the specified frame rate
    // if the specified frame rate is invalid, then the first available format is selected.
    // m_formats must be valid before calling this function.
    int SelectClosestFormat(int frameRate)
    {
        if (frameRate <= 0) { return 0; }

        float smallestDif = float.MaxValue;
        int smallestDifIndex = 0;
        for (int i = 0; i < m_formats.Count; i++) {
            float dif = Mathf.Abs(m_formats[i].frameRate - frameRate);
            if (dif < smallestDif) {
                smallestDif = dif;
                smallestDifIndex = i;
            }
        }
        return smallestDifIndex;
    }

    // this function will refresh the combo box selections to reflect the current choices specified in m_formats
    void RefreshFormatComboBox()
    {
        // fill the format strings
        m_formatStrings = new string[m_formats.Count];
        for (int i = 0; i < m_formats.Count; i++) {
            TexSampleFormat fmt = m_formats[i];
            m_formatStrings[i] = $"{fmt.frameRate} fps ({fmt.width}x{fmt.height}, {(int)((float)fmt.CalculateBytes(m_opts.outputType) * 0.001)} KB)";
        }
    }

    string GetProjectName()
     {
         string[] s = Application.dataPath.Split('/');
         string projectName = s[s.Length - 2];
         return projectName;
     }
}

#endif