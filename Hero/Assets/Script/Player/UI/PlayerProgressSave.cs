using UnityEngine;

/// <summary>
/// Owns persistent player progression and the player's last saved world position.
/// Inventory, health and enemy world state remain outside this save system.
/// </summary>
public static class PlayerProgressSave
{
    private const int CurrentVersion = 1;
    private const string Prefix = "Hero2D.Progress.";

    private const string VersionKey = Prefix + "Version";
    private const string LevelKey = Prefix + "Level";
    private const string XpKey = Prefix + "XP";
    private const string AbilityPointsKey = Prefix + "AbilityPoints";

    private const string PositionSavedKey = Prefix + "PositionSaved";
    private const string PositionXKey = Prefix + "PositionX";
    private const string PositionYKey = Prefix + "PositionY";

    private const string DamageLevelKey = Prefix + "DamageLevel";
    private const string MaxHealthLevelKey = Prefix + "MaxHealthLevel";
    private const string MaxHealthKey = Prefix + "MaxHealth";
    private const string RegenLevelKey = Prefix + "RegenLevel";
    private const string AttackSpeedLevelKey = Prefix + "AttackSpeedLevel";
    private const string AttackCooldownKey = Prefix + "AttackCooldown";
    private const string LifeStealLevelKey = Prefix + "LifeStealLevel";
    private const string LifeStealPercentKey = Prefix + "LifeStealPercent";
    private const string CritLevelKey = Prefix + "CritLevel";
    private const string CritChanceKey = Prefix + "CritChance";

    public static void RestorePlayer(PlayerXP player, int inspectorLevel, int inspectorAbilityPoints)
    {
        if (player == null)
            return;

        if (!PlayerPrefs.HasKey(VersionKey))
        {
            int checkpointLevel = Mathf.Max(Mathf.Max(1, inspectorLevel), BossLevelCheckpoint.Level);
            player.level = checkpointLevel;
            player.xp = 0;
            player.xpToNextLevel = player.GetXpRequiredForLevel(checkpointLevel);
            player.abilityPoints = Mathf.Max(0, inspectorAbilityPoints) +
                                   (checkpointLevel - Mathf.Max(1, inspectorLevel));
            SavePlayer(player);
            return;
        }

        player.level = Mathf.Max(1, PlayerPrefs.GetInt(LevelKey, inspectorLevel));
        player.xpToNextLevel = player.GetXpRequiredForLevel(player.level);
        player.xp = Mathf.Clamp(PlayerPrefs.GetInt(XpKey, 0), 0, player.xpToNextLevel - 1);
        player.abilityPoints = Mathf.Max(0, PlayerPrefs.GetInt(
            AbilityPointsKey, inspectorAbilityPoints));
    }

    public static void SavePlayer(PlayerXP player)
    {
        if (player == null)
            return;

        PlayerPrefs.SetInt(VersionKey, CurrentVersion);
        PlayerPrefs.SetInt(LevelKey, Mathf.Max(1, player.level));
        PlayerPrefs.SetInt(XpKey, Mathf.Max(0, player.xp));
        PlayerPrefs.SetInt(AbilityPointsKey, Mathf.Max(0, player.abilityPoints));
        PlayerPrefs.Save();
    }

    public static bool TryRestorePlayerPosition(out Vector2 position)
    {
        position = Vector2.zero;

        if (PlayerPrefs.GetInt(PositionSavedKey, 0) != 1)
            return false;

        float x = PlayerPrefs.GetFloat(PositionXKey, 0f);
        float y = PlayerPrefs.GetFloat(PositionYKey, 0f);

        if (float.IsNaN(x) || float.IsInfinity(x) ||
            float.IsNaN(y) || float.IsInfinity(y))
        {
            return false;
        }

        position = new Vector2(x, y);
        return true;
    }

    public static void SavePlayerPosition(Vector2 position)
    {
        if (float.IsNaN(position.x) || float.IsInfinity(position.x) ||
            float.IsNaN(position.y) || float.IsInfinity(position.y))
        {
            return;
        }

        PlayerPrefs.SetInt(PositionSavedKey, 1);
        PlayerPrefs.SetFloat(PositionXKey, position.x);
        PlayerPrefs.SetFloat(PositionYKey, position.y);
        PlayerPrefs.Save();
    }

    public static void RestoreDamageUpgrade(DamageUpgrade upgrade)
    {
        if (upgrade == null || !PlayerPrefs.HasKey(DamageLevelKey))
            return;

        upgrade.RestoreDamageLevel(PlayerPrefs.GetInt(DamageLevelKey, 0));
    }

