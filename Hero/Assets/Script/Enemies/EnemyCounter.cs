using UnityEngine;

public class EnemyCounter : MonoBehaviour
{
    public static int Count { get; private set; }

    private void OnEnable() => Count++;
    private void OnDisable() => Count--;
}
