using UnityEngine;

public class CannonController : MonoBehaviour
{
    [Header("Bullet")]
    [SerializeField, Tooltip("")]
    private Bullet bulletPrefab;
    [SerializeField, Tooltip("")]
    private Transform firePoint;
    [SerializeField, Tooltip("")]
    private float bulletSpeed = 20f;

    [Header("Upgrade Settings")]
    [SerializeField, Tooltip("")]
    private int maxLevel = 3;

    [Header("Base Stats (level 0)")]
    [SerializeField, Tooltip("")]
    private float baseFireCooldown = 2f;
    [SerializeField, Tooltip("")]
    private float baseBulletRange = 8f;
    [SerializeField, Tooltip("")]
    private float baseBulletCurveSpeed = 60f;

    [Header("Per-Level Bonus")]
    [SerializeField, Tooltip("")]
    private float fireCooldownReductionPerLevel = 0.4f;
    [SerializeField, Tooltip("")]
    private float bulletRangeBonusPerLevel = 3f;
    [SerializeField, Tooltip("")]
    private float bulletCurveSpeedBonusPerLevel = 40f;

    public int CurrentLevel { get; private set; }
    public bool CanUpgrade => CurrentLevel < maxLevel;

    private float _nextFireTime;

    public bool TryShoot()
    {
        if (Time.time < _nextFireTime)
        {
            return false;
        }

        float cooldown = baseFireCooldown - (fireCooldownReductionPerLevel * CurrentLevel);
        float range = baseBulletRange + (bulletRangeBonusPerLevel * CurrentLevel);
        float curveSpeed = baseBulletCurveSpeed + (bulletCurveSpeedBonusPerLevel * CurrentLevel);

        Vector3 direction = firePoint.forward;
        direction.y = 0f;
        direction.Normalize();

        Bullet bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(direction));
        bullet.Shoot(direction, bulletSpeed, range, curveSpeed);

        _nextFireTime = Time.time + cooldown;
        return true;
    }

    public bool TryUpgrade(CharacterHealth chestHealth, int hpCost)
    {
        if (CanUpgrade == false)
        {
            return false;
        }

        if (chestHealth.CurrentHealth <= hpCost)
        {
            return false;
        }

        chestHealth.TakeDamage(hpCost);
        CurrentLevel++;
        return true;
    }
}