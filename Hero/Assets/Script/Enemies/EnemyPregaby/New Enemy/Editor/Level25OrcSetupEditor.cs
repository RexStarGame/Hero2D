#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds the Animator directly from the tagged Orc Aseprite file and safely
/// configures the existing level-25 enemy prefab for top-down gameplay.
/// </summary>
public static class Level25OrcSetupEditor
{
    private const string Root = "Assets/Script/Enemies/EnemyPregaby/New Enemy";
    private const string OrcPngRoot = Root + "/Tiny RPG Character Asset Pack 01 v2 1.0 -Free Soldier&Orc/Characters(100x100 split)/Orc/Orc";
    private const string GeneratedClipsFolder = Root + "/Generated Orc Animations";
    private const string PrefabPath = Root + "/Chain Enemy.prefab";
    private const string ControllerPath = Root + "/Level25 Orc.controller";

    [MenuItem("Hero2D/Enemies/Setup Level 25 Orc")]
    public static void Setup()
    {
        Dictionary<string, AnimationClip> clips = CreateClipsFromPngStrips();
        string[] required = { "Idle", "Walk", "Attack01", "Attack02", "Hurt", "Death" };
        List<string> missing = new List<string>();

        foreach (string clipName in required)
        {
            if (!clips.ContainsKey(clipName))
                missing.Add(clipName);
        }

        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Level 25 Orc setup stopped",
                "Unity could not build these clips from the Orc PNG strips:\n\n" +
                string.Join(", ", missing) +
                "\n\nConfirm that the split Orc PNG files are still inside the imported asset pack, then run this menu item again.",
                "OK");
            return;
        }

        AnimatorController controller = BuildController(clips);
        ConfigurePrefab(controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        EditorUtility.DisplayDialog(
            "Level 25 Orc is ready",
            "Configured Chain Enemy with:\n" +
            "- Idle, Walk, Attack01, Attack02, Hurt and Death\n" +
            "- side-facing sprite flip\n" +
            "- top-down Rigidbody2D\n" +
            "- stable body-only CapsuleCollider2D\n" +
            "- aimed slash and larger circular AOE\n" +
            "- hit/death animation support and coin reward\n\n" +
            "Add Chain Enemy to EnemySpawn's pool with Min Level 25.",
            "OK");
    }

    private static Dictionary<string, AnimationClip> CreateClipsFromPngStrips()
    {
        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
        EnsureFolder(GeneratedClipsFolder);

        CreateClip(clips, "Idle", "Orc_Idle.png", 6, true);
        CreateClip(clips, "Walk", "Orc_Walk.png", 8, true);
        CreateClip(clips, "Attack01", "Orc_Attack01.png", 6, false);
        CreateClip(clips, "Attack02", "Orc_Attack02.png", 6, false);
        CreateClip(clips, "Hurt", "Orc_Hurt.png", 4, false);
        CreateClip(clips, "Death", "Orc_Death.png", 4, false);
        return clips;
    }

    private static void CreateClip(
        Dictionary<string, AnimationClip> clips,
        string clipName,
        string pngName,
        int frameCount,
        bool loop)
    {
        string pngPath = OrcPngRoot + "/" + pngName;
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath) == null)
            return;

        ConfigurePngStrip(pngPath, clipName, frameCount);
        List<Sprite> frames = new List<Sprite>();
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(pngPath))
        {
            if (asset is Sprite sprite)
                frames.Add(sprite);
        }

        frames.Sort((left, right) => ExtractFrameNumber(left.name).CompareTo(ExtractFrameNumber(right.name)));
        if (frames.Count < frameCount)
            return;

        string clipPath = GeneratedClipsFolder + "/" + clipName + ".anim";
        AssetDatabase.DeleteAsset(clipPath);

        AnimationClip clip = new AnimationClip
        {
            name = clipName,
            frameRate = 10f
        };

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frameCount + 1];
        for (int i = 0; i < frameCount; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / 10f,
                value = frames[i]
            };
        }

        // Duplicate the last sprite one frame later so every source frame is
        // displayed for a full 0.1 seconds.
        keys[frameCount] = new ObjectReferenceKeyframe
        {
            time = frameCount / 10f,
            value = frames[frameCount - 1]
        };

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, clipPath);
        clips[clipName] = clip;
    }

    private static void ConfigurePngStrip(string pngPath, string clipName, int frameCount)
    {
        TextureImporter importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

#pragma warning disable 0618
        SpriteMetaData[] slices = new SpriteMetaData[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            slices[i] = new SpriteMetaData
            {
                name = clipName + "_" + i,
                rect = new Rect(i * 100f, 0f, 100f, 100f),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }
        importer.spritesheet = slices;
#pragma warning restore 0618
        importer.SaveAndReimport();
    }

    private static int ExtractFrameNumber(string spriteName)
    {
        int underscore = spriteName.LastIndexOf('_');
        return underscore >= 0 && int.TryParse(spriteName.Substring(underscore + 1), out int frame)
            ? frame
            : int.MaxValue;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        int slash = folderPath.LastIndexOf('/');
        string parent = folderPath.Substring(0, slash);
        string folder = folderPath.Substring(slash + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    private static AnimatorController BuildController(Dictionary<string, AnimationClip> clips)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack1", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack2", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = AddState(machine, "Idle", clips["Idle"], new Vector3(250f, 50f));
        AnimatorState walk = AddState(machine, "Walk", clips["Walk"], new Vector3(500f, 50f));
        AnimatorState attack1 = AddState(machine, "Attack 1 - Slash", clips["Attack01"], new Vector3(500f, 170f));
        AnimatorState attack2 = AddState(machine, "Attack 2 - AOE Sweep", clips["Attack02"], new Vector3(500f, 280f));
        AnimatorState hurt = AddState(machine, "Hurt", clips["Hurt"], new Vector3(250f, 280f));
        AnimatorState death = AddState(machine, "Death", clips["Death"], new Vector3(750f, 280f));
        machine.defaultState = idle;

        AddFloatTransition(idle, walk, "Speed", AnimatorConditionMode.Greater, 0.01f);
        AddFloatTransition(walk, idle, "Speed", AnimatorConditionMode.Less, 0.01f);

        AddTriggerTransition(machine, attack1, "Attack1", 0.02f);
        AddTriggerTransition(machine, attack2, "Attack2", 0.02f);
        AddTriggerTransition(machine, hurt, "Hurt", 0.01f);
        AddTriggerTransition(machine, death, "Die", 0f);

        AddExitTransition(attack1, idle);
        AddExitTransition(attack2, idle);
        AddExitTransition(hurt, idle);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion, Vector3 position)
    {
        AnimatorState state = machine.AddState(name, position);
        state.motion = motion;
        state.writeDefaultValues = true;
        return state;
    }

    private static void AddFloatTransition(
        AnimatorState from,
        AnimatorState to,
        string parameter,
        AnimatorConditionMode mode,
        float threshold)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.06f;
        transition.AddCondition(mode, threshold, parameter);
    }

    private static void AddTriggerTransition(
        AnimatorStateMachine machine,
        AnimatorState to,
        string parameter,
        float duration)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
    }

    private static void AddExitTransition(AnimatorState from, AnimatorState to)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = 0.96f;
        transition.duration = 0.04f;
    }

    private static void ConfigurePrefab(RuntimeAnimatorController controller)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Animator animator = GetOrAdd<Animator>(root);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            Rigidbody2D body = GetOrAdd<Rigidbody2D>(root);
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            foreach (PolygonCollider2D polygon in root.GetComponents<PolygonCollider2D>())
                UnityEngine.Object.DestroyImmediate(polygon);

            CapsuleCollider2D capsule = GetOrAdd<CapsuleCollider2D>(root);
            capsule.isTrigger = false;
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = new Vector2(0.48f, 0.68f);
            capsule.offset = new Vector2(0f, -0.08f);

            EnemyHealth health = GetOrAdd<EnemyHealth>(root);
            GetOrAdd<EnemyAggro2D>(root);
            GetOrAdd<EnemyDifficultyProfile>(root);
            GetOrAdd<EnemyCoins>(root);
            GetOrAdd<HitFeedback>(root);
            ChainSamuraiLevel25 enemy = GetOrAdd<ChainSamuraiLevel25>(root);
            SpriteRenderer sprite = root.GetComponentInChildren<SpriteRenderer>(true);
            if (sprite != null)
                sprite.sprite = LoadFirstSprite(OrcPngRoot + "/Orc_Idle.png");

            SerializedObject enemySettings = new SerializedObject(enemy);
            Set(enemySettings, "useHorizontalSpriteFlipping", true);
            Set(enemySettings, "sourceSpriteFacesRight", true);
            Set(enemySettings, "facingSprite", sprite);
            Set(enemySettings, "speedParameter", "Speed");
            Set(enemySettings, "slashTrigger", "Attack1");
            Set(enemySettings, "sweepTrigger", "Attack2");
            Set(enemySettings, "hurtTrigger", "Hurt");
            Set(enemySettings, "deathTrigger", "Die");
            Set(enemySettings, "hurtLockDuration", 0.40f);
            Set(enemySettings, "slashWindup", 0.30f);
            Set(enemySettings, "slashRecovery", 0.30f);
            Set(enemySettings, "sweepWindup", 0.30f);
            Set(enemySettings, "sweepRecovery", 0.30f);
            enemySettings.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject healthSettings = new SerializedObject(health);
            Set(healthSettings, "deathDestroyDelay", 1.05f);
            healthSettings.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static Sprite LoadFirstSprite(string assetPath)
    {
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (asset is Sprite sprite && ExtractFrameNumber(sprite.name) == 0)
                return sprite;
        }

        return null;
    }

    private static void Set(SerializedObject target, string propertyName, bool value)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property != null)
            property.boolValue = value;
    }

    private static void Set(SerializedObject target, string propertyName, float value)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property != null)
            property.floatValue = value;
    }

    private static void Set(SerializedObject target, string propertyName, string value)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property != null)
            property.stringValue = value;
    }

    private static void Set(SerializedObject target, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }
}
#endif