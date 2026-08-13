using System.Collections.Generic;
using UnityEngine;

public class GrabZone2D : MonoBehaviour
{
    [SerializeField] private BossGrabHandler handler;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool logDebug;

    private readonly HashSet<Collider2D> playerColliders = new HashSet<Collider2D>();

    private void Awake()
    {
        if (handler == null)
            handler = GetComponentInParent<BossGrabHandler>();

        if (handler == null)
            Debug.LogWarning("[GrabZone2D] BossGrabHandler is missing.", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (handler == null || !BelongsToPlayer(other))
            return;

        playerColliders.Add(other);
        handler.SetPlayerInGrabZone(other, true);

        if (logDebug)
            Debug.Log("[GrabZone2D] ENTER: " + other.name, this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (handler == null || !playerColliders.Remove(other))
            return;

        RemoveInvalidColliders();
        handler.SetPlayerInGrabZone(other, playerColliders.Count > 0);

        if (logDebug)
            Debug.Log("[GrabZone2D] EXIT: " + other.name, this);
    }

    private void OnDisable()
    {
        playerColliders.Clear();

        if (handler != null)
            handler.ClearGrabZone();
    }

    private bool BelongsToPlayer(Collider2D other)
    {
        if (other == null)
            return false;

        if (other.CompareTag(playerTag))
            return true;

        Transform root = other.transform.root;
        return root != null && root.CompareTag(playerTag);
    }

    private void RemoveInvalidColliders()
    {
        playerColliders.RemoveWhere(collider => collider == null);
    }
}
