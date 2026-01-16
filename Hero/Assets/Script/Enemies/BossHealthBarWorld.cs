using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarWorld : MonoBehaviour
{
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private Slider slider;

    [Header("Debug")]
    [SerializeField] private bool log = true;

    private void Awake()
    {
        if (bossHealth == null)
            bossHealth = GetComponentInParent<BossHealth>();

        if (slider == null)
            slider = GetComponentInChildren<Slider>(true);

        if (log)
        {
            Debug.Log($"[BossHealthBarWorld] Awake on {name} | bossHealth={(bossHealth ? bossHealth.name : "NULL")} | slider={(slider ? slider.name : "NULL")}");
        }
    }

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            bossHealth.onHealthChanged.AddListener(UpdateBar);
            if (log) Debug.Log("[BossHealthBarWorld] Subscribed to onHealthChanged");
        }
        else
        {
            if (log) Debug.LogError("[BossHealthBarWorld] bossHealth is NULL (Canvas must be child of Boss or assign it)");
        }
    }

    private void Start()
    {
        if (bossHealth != null)
        {
            if (log) Debug.Log($"[BossHealthBarWorld] Start initial update: {bossHealth.CurrentHealth}/{bossHealth.maxHealth}");
            UpdateBar(bossHealth.CurrentHealth, bossHealth.maxHealth);
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
            bossHealth.onHealthChanged.RemoveListener(UpdateBar);
    }

    private void UpdateBar(float current, float max)
    {
        if (slider == null)
        {
            Debug.LogError("[BossHealthBarWorld] slider is NULL");
            return;
        }

        slider.maxValue = max;
        slider.value = current;

        if (log) Debug.Log($"[BossHealthBarWorld] UpdateBar -> {current}/{max}");
    }
}
