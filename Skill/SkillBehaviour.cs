using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using StarCloudgamesLibrary;
using UnityEngine;

[RequireComponent(typeof(FloatingTextController))]
public class SkillBehaviour : MonoBehaviour
{
    public bool moveForward;
    public float moveSpeed;

    private HashSet<EnemyCharacter> detectedEnemies;
    private List<Collider2D> areaDetectedEnemies;
    private ContactFilter2D areaContactFilter;

    private SkillStatScriptable skillStat;
    private FloatingTextController floatingTextController;
    private Collider2D skillCollider;

    private bool initialized;
    private Coroutine moveCoroutine;
    private LayerMask enemyLayerMask;

    private readonly float disableTimer = 10.0f;

    private readonly float damageTextDelay = 0.1f;

    #region "Initialize"

    private void Initialize()
    {
        initialized = true;
        floatingTextController = GetComponent<FloatingTextController>();
        enemyLayerMask = LayerMask.GetMask("Enemy");
        skillCollider = GetComponent<Collider2D>();

        areaContactFilter = new ContactFilter2D()
        {
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Enemy"),
            useTriggers = true
        };
    }

    #endregion

    #region "Set Up"

    public void SetUp(SkillStatScriptable targetSkillStat, Transform player, Transform targetEnemy)
    {
        if(!initialized) Initialize();

        if(moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        skillStat = targetSkillStat;
        detectedEnemies = new HashSet<EnemyCharacter>();
        areaDetectedEnemies = new List<Collider2D>();

        SetUpTransform(player, targetEnemy);

        gameObject.SetActive(true);

        if(moveForward)
        {
            moveCoroutine ??= StartCoroutine(MoveForwardCoroutine());
        }
        else
        {
            if(moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }

            DetectEnemyInArea();
        }
    }

    private void SetUpTransform(Transform player, Transform target)
    {
        var direction = target.position - player.position;
        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        transform.position = player.position;
    }

    #endregion

    #region "Move"

    private IEnumerator MoveForwardCoroutine()
    {
        var currentTimer = 0.0f;

        while(gameObject.activeSelf || currentTimer < disableTimer)
        {
            transform.position += transform.right * Time.deltaTime * moveSpeed;
            currentTimer += Time.deltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
    }

    #endregion

    #region "Detect Enemy"

    private void DetectEnemyInArea()
    {
        skillCollider.OverlapCollider(areaContactFilter, areaDetectedEnemies);

        foreach(var areaDetectedEnemy in areaDetectedEnemies)
        {
            if(areaDetectedEnemy.TryGetComponent<EnemyCharacter>(out var enemy) && !enemy.IsAlive()) continue;
            StartCoroutine(GiveDamageCoroutine(enemy));
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if(!moveForward) return;

        if(((1 << collision.gameObject.layer) & enemyLayerMask) != 0 && collision.TryGetComponent<EnemyCharacter>(out var enemy) && !detectedEnemies.Contains(enemy) && enemy.IsAlive())
        {
            detectedEnemies.Add(enemy);
            StartCoroutine(GiveDamageCoroutine(enemy));
        }
    }

    #endregion

    #region "Damage"

    private IEnumerator GiveDamageCoroutine(EnemyCharacter enemy)
    {
        var textList = new List<KeyValuePair<string, bool>>();

        for(int i = 0; i < skillStat.attackCount; i++)
        {
            var damage = CalculateDamage(enemy, out var critical);

            if(enemy.IsAlive())
            {
                enemy.GetDamage(damage);
            }

            textList.Add(new KeyValuePair<string, bool>(damage.ToCurrencyString(), critical));
        }

        foreach(var textData in textList)
        {
            floatingTextController.SetFloatingText(textData.Key, enemy.transform.position, textData.Value);
            yield return Yielder.WaitForSeconds(damageTextDelay);
        }
    }

    private double CalculateDamage(EnemyCharacter targetEnemy, out bool critical)
    {
        var damage = StatManager.instance.GetFinalSkillAttackDamage(out var isCritical);
        var bonusStat = StatManager.instance.GetStat(ScriptableStatType.None, targetEnemy.bonusDamageStatType);
        critical = isCritical;

        damage = damage * (1 + bonusStat / 100.0d);
        
        return damage;
    }

    #endregion
}