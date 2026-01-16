using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossStompAoeTelegraph2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask hitMask;

    [Header("Telegraph Prefabs")]
    [SerializeField] private GameObject telegraphPrimaryPrefab;
    [SerializeField] private GameObject telegraphNextPrefab;
    [SerializeField] private GameObject impactPrefab;

    [Header("AOE Settings")]
    [SerializeField] private int strikeCount = 3;
    [SerializeField] private float radius = 3.5f;
    [SerializeField] private float warningTime = 0.7f;
    [SerializeField] private float timeBetweenStrikes = 0.15f;

    [Header("Prediction")]
    [SerializeField] private bool usePrediction = true;
    [SerializeField] private float leadTime = 0.35f;
    [SerializeField] private float maxPredictDistance = 2.0f;

    [Header("Damage + Knockback")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private bool applyKnockback = true;
    [SerializeField] private float knockbackForce = 8f;

    [Header("Debug / Test")]
    [SerializeField] private bool logDebug = true;
    [SerializeField] private bool testWithKey = true;
    [SerializeField] private KeyCode testKey = KeyCode.T;

    [Header("Telegraph Visual Overrides")]
    [SerializeField] private int telegraphSortingOrder = 999;
    [SerializeField] private int lineSegments = 64;

    [Header("Fill (Knob/Circle)")]
    [Tooltip("Child name in prefab that holds the filled sprite. Recommended: 'Fill'.")]
    [SerializeField] private string fillChildName = "Fill";

    [Tooltip("Manual tweak for fill size. 1 = matches ring radius. 0.9 smaller. 1.2 bigger.")]
    [SerializeField] private float fillScaleMultiplier = 1f;

    [Tooltip("Fill sorting order relative to ring. -1 means fill behind ring.")]
    [SerializeField] private int fillSortingOffset = -1;

    private bool running;

    // For prediction fallback if player has no Rigidbody2D velocity:
    private Vector2 lastPlayerPos;
    private Vector2 approxVelocity;

    private void Awake()
    {
        EnsurePlayer();

        if (player != null) lastPlayerPos = player.position;

        if (logDebug)
        {
            Debug.Log($"[AOE] Awake on {name}. player={(player ? player.name : "NULL")} " +
                      $"primaryPrefab={(telegraphPrimaryPrefab ? telegraphPrimaryPrefab.name : "NULL")}");
        }
    }

    private void Update()
    {
        if (testWithKey && Input.GetKeyDown(testKey))
        {
            if (logDebug) Debug.Log("[AOE] Test key pressed -> PlayStompAoe()");
            PlayStompAoe();
        }

        if (player == null) return;

        Vector2 current = player.position;
        float dt = Time.deltaTime;
        if (dt > 0f) approxVelocity = (current - lastPlayerPos) / dt;
        lastPlayerPos = current;
    }

    // Hook this to BossBehaviorController.onStompAoe
    public void PlayStompAoe()
    {
        if (running)
        {
            if (logDebug) Debug.Log("[AOE] PlayStompAoe ignored (already running)");
            return;
        }

        EnsurePlayer();

        if (player == null)
        {
            Debug.LogWarning("[AOE] No player found (tag mismatch?)");
            return;
        }

        if (telegraphPrimaryPrefab == null)
        {
            Debug.LogWarning("[AOE] telegraphPrimaryPrefab is NULL (assign in inspector)");
            return;
        }

        if (strikeCount <= 0)
        {
            Debug.LogWarning("[AOE] strikeCount <= 0");
            return;
        }

        if (logDebug) Debug.Log("[AOE] PlayStompAoe START");
        StartCoroutine(StompRoutine());
    }

    private void EnsurePlayer()
    {
        if (player != null) return;

        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) player = p.transform;
    }

    private IEnumerator StompRoutine()
    {
        running = true;

        float startTime = Time.time;

        float GetHitTimeAbs(int idx)
        {
            // Absolute hit time for strike idx
            return startTime + warningTime * (idx + 1) + timeBetweenStrikes * idx;
        }

        // Precompute strike positions (so we can show "next" early)
        List<Vector2> strikes = new List<Vector2>(strikeCount);
        for (int i = 0; i < strikeCount; i++)
            strikes.Add(GetStrikePosition(i));

        // Spawn first + next warning immediately (with correct time-to-hit)
        GameObject currentTele = SpawnTelegraph(
            strikes[0],
            telegraphPrimaryPrefab,
            radius,
            Mathf.Max(0.01f, GetHitTimeAbs(0) - Time.time)
        );

        GameObject nextTele = null;
        if (strikeCount > 1)
        {
            GameObject nextPrefab = telegraphNextPrefab != null ? telegraphNextPrefab : telegraphPrimaryPrefab;
            nextTele = SpawnTelegraph(
                strikes[1],
                nextPrefab,
                radius,
                Mathf.Max(0.01f, GetHitTimeAbs(1) - Time.time)
            );
        }

        for (int i = 0; i < strikeCount; i++)
        {
            if (logDebug) Debug.Log($"[AOE] Strike {i + 1}/{strikeCount} warning for {warningTime}s");

            // Wait warning time then hit
            yield return new WaitForSeconds(warningTime);

            DoHit(strikes[i]);

            // Remove the telegraph that just hit
            if (currentTele != null) Destroy(currentTele);

            // Shift "next" telegraph to become "current"
            currentTele = nextTele;
            nextTele = null;

            // Spawn the next-next telegraph early (with correct time-to-hit)
            int nextIndex = i + 2;
            if (nextIndex < strikeCount)
            {
                GameObject nextPrefab = telegraphNextPrefab != null ? telegraphNextPrefab : telegraphPrimaryPrefab;
                nextTele = SpawnTelegraph(
                    strikes[nextIndex],
                    nextPrefab,
                    radius,
                    Mathf.Max(0.01f, GetHitTimeAbs(nextIndex) - Time.time)
                );
            }

            if (i < strikeCount - 1)
                yield return new WaitForSeconds(timeBetweenStrikes);
        }

        if (currentTele != null) Destroy(currentTele);
        if (nextTele != null) Destroy(nextTele);

        running = false;
        if (logDebug) Debug.Log("[AOE] PlayStompAoe END");
    }

    private Vector2 GetStrikePosition(int index)
    {
        Vector2 p = player.position;

        if (!usePrediction) return p;

        Vector2 v = Vector2.zero;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            v = rb.linearVelocity;
#else
            v = rb.velocity;
#endif
        }
        else
        {
            v = approxVelocity;
        }

        // Each next strike leads a bit further
        float t = leadTime + (index * 0.12f);

        Vector2 predicted = p + v * t;

        Vector2 delta = predicted - p;
        if (delta.magnitude > maxPredictDistance)
            predicted = p + delta.normalized * maxPredictDistance;

        return predicted;
    }

    private GameObject SpawnTelegraph(Vector2 pos, GameObject prefab, float r, float timeToHit)
    {
        if (prefab == null) return null;

        Vector3 spawnPos = new Vector3(pos.x, pos.y, 0f);
        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Find renderers
        var lr = go.GetComponentInChildren<LineRenderer>(true);
        var allSR = go.GetComponentsInChildren<SpriteRenderer>(true);

        // Sorting for all sprites (so fill + other sprites don't hide)
        if (allSR != null)
        {
            for (int i = 0; i < allSR.Length; i++)
            {
                if (allSR[i] != null)
                    allSR[i].sortingOrder = telegraphSortingOrder;
            }
        }

        if (lr != null)
        {
            lr.sortingOrder = telegraphSortingOrder;
            DrawCircleOnLineRenderer(lr, r);

            // Important: avoid scaling line circle unintentionally
            go.transform.localScale = Vector3.one;

            // Scale Fill sprite to match radius (works regardless of Pixels Per Unit)
            ApplyFillSizing(go, r);
        }
        else
        {
            // Sprite-only: scale whole object to diameter
            float d = r * 2f;
            go.transform.localScale = new Vector3(d, d, 1f);
        }

        // Blink / color timing (optional)
        var pulse = go.GetComponentInChildren<TelegraphPulse2D>(true);
        if (pulse != null)
            pulse.Init(timeToHit);

        if (logDebug) Debug.Log($"[AOE] SpawnTelegraph {prefab.name} at {spawnPos} timeToHit={timeToHit:0.00}s");
        return go;
    }

    private void ApplyFillSizing(GameObject telegraphInstance, float r)
    {
        // Find Fill object
        Transform fillT = FindDeepChild(telegraphInstance.transform, fillChildName);

        SpriteRenderer fillSR = null;

        if (fillT != null)
            fillSR = fillT.GetComponent<SpriteRenderer>();

        // Fallback: pick a SpriteRenderer that is NOT on the same object as the LineRenderer
        if (fillSR == null)
        {
            var srs = telegraphInstance.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < srs.Length; i++)
            {
                if (srs[i] == null) continue;
                if (srs[i].GetComponent<LineRenderer>() != null) continue;
                fillSR = srs[i];
                break;
            }
        }

        if (fillSR == null || fillSR.sprite == null)
            return;

        // Center it
        fillSR.transform.localPosition = Vector3.zero;

        // Compute scale based on sprite's bounds in units at scale 1
        float baseDiameter = Mathf.Max(0.0001f, fillSR.sprite.bounds.size.x); // assumes round sprite
        float targetDiameter = r * 2f * Mathf.Max(0.01f, fillScaleMultiplier);

        float scale = targetDiameter / baseDiameter;
        fillSR.transform.localScale = new Vector3(scale, scale, 1f);

        // Put fill behind ring
        fillSR.sortingOrder = telegraphSortingOrder + fillSortingOffset;
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null) return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform c = parent.GetChild(i);
            if (c.name == name) return c;

            Transform found = FindDeepChild(c, name);
            if (found != null) return found;
        }
        return null;
    }

    private void DrawCircleOnLineRenderer(LineRenderer lr, float r)
    {
        lr.useWorldSpace = false;
        int seg = Mathf.Max(8, lineSegments);
        lr.positionCount = seg + 1;

        for (int i = 0; i <= seg; i++)
        {
            float t = (float)i / seg;
            float ang = t * Mathf.PI * 2f;
            float x = Mathf.Cos(ang) * r;
            float y = Mathf.Sin(ang) * r;
            lr.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    private void DoHit(Vector2 center)
    {
        if (logDebug) Debug.Log($"[AOE] HIT at {center}");

        if (impactPrefab != null)
            Instantiate(impactPrefab, new Vector3(center.x, center.y, 0f), Quaternion.identity);

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = hitMask;
        filter.useTriggers = true;

        List<Collider2D> results = new List<Collider2D>(16);
        Physics2D.OverlapCircle(center, radius, filter, results);

        for (int i = 0; i < results.Count; i++)
        {
            Collider2D c = results[i];
            if (c == null) continue;
            if (!c.CompareTag(playerTag)) continue;

            c.gameObject.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

            if (applyKnockback)
            {
                Rigidbody2D rb = c.attachedRigidbody;
                if (rb != null)
                {
                    Vector2 dir = ((Vector2)c.transform.position - center);
                    if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
                    dir = dir.normalized;
                    rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
                }
            }
        }
    }
}
