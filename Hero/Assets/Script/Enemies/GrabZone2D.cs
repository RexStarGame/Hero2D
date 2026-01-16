using UnityEngine;

public class GrabZone2D : MonoBehaviour
{
    [SerializeField] private BossGrabHandler handler;
    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        if (handler == null)
            handler = GetComponentInParent<BossGrabHandler>();

        Debug.Log($"[GrabZone2D] Awake. handler={(handler ? handler.name : "NULL")}  on={name}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (handler == null)
        {
            Debug.LogWarning("[GrabZone2D] handler is NULL on trigger enter!");
            return;
        }

        if (other.CompareTag(playerTag))
        {
            Debug.Log("[GrabZone2D] ENTER: " + other.name);
            handler.SetPlayerInGrabZone(other, true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (handler == null) return;

        if (other.CompareTag(playerTag))
        {
            Debug.Log("[GrabZone2D] EXIT: " + other.name);
            handler.SetPlayerInGrabZone(other, false);
        }
    }
}
