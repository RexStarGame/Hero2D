using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BossBehaviorController : MonoBehaviour
{
    public enum BossAttackType
    {
        TelegraphedCharge,
        MultiPhaseCombo,
        StompAoe,
        Feint,
        Grab
    }

    public enum ElementWeakness
    {
        None,
        Fire,
        Ice,
        Lightning
    }

    public enum StatusEffect
    {
        Poison,
        Frost,
        Burn
    }
    private Coroutine attackRoutine;
    private readonly List<BossAttackType> options = new List<BossAttackType>(8);

    [System.Serializable]
    public class TelegraphedChargeSettings
    {
        public bool enabled = true;
        [Tooltip("Hvor lang tid bossen lader op før dash/slag.")]
        public float windupTime = 0.75f;
        [Tooltip("Hvor hurtigt bossen bevæger sig i chargedash.")]
        public float dashSpeed = 8f;
        [Tooltip("Cooldown før angrebet kan vælges igen.")]
        public float cooldown = 4f;
    }

    [System.Serializable]
    public class MultiPhaseComboSettings
    {
        public bool enabled = true;
        [Tooltip("Antal slag i combo (2-4).")]
        public int comboHits = 3;
        [Tooltip("Tid mellem hvert slag.")]
        public float hitInterval = 0.35f;
        [Tooltip("Ekstra rækkevidde/AOE på sidste hit.")]
        public float finisherBonusRange = 1.5f;
        public float cooldown = 3f;
    }

    [System.Serializable]
    public class StompAoeSettings
    {
        public bool enabled = true;
        [Tooltip("Hvor højt bossen hopper (visuelt/timing).")]
        public float jumpTime = 0.5f;
        [Tooltip("Radius på chokbølgen.")]
        public float shockwaveRadius = 4f;
        public float cooldown = 5f;
    }

    [System.Serializable]
    public class FeintSettings
    {
        public bool enabled = true;
        [Tooltip("Hvor lang tid feint 'fakes' før skift til rigtigt angreb.")]
        public float feintTime = 0.35f;
        [Tooltip("Mulighed for at feinten erstatter et andet angreb.")]
        public float replaceChance = 0.4f;
        public float cooldown = 2.5f;
    }

    [System.Serializable]
    public class GrabSettings
    {
        public bool enabled = true;
        [Tooltip("Min. afstand for at bossen vil gribe.")]
        public float grabRange = 1.5f;
        [Tooltip("Hvor længe spilleren skal være tæt på før grab vælges.")]
        public float closeRangeTime = 1f;
        public float cooldown = 6f;
    }

    [System.Serializable]
    public class AggroShiftSettings
    {
        public bool enabled = true;
        [Tooltip("Hvor mange dodges i vinduet før AOE prioriteres.")]
        public int dodgeThreshold = 3;
        [Tooltip("Sekunder der måles dodges over.")]
        public float dodgeWindow = 4f;
        [Tooltip("Hvor meget AOE vægtes ekstra ved mange dodges.")]
        public float aoeWeightBonus = 1.5f;
    }

    [System.Serializable]
    public class TerritoryControlSettings
    {
        public bool enabled = true;
        [Tooltip("Mål-områder bossen forsøger at trække spilleren imod.")]
        public Transform[] controlPoints;
        [Tooltip("Afstand til control point, hvor bossen anses for at have flyttet kampen.")]
        public float controlRadius = 2f;
    }

    [System.Serializable]
    public class ZoneSettings
    {
        public bool enabled = true;
        [Tooltip("Start radius på den sikre zone.")]
        public float safeZoneRadius = 6f;
        [Tooltip("Hvor hurtigt safe zone skrumper pr. sekund.")]
        public float shrinkPerSecond = 0.15f;
    }

    [System.Serializable]
    public class DesperationSettings
    {
        public bool enabled = true;
        [Range(0f, 1f)]
        [Tooltip("HP-procent hvor bossen går berserk.")]
        public float triggerHealthPercent = 0.3f;
        [Tooltip("Hvor meget tempo/angrebscooldowns skaleres.")]
        public float attackSpeedMultiplier = 1.3f;
    }

    [System.Serializable]
    public class StaggerSensitivitySettings
    {
        public bool enabled = true;
        [Tooltip("Antal blocks før ekstra stagger trigger.")]
        public int blocksBeforeStagger = 3;
        [Tooltip("Hvor længe bossen er ekstra stagger-sårbar.")]
        public float staggerWindow = 2f;
    }

    [System.Serializable]
    public class WeaknessSettings
    {
        [Header("Eksponerede faser")]
        public bool exposedPhaseEnabled = true;
        [Tooltip("Hvor længe svagt punkt er eksponeret.")]
        public float exposedDuration = 2f;

        [Header("Overheat")]
        public bool overheatEnabled = true;
        [Tooltip("Antal store angreb før overheat.")]
        public int bigAttacksBeforeOverheat = 3;
        public float overheatDuration = 2.5f;

        [Header("Element svaghed")]
        public bool elementWeaknessEnabled = true;
        public ElementWeakness elementWeakness = ElementWeakness.Ice;
        public float elementDamageMultiplier = 1.5f;

        [Header("Del-bar")]
        public bool partBreakEnabled = true;
        [Tooltip("Navn på kropsdel der kan knuses.")]
        public string breakablePartName = "Arm";
        [Tooltip("Når del er knust, disables et angreb (fx Grab).")]
        public BossAttackType disabledAttackOnBreak = BossAttackType.Grab;

        [Header("Status trigger")]
        public bool statusTriggerEnabled = true;
        public StatusEffect disablesBlockStatus = StatusEffect.Frost;
        public float statusDisableDuration = 2f;
    }

    [Header("Angreb (core kit)")]
    [SerializeField] private TelegraphedChargeSettings telegraphedCharge = new TelegraphedChargeSettings();
    [SerializeField] private MultiPhaseComboSettings multiPhaseCombo = new MultiPhaseComboSettings();
    [SerializeField] private StompAoeSettings stompAoe = new StompAoeSettings();
    [SerializeField] private FeintSettings feint = new FeintSettings();
    [SerializeField] private GrabSettings grab = new GrabSettings();

    [Header("Adfærd")]
    [SerializeField] private AggroShiftSettings aggroShift = new AggroShiftSettings();
    [SerializeField] private TerritoryControlSettings territoryControl = new TerritoryControlSettings();
    [SerializeField] private ZoneSettings zones = new ZoneSettings();
    [SerializeField] private DesperationSettings desperation = new DesperationSettings();
    [SerializeField] private StaggerSensitivitySettings staggerSensitivity = new StaggerSensitivitySettings();

    [Header("Svagheder")]
    [SerializeField] private WeaknessSettings weaknesses = new WeaknessSettings();

    [Header("Smart Chase")]
    [Min(0.1f)] [SerializeField] private float detectionRange = 9f;
    [Min(0.1f)] [SerializeField] private float giveUpRange = 15f;
    [Min(0.1f)] [SerializeField] private float chaseSpeed = 2.5f;
    [Min(0f)] [SerializeField] private float attackEngageRange = 3f;

    [Header("Events (hook til animation/FX)")]
    public UnityEvent onTelegraphedCharge;
    public UnityEvent onMultiPhaseCombo;
    public UnityEvent onStompAoe;
    public UnityEvent onFeint;
    public UnityEvent onGrab;

    private Transform player;
    private EnemyAggro2D aggro;
    private Rigidbody2D rb;
    private bool isAttacking;
    private bool isChasing;
    private float nextAttackTime;
    private readonly Dictionary<BossAttackType, float> cooldowns = new Dictionary<BossAttackType, float>();

    private int dodgeCount;
    private float dodgeWindowTimer;
    private float closeRangeTimer;
    private int blocksTaken;
    private bool isOverheated;
    private bool isWeakPointExposed;
    private bool isPartBroken;
    private bool blockDisabled;
    private float blockDisabledUntil;
    private int bigAttackCounter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        aggro = GetComponent<EnemyAggro2D>();
        if (aggro == null) aggro = gameObject.AddComponent<EnemyAggro2D>();
        aggro.ConfigureRanges(detectionRange, giveUpRange);
    }

    private void Update()
    {
        UpdateTimers();
        UpdateAggroTracking();

        player = aggro != null ? aggro.CurrentTarget : null;

        if (player == null)
        {
            StopChasing();
            return;
        }

        if (isAttacking)
        {
            StopChasing();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer > attackEngageRange)
        {
            isChasing = true;
            if (rb == null && aggro.HasAuthority())
            {
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    player.position,
                    chaseSpeed * Time.deltaTime);
            }
            return;
        }

        StopChasing();

        if (Time.time < nextAttackTime)
            return;

        BossAttackType? nextAttack = SelectNextAttack();
        if (nextAttack.HasValue)
        {
            isAttacking = true; // set BEFORE starting coroutine
            attackRoutine = StartCoroutine(ExecuteAttack(nextAttack.Value));
        }
    }

    private void FixedUpdate()
    {
        if (!isChasing || rb == null || player == null || aggro == null || !aggro.HasAuthority())
            return;

        Vector2 direction = ((Vector2)player.position - rb.position).normalized;
        rb.linearVelocity = direction * chaseSpeed;
    }

    private void StopChasing()
    {
        if (!isChasing) return;

        isChasing = false;
        if (rb != null && aggro != null && aggro.HasAuthority())
            rb.linearVelocity = Vector2.zero;
    }

    private void UpdateTimers()
    {
        if (blockDisabled && Time.time >= blockDisabledUntil)
        {
            blockDisabled = false;
        }
    }

    private void UpdateAggroTracking()
    {
        if (!aggroShift.enabled)
            return;

        if (dodgeCount > 0)
        {
            dodgeWindowTimer += Time.deltaTime;
            if (dodgeWindowTimer >= aggroShift.dodgeWindow)
            {
                dodgeWindowTimer = 0f;
                dodgeCount = 0;
            }
        }
    }

    public void RegisterPlayerDodge()
    {
        if (!aggroShift.enabled)
            return;

        dodgeCount++;
        dodgeWindowTimer = 0f;
    }

    public void RegisterPlayerBlockHit()
    {
        if (!staggerSensitivity.enabled)
            return;

        blocksTaken++;
        if (blocksTaken >= staggerSensitivity.blocksBeforeStagger)
        {
            blocksTaken = 0;
            StartCoroutine(TemporarilyExposeWeakPoint(staggerSensitivity.staggerWindow));
        }
    }

    public void RegisterPartBreak(string partName)
    {
        if (!weaknesses.partBreakEnabled || isPartBroken)
            return;

        if (!string.Equals(partName, weaknesses.breakablePartName))
            return;

        isPartBroken = true;
    }

    public bool IsAttackEnabled(BossAttackType attack)
    {
        switch (attack)
        {
            case BossAttackType.TelegraphedCharge:
                return telegraphedCharge.enabled;
            case BossAttackType.MultiPhaseCombo:
                return multiPhaseCombo.enabled;
            case BossAttackType.StompAoe:
                return stompAoe.enabled;
            case BossAttackType.Feint:
                return feint.enabled;
            case BossAttackType.Grab:
                return grab.enabled && !IsAttackDisabledByPartBreak(BossAttackType.Grab);
            default:
                return false;
        }
    }

    private bool IsAttackDisabledByPartBreak(BossAttackType attack)
    {
        return weaknesses.partBreakEnabled && isPartBroken && weaknesses.disabledAttackOnBreak == attack;
    }

    private BossAttackType? SelectNextAttack()
    {
        options.Clear();

        if (IsAttackReady(BossAttackType.TelegraphedCharge))
            options.Add(BossAttackType.TelegraphedCharge);
        if (IsAttackReady(BossAttackType.MultiPhaseCombo))
            options.Add(BossAttackType.MultiPhaseCombo);
        if (IsAttackReady(BossAttackType.StompAoe))
            options.Add(BossAttackType.StompAoe);
        if (IsAttackReady(BossAttackType.Feint))
            options.Add(BossAttackType.Feint);
        if (IsAttackReady(BossAttackType.Grab) && IsPlayerInGrabRange())
            options.Add(BossAttackType.Grab);

        if (options.Count == 0)
            return null;

        float aoeWeight = 1f;
        if (aggroShift.enabled && dodgeCount >= aggroShift.dodgeThreshold)
            aoeWeight += aggroShift.aoeWeightBonus;

        BossAttackType selected = options[Random.Range(0, options.Count)];
        if (options.Contains(BossAttackType.StompAoe) && Random.value < aoeWeight / (aoeWeight + 1f))
            selected = BossAttackType.StompAoe;

        return selected;
    }


    private bool IsAttackReady(BossAttackType attack)
    {
        if (!IsAttackEnabled(attack))
            return false;

        if (cooldowns.TryGetValue(attack, out float readyTime))
            return Time.time >= readyTime;

        return true;
    }

    private bool IsPlayerInGrabRange()
    {
        if (player == null)
            return false;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= grab.grabRange)
        {
            closeRangeTimer += Time.deltaTime;
        }
        else
        {
            closeRangeTimer = 0f;
        }

        return closeRangeTimer >= grab.closeRangeTime;
    }

    private IEnumerator ExecuteAttack(BossAttackType attack)
    {
        //isAttacking = true;
        float attackSpeed = desperation.enabled ? desperation.attackSpeedMultiplier : 1f;

        switch (attack)
        {
            case BossAttackType.TelegraphedCharge:
                onTelegraphedCharge?.Invoke();
                yield return new WaitForSeconds(telegraphedCharge.windupTime / attackSpeed);
                break;
            case BossAttackType.MultiPhaseCombo:
                onMultiPhaseCombo?.Invoke();
                for (int i = 0; i < multiPhaseCombo.comboHits; i++)
                {
                    yield return new WaitForSeconds(multiPhaseCombo.hitInterval / attackSpeed);
                }
                break;
            case BossAttackType.StompAoe:
                onStompAoe?.Invoke();
                yield return new WaitForSeconds(stompAoe.jumpTime / attackSpeed);
                break;
            case BossAttackType.Feint:
                onFeint?.Invoke();
                yield return new WaitForSeconds(feint.feintTime / attackSpeed);
                break;
            case BossAttackType.Grab:
                onGrab?.Invoke();
                yield return new WaitForSeconds(0.2f / attackSpeed);
                break;
        }

        ApplyWeaknessWindows(attack);
        SetAttackCooldown(attack);
        nextAttackTime = Time.time + 0.2f;
        isAttacking = false;
        attackRoutine = null;

    }

    private void ApplyWeaknessWindows(BossAttackType attack)
    {
        if (weaknesses.exposedPhaseEnabled)
        {
            StartCoroutine(TemporarilyExposeWeakPoint(weaknesses.exposedDuration));
        }

        if (weaknesses.overheatEnabled && (attack == BossAttackType.TelegraphedCharge || attack == BossAttackType.StompAoe))
        {
            bigAttackCounter++;
            if (bigAttackCounter >= weaknesses.bigAttacksBeforeOverheat)
            {
                bigAttackCounter = 0;
                StartCoroutine(TemporarilyOverheat(weaknesses.overheatDuration));
            }
        }
    }

    private void SetAttackCooldown(BossAttackType attack)
    {
        float cooldown = attack switch
        {
            BossAttackType.TelegraphedCharge => telegraphedCharge.cooldown,
            BossAttackType.MultiPhaseCombo => multiPhaseCombo.cooldown,
            BossAttackType.StompAoe => stompAoe.cooldown,
            BossAttackType.Feint => feint.cooldown,
            BossAttackType.Grab => grab.cooldown,
            _ => 1f
        };

        if (desperation.enabled)
            cooldown /= Mathf.Max(0.1f, desperation.attackSpeedMultiplier);

        cooldowns[attack] = Time.time + cooldown;
    }

    private IEnumerator TemporarilyExposeWeakPoint(float duration)
    {
        if (!weaknesses.exposedPhaseEnabled)
            yield break;

        isWeakPointExposed = true;
        yield return new WaitForSeconds(duration);
        isWeakPointExposed = false;
    }

    private IEnumerator TemporarilyOverheat(float duration)
    {
        isOverheated = true;
        yield return new WaitForSeconds(duration);
        isOverheated = false;
    }

    public bool IsWeakPointExposed()
    {
        return isWeakPointExposed || isOverheated;
    }

    public float GetElementDamageMultiplier(ElementWeakness element)
    {
        if (!weaknesses.elementWeaknessEnabled)
            return 1f;

        return weaknesses.elementWeakness == element ? weaknesses.elementDamageMultiplier : 1f;
    }

    public void ApplyStatusEffect(StatusEffect effect)
    {
        if (!weaknesses.statusTriggerEnabled)
            return;

        if (effect == weaknesses.disablesBlockStatus)
        {
            blockDisabled = true;
            blockDisabledUntil = Time.time + weaknesses.statusDisableDuration;
        }
    }
    private void OnDisable()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        StopChasing();
        isAttacking = false;
    }
    public bool IsBlockDisabled()
    {
        if (!weaknesses.statusTriggerEnabled)
            return false;

        return blockDisabled;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0.2f, 0.15f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, giveUpRange);

#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(1f, 0.75f, 0.1f, 1f);
        UnityEditor.Handles.Label(transform.position + Vector3.right * detectionRange, "CHASE START");
        UnityEditor.Handles.color = new Color(1f, 0.2f, 0.15f, 1f);
        UnityEditor.Handles.Label(transform.position + Vector3.right * giveUpRange, "CHASE STOPS");
#endif
    }
}
