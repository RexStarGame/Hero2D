using TMPro;
using UnityEngine;

public class CoinCounterUI : MonoBehaviour
{
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private string prefix = "Coins: ";

    private void Awake()
    {
        if (coinText == null)
            coinText = GetComponent<TMP_Text>();

        FindWallet();
        Refresh();
    }

    private void OnEnable()
    {
        FindWallet();

        if (wallet != null)
            wallet.GoldChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (wallet != null)
            wallet.GoldChanged -= Refresh;
    }

    public void Refresh()
    {
        if (coinText == null)
            return;

        coinText.text = wallet == null
            ? $"{prefix}N/A"
            : $"{prefix}<color=#FFD166>{wallet.Gold}</color>";
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
}
