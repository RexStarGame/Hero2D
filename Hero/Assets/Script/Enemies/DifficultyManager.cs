using System;
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    private const string DifficultySaveKey = "Hero2D.GameDifficulty";

    public static event Action<GameDifficulty> DifficultyChanged;

    public static DifficultyManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<DifficultyManager>();
                if (instance == null)
                {
                    GameObject managerObject = new GameObject("DifficultyManager");
                    instance = managerObject.AddComponent<DifficultyManager>();
                }
            }

            return instance;
        }
    }

    public static GameDifficulty CurrentDifficulty => Instance.currentDifficulty;

    [Header("Saved difficulty")]
    [SerializeField] private GameDifficulty currentDifficulty = GameDifficulty.Easy;

    private static DifficultyManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureManagerExists()
    {
        _ = Instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        int savedValue = PlayerPrefs.GetInt(
            DifficultySaveKey,
            (int)GameDifficulty.Easy);
        currentDifficulty = ClampDifficulty(savedValue);
    }

    public void SetDifficulty(GameDifficulty difficulty)
    {
        difficulty = ClampDifficulty((int)difficulty);
        if (currentDifficulty == difficulty)
            return;

        currentDifficulty = difficulty;
        PlayerPrefs.SetInt(DifficultySaveKey, (int)currentDifficulty);
        PlayerPrefs.Save();
        DifficultyChanged?.Invoke(currentDifficulty);
    }

    private static GameDifficulty ClampDifficulty(int value)
    {
        return (GameDifficulty)Mathf.Clamp(
            value,
            (int)GameDifficulty.Easy,
            (int)GameDifficulty.Nightmare);
    }

    private static T FindAnyObjectByType<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindAnyObjectByType<T>();
#else
        return UnityEngine.Object.FindObjectOfType<T>();
#endif
    }
}