    public static void SaveDamageUpgrade(DamageUpgrade upgrade, PlayerXP player)
    {
        if (upgrade == null)
            return;

        PlayerPrefs.SetInt(DamageLevelKey, Mathf.Max(0, upgrade.DamageLevel));
        SavePlayer(player);
    }

    public static void RestoreHealthUpgrades(PlayerHealth health)
    {
        if (health == null)
            return;

        int maxHealthLevel = PlayerPrefs.GetInt(MaxHealthLevelKey, health.maxHealthLevel);
        int regenLevel = PlayerPrefs.GetInt(RegenLevelKey, health.regenLevel);
        float maxHealth = PlayerPrefs.GetFloat(MaxHealthKey, health.maxHealth);
        health.RestoreUpgradeProgress(maxHealthLevel, regenLevel, maxHealth);
    }

    public static void SaveHealthUpgrades(PlayerHealth health, PlayerXP player)
    {
        if (health == null)
            return;

        PlayerPrefs.SetInt(MaxHealthLevelKey, Mathf.Max(0, health.maxHealthLevel));
        PlayerPrefs.SetFloat(MaxHealthKey, Mathf.Max(1f, health.maxHealth));
        PlayerPrefs.SetInt(RegenLevelKey, Mathf.Max(0, health.regenLevel));
        SavePlayer(player);
    }

    public static void RestoreAttackUpgrades(PlayerAttack attack)
    {
        if (attack == null)
            return;

        attack.RestoreUpgradeProgress(
            PlayerPrefs.GetInt(AttackSpeedLevelKey, attack.attackSpeedLevel),
            PlayerPrefs.GetFloat(AttackCooldownKey, attack.BaseAttackCooldown),
            PlayerPrefs.GetInt(LifeStealLevelKey, attack.lifeStealLevel),
            PlayerPrefs.GetFloat(LifeStealPercentKey, attack.AbilityLifeStealPercent),
            PlayerPrefs.GetInt(CritLevelKey, attack.critLevel),
            PlayerPrefs.GetFloat(CritChanceKey, attack.AbilityCritChance));
    }

    public static void SaveAttackUpgrades(PlayerAttack attack, PlayerXP player)
    {
        if (attack == null)
            return;

        PlayerPrefs.SetInt(AttackSpeedLevelKey, Mathf.Max(0, attack.attackSpeedLevel));
        PlayerPrefs.SetFloat(AttackCooldownKey, Mathf.Max(0.01f, attack.BaseAttackCooldown));
        PlayerPrefs.SetInt(LifeStealLevelKey, Mathf.Max(0, attack.lifeStealLevel));
        PlayerPrefs.SetFloat(LifeStealPercentKey, Mathf.Max(0f, attack.AbilityLifeStealPercent));
        PlayerPrefs.SetInt(CritLevelKey, Mathf.Max(0, attack.critLevel));
        PlayerPrefs.SetFloat(CritChanceKey, Mathf.Clamp01(attack.AbilityCritChance));
        SavePlayer(player);
    }

    public static int ResetAbilityUpgrades(
        PlayerXP player,
        DamageUpgrade damage,
        PlayerHealth health,
        PlayerAttack attack)
    {
        if (player == null || damage == null || health == null || attack == null)
            return -1;

        int refundedPoints =
            Mathf.Max(0, damage.DamageLevel) +
            Mathf.Max(0, health.maxHealthLevel) +
            Mathf.Max(0, health.regenLevel) +
            Mathf.Max(0, attack.attackSpeedLevel) +
            Mathf.Max(0, attack.lifeStealLevel) +
            Mathf.Max(0, attack.critLevel);

        damage.RestoreDamageLevel(0);
        health.ResetAbilityUpgradeProgress();
        attack.ResetAbilityUpgradeProgress();
        player.abilityPoints += refundedPoints;

        PlayerPrefs.SetInt(DamageLevelKey, 0);
        PlayerPrefs.SetInt(MaxHealthLevelKey, 0);
        PlayerPrefs.SetFloat(MaxHealthKey, health.maxHealth);
        PlayerPrefs.SetInt(RegenLevelKey, 0);
        PlayerPrefs.SetInt(AttackSpeedLevelKey, 0);
        PlayerPrefs.SetFloat(AttackCooldownKey, attack.BaseAttackCooldown);
        PlayerPrefs.SetInt(LifeStealLevelKey, 0);
        PlayerPrefs.SetFloat(LifeStealPercentKey, 0f);
        PlayerPrefs.SetInt(CritLevelKey, 0);
        PlayerPrefs.SetFloat(CritChanceKey, 0f);
        SavePlayer(player);
        return refundedPoints;
    }

}
