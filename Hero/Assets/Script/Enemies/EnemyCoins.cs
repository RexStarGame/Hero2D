using UnityEngine;

public class EnemyCoins : MonoBehaviour
{
    [Header("Coin Reward")]
    [Min(0)] [SerializeField] private int minCoins = 1;
    [Min(0)] [SerializeField] private int maxCoins = 3;

    [Header("Optional - auto-find if empty")]
    [SerializeField] private PlayerWallet wallet;

    private EnemyHealth enemyHealth;
    private BossHealth bossHealth;
    private bool rewarded;

    public int MinCoins => minCoins;
    public int MaxCoins => maxCoins;

    private void Awake()
    {
        FindDeathSource();
        FindWallet();
    }

    private void OnEnable()
    {
        FindDeathSource();

        if (enemyHealth != null)
            enemyHealth.onDeath.AddListener(AwardCoins);

        if (bossHealth != null)
            bossHealth.onDeath.AddListener(AwardCoins);
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
            enemyHealth.onDeath.RemoveListener(AwardCoins);

        if (bossHealth != null)
            bossHealth.onDeath.RemoveListener(AwardCoins);
    }

    public void AwardCoins()
    {
        if (rewarded)
            return;

        rewarded = true;
        FindWallet();

        if (wallet == null)
        {
            Debug.LogWarning(
                $"[EnemyCoins] No PlayerWallet was found for '{gameObject.name}'.",
                this);
            return;
        }

        int safeMinimum = Mathf.Max(0, minCoins);
        int safeMaximum = Mathf.Max(safeMinimum, maxCoins);
        int reward = Random.Range(safeMinimum, safeMaximum + 1);

        wallet.AddGold(reward);
        Debug.Log($"{gameObject.name} rewarded {reward} coins.");
    }

    private void FindDeathSource()
    {
        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<EnemyHealth>();
            if (enemyHealth == null)
                enemyHealth = GetComponentInChildren<EnemyHealth>(true);
        }

        if (bossHealth == null)
        {
            bossHealth = GetComponent<BossHealth>();
            if (bossHealth == null)
                bossHealth = GetComponentInChildren<BossHealth>(true);
        }
    }

    private void FindWallet()
    {
        if (wallet != null)
            return;

#if UNITY_2023_1_OR_NEWER
        wallet = FindAnyObjectByType<PlayerWallet>();
#else
        wallet = FindObjectOfType<PlayerWallet>();
#endif
    }

    private void OnValidate()
    {
        minCoins = Mathf.Max(0, minCoins);
        maxCoins = Mathf.Max(minCoins, maxCoins);
    }
}
