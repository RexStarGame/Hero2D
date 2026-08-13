public enum MenuOwner
{
    None,
    Upgrade,
    Store,
    Pause
}

public static class MenuLock
{
    public static MenuOwner Owner = MenuOwner.None;
    public static bool IsGameplayInputBlocked => Owner != MenuOwner.None;

    public static bool CanOpen(MenuOwner requester)
    {
        return Owner == MenuOwner.None || Owner == requester;
    }

    public static void Set(MenuOwner owner)
    {
        Owner = owner;
    }

    public static void Clear(MenuOwner owner)
    {
        if (Owner == owner)
            Owner = MenuOwner.None;
    }
}