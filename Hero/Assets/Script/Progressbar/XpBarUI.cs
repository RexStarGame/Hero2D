using UnityEngine;
using UnityEngine.UI;

public class XPBarUI : MonoBehaviour
{
    public Slider slider;
    public PlayerXP player;

    void Update()
    {
        // Fill amount from 0–1
        slider.value = (float)player.xp / player.xpToNextLevel;
    }
}
