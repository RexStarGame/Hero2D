using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossStompAoeTelegraph2D : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private BossAttackAnimation bossAnim;
    [Header("Layer Rules")]
    [SerializeField] private LayerMask damageLayers; // vælg Player-layer (eller din hitbox-layer) i Inspector

    [Header("Target")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask hitMask;
    [Tooltip("Auto-destroy impact instance after this many seconds (0 disables).")]
    [SerializeField] private float impactLifetime = 2.5f;
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

    [Header("Telegraph Fire (inside circle)")]
    [SerializeField] private GameObject telegraphFirePrefab; // ParticleSystem prefab (looping)
    [SerializeField] private int fireSortingOffset = -2;     // bag ring/fill
    [SerializeField] private bool fireSimulationLocal = true;

    private bool running;
    private EnemyDifficultyProfile difficultyProfile;

    // For prediction fallback if player has no Rigidbody2D velocity:
    private Vector2 lastPlayerPos;
    private Vector2 approxVelocity;

    private void Awake()
    {
        difficultyProfile = GetComponentInParent<EnemyDifficultyProfile>();

        // Cache animation component (optional if set in Inspector)
        if (bossAnim == null)
            bossAnim = GetComponentInChildren<BossAttackAnimation>(true);

        EnsurePlayer();

        if (player != null)
            lastPlayerPos = player.position;

        if (logDebug)
        {
            Debug.Log(
                $"[AOE] Awake on {name}. player={(player ? player.name : "NULL")} " +
                $"primaryPrefab={(telegraphPrimaryPrefab ? telegraphPrimaryPrefab.name : "NULL")} " +
                $"bossAnim={(bossAnim ? bossAnim.name : "NULL")}"
            );
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

        // Trigger attack animation once for the whole combo
        bossAnim?.PlayAttack();

        StartCoroutine(StompRoutine());
    }


    private void EnsurePlayer()
    {
        if (player != null) return;

        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null)
        {
            player = p.transform;
            lastPlayerPos = player.position;
            approxVelocity = Vector2.zero;
        }
    }


    private IEnumerator StompRoutine()
    {
        running = true;

        GameObject currentTele = null;
        GameObject nextTele = null;

        try
        {
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
            currentTele = SpawnTelegraph(
                strikes[0],
                telegraphPrimaryPrefab,
                radius,
                Mathf.Max(0.01f, GetHitTimeAbs(0) - Time.time)
            );

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

                yield return new WaitForSeconds(warningTime);

                DoHit(strikes[i]);

                // Remove the telegraph that just hit
                if (currentTele != null) Destroy(currentTele);

                // Shift "next" telegraph to become "current"
                currentTele = nextTele;
                nextTele = null;

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

            if (logDebug) Debug.Log("[AOE] PlayStompAoe END");
        }
        finally
        {
            // Always reset state + animation, even if something interrupts the coroutine
            running = false;
            bossAnim?.PlayIdle();

            // Safety: if coroutine ends early, clean leftovers
            if (currentTele != null) Destroy(currentTele);
            if (nextTele != null) Destroy(nextTele);
        }
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
    private void SpawnFireAtHit(Vector2 center, float r)
    {
        if (telegraphFirePrefab == null) return;

        GameObject fireGo = Instantiate(telegraphFirePrefab, new Vector3(center.x, center.y, 0f), Quaternion.identity);

        var ps = fireGo.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.simulationSpace = fireSimulationLocal
                ? ParticleSystemSimulationSpace.Local
                : ParticleSystemSimulationSpace.World;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = r;
            shape.radiusThickness = 1f; // ✅ fyld hele området

            ps.Play(true);

            Destroy(fireGo, Mathf.Max(0.5f, main.duration + main.startLifetime.constantMax));
        }
        else
        {
            Destroy(fireGo, 1.5f);
        }

        var psr = fireGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.sortingOrder = telegraphSortingOrder + fireSortingOffset;

            // match sorting layer (valgfrit)
            var anySR = GetComponentInChildren<SpriteRenderer>(true);
            if (anySR != null)
                psr.sortingLayerID = anySR.sortingLayerID;
        }
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

        // 🔥 VFX ved hit
        SpawnFireAtHit(center, radius);

        if (impactPrefab != null)
        {
            GameObject impactInstance = Instantiate(
                impactPrefab,
                new Vector3(center.x, center.y, 0f),
                Quaternion.identity
            );

            if (impactLifetime > 0f)
                Destroy(impactInstance, impactLifetime);
        }

        // ✅ Samme metode som EnemyBullet: OverlapCircleAll + damageLayers
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, damageLayers);

        if (logDebug) Debug.Log($"[AOE] Overlap hits={hits.Length}");

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D other = hits[i];
            if (other == null) continue;

            // ✅ Samme som i EnemyBullet (finder PlayerHealth på parent)
            var hp = other.GetComponentInParent<PlayerHealth>();
            if (hp == null) continue;

            // Damage (kun én gang)
            if (difficultyProfile == null)
                difficultyProfile = GetComponentInParent<EnemyDifficultyProfile>();
            float scaledDamage = difficultyProfile != null
                ? difficultyProfile.ScaleDamage(damage)
                : damage * EnemyDifficultyProfile.GetDefaultDamageMultiplier();
            DifficultyDebugTelemetry.RecordEnemyDamage(
                this, damage, scaledDamage);
            hp.TakeDamage(scaledDamage);

            // Knockback (brug rb på parent hvis child collider ikke har rb)
            if (applyKnockback)
            {
                Rigidbody2D rb = other.attachedRigidbody != null
                    ? other.attachedRigidbody
                    : other.GetComponentInParent<Rigidbody2D>();

                if (rb != null)
                {
                    Vector2 dir = ((Vector2)hp.transform.position - center);
                    if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
                    rb.AddForce(dir.normalized * knockbackForce, ForceMode2D.Impulse);
                }
            }

            if (logDebug) Debug.Log($"[AOE] Damaged player for {damage}");
            break; // stop så du ikke rammer flere gange pga flere colliders
        }
    }
}
