using UnityEngine;

[CreateAssetMenu(fileName = "New Bullet", menuName = "Bullet Parameters")]
public class BulletParameters : ScriptableObject
{
    public string bulletName;
    public float velocity; // Bullet velocity in meters per second
    public int penetration; // Penetration value (0-100)
    public int baseDamage; // Base damage value (0-200)
    public BulletCaliber caliber; // Bullet caliber (enum)
    public float lifeTime; // Bullet life time in seconds
    public GameObject bulletPrefab; // Prefab object for this bullet
    // Add more parameters as needed
}

public enum BulletCaliber
{
    Pistol, // Example calibers
    Rifle,
    Shotgun
    // Add more calibers as needed
}
