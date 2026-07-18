using System.Collections.Generic;
using UnityEngine;

public class DifficultyDebugHUD : MonoBehaviour
{
    private readonly List<EnemyDifficultyProfile> profiles =
        new List<EnemyDifficultyProfile>(32);

    private Rect windowRect = new Rect(18f, 55f, 620f, 520f);
    private Vector2 scrollPosition;
    private bool visible;
    private float nextRefreshTime;
    private EnemySpawn enemySpawn;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateDebugHUD()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (FindAnyObjectByType<DifficultyDebugHUD>() != null)
            return;

        GameObject hudObject = new GameObject("DifficultyDebugHUD");
        DontDestroyOnLoad(hudObject);
        hudObject.AddComponent<DifficultyDebugHUD>();
#endif
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
        {
            visible = !visible;
            if (visible)
                RefreshRuntimeData(true);
        }

        if (visible)
            RefreshRuntimeData(false);
    }

    private void OnGUI()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        GUI.Box(new Rect(18f, 15f, 235f, 32f), "F8  Difficulty Test Panel");

        if (!visible)
            return;

        windowRect = GUI.Window(
            GetInstanceID(), windowRect, DrawWindow,
            "HERO2D DIFFICULTY TEST (development only)");
#endif
    }

    private void DrawWindow(int windowId)
    {
        GUILayout.BeginVertical();
        GUILayout.Label($"Active mode: {DifficultyManager.CurrentDifficulty}");

        GUILayout.BeginHorizontal();
        DrawDifficultyButton(GameDifficulty.Easy);
        DrawDifficultyButton(GameDifficulty.Normal);
        DrawDifficultyButton(GameDifficulty.Hard);
        DrawDifficultyButton(GameDifficulty.Nightmare);
        GUILayout.EndHorizontal();

        if (enemySpawn != null)
        {
            GUILayout.Label(
                $"Normal enemy spawn limits: {enemySpawn.ActiveMinSpawn} - " +
                $"{enemySpawn.ActiveMaxSpawn}  (living: {EnemyCounter.Count})");
        }

        GUILayout.Space(8f);
        GUILayout.Label("LATEST ENEMY HIT");

        if (DifficultyDebugTelemetry.LastDamageTime < 0f)
        {
            GUILayout.Label("Let an enemy hit the player to record exact damage.");
        }
        else
        {
            GUILayout.Label($"Source: {DifficultyDebugTelemetry.LastDamageSource}");
            string finalDamage = DifficultyDebugTelemetry.LastDamageReachedPlayerHealth
                ? DifficultyDebugTelemetry.LastDamageAfterDefense.ToString("0.##")
                : "blocked before HP (for example dash immunity)";
            GUILayout.Label(
                $"Base {DifficultyDebugTelemetry.LastBaseDamage:0.##}  ->  " +
                $"Difficulty {DifficultyDebugTelemetry.LastDifficultyDamage:0.##}  ->  " +
                $"After Defense {finalDamage}");
        }

        GUILayout.Space(8f);
        GUILayout.Label("LIVE ENEMY / BOSS HEALTH AND DAMAGE MULTIPLIERS");

        scrollPosition = GUILayout.BeginScrollView(
            scrollPosition, GUILayout.Height(300f));

        if (profiles.Count == 0)
            GUILayout.Label("No living enemies or bosses found yet.");

        for (int i = 0; i < profiles.Count; i++)
            DrawProfile(profiles[i]);

        GUILayout.EndScrollView();
        GUILayout.Label(
            "Expected defaults: Easy 0/0%, Normal +10/+5%, " +
            "Hard +30/+20%, Nightmare +60/+40% (HP/Damage).");
        GUILayout.Label("F8 closes this panel. Values update while the game is paused too.");
        GUILayout.EndVertical();

        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 28f));
    }

    private static void DrawDifficultyButton(GameDifficulty difficulty)
    {
        GUI.enabled = DifficultyManager.CurrentDifficulty != difficulty;
        if (GUILayout.Button(difficulty.ToString(), GUILayout.Height(30f)))
            DifficultyManager.Instance.SetDifficulty(difficulty);
        GUI.enabled = true;
    }

    private static void DrawProfile(EnemyDifficultyProfile profile)
    {
        if (profile == null)
            return;

        EnemyHealth enemy = profile.GetComponentInChildren<EnemyHealth>(true);
        BossHealth boss = profile.GetComponentInChildren<BossHealth>(true);
        string healthText = "No health component";

        if (enemy != null)
        {
            healthText =
                $"HP {enemy.CurrentHealth}/{enemy.MaxHealth} " +
                $"(base {enemy.BaseMaxHealth} -> max {enemy.MaxHealth})";
        }
        else if (boss != null)
        {
            healthText =
                $"HP {boss.CurrentHealth:0.##}/{boss.MaxHealth:0.##} " +
                $"(base {boss.BaseMaxHealth:0.##} -> max {boss.MaxHealth:0.##})";
        }

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(profile.name);
        GUILayout.Label(healthText);
        GUILayout.Label(
            $"Profile: HP +{profile.CurrentHealthBonusPercent:0.##}%  |  " +
            $"Damage +{profile.CurrentDamageBonusPercent:0.##}% " +
            $"(x{profile.DamageMultiplier:0.###})");
        GUILayout.EndVertical();
    }

    private void RefreshRuntimeData(bool force)
    {
        if (!force && Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + 0.35f;
        profiles.Clear();

#if UNITY_2023_1_OR_NEWER
        EnemyDifficultyProfile[] found = FindObjectsByType<EnemyDifficultyProfile>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        EnemyDifficultyProfile[] found = FindObjectsOfType<EnemyDifficultyProfile>();
#endif

        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null)
                profiles.Add(found[i]);
        }

        if (enemySpawn == null)
            enemySpawn = FindAnyObjectByType<EnemySpawn>();
    }

    private static T FindAnyObjectByType<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindAnyObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
