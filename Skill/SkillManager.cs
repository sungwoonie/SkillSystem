using System.Collections;
using System.Collections.Generic;
using StarCloudgamesLibrary;
using UnityEngine;

public class SkillManager : SingleTon<SkillManager>
{
    [SerializeField] private int poolCount;
    [SerializeField] private List<SkillBehaviour> skillEffectPrefabs;
    private Dictionary<string, ObjectPooling<SkillBehaviour>> skillEffectPools;

    #region "Unity"

    protected override void Awake()
    {
        base.Awake();

        Initialize();
    }

    #endregion

    #region "Initialize"

    private void Initialize()
    {
        skillEffectPools = new Dictionary<string, ObjectPooling<SkillBehaviour>>();

        foreach(var skillEffectPrefab in skillEffectPrefabs)
        {
            var newObjectPooling = new ObjectPooling<SkillBehaviour>(skillEffectPrefab, poolCount, transform);
            skillEffectPools[skillEffectPrefab.name] = newObjectPooling;
        }
    }

    #endregion

    #region "Get"

    public SkillBehaviour GetSkillEffect(string skillName)
    {
        if(skillEffectPools.TryGetValue(skillName, out var pool))
        {
            return pool.GetPool();
        }

        return null;
    }

    #endregion

    #region "Use Skill"

    public bool UseSkill(SkillStatScriptable skillStat)
    {
        var skillEffect = GetSkillEffect(skillStat.effectName);
        if(skillEffect == null)
        {
            DebugManager.DebugInGameWarningMessage($"{skillStat.effectName} is not exist");
            return false;
        }

        var targetEnemy = StageManager.instance.GetRandomSpawnedEnemy();
        if(targetEnemy == null) return false;

        skillEffect.SetUp(skillStat, PlayableCharacter.instance.transform, targetEnemy.transform);

        return true;
    }

    #endregion
}