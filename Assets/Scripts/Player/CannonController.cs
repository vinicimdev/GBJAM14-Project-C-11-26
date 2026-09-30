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

    [Header("Ammo Settings")]
    [SerializeField, Tooltip("")]
    private int maxAmmo = 2;
    [SerializeField, Tooltip("")]
    private int startingAmmo = 2;

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
    public int CurrentAmmo { get; private set; }
    public int MaxAmmo => maxAmmo;
    public bool NeedsReload => CurrentAmmo < maxAmmo;

    private float _nextFireTime;

    private void Awake()
    {
        CurrentAmmo = Mathf.Min(startingAmmo, maxAmmo);
    }

    public bool TryShoot()
    {
        if (Time.time < _nextFireTime)
        {
            return false;
        }

        if (CurrentAmmo <= 0)
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

        CurrentAmmo--;
        _nextFireTime = Time.time + cooldown;
        return true;
    }

    public int Reload()
    {
        int added = maxAmmo - CurrentAmmo;
        CurrentAmmo = maxAmmo;
        return added;
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
