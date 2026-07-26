using UnityEngine;

public class AutoAttackController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    private SkillData basicSkill;

    [SerializeField]
    private SkillRunner skillRunner;

    private float attackInterval;
    private float attackRange;
    private Projectile projectilePrefab;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
