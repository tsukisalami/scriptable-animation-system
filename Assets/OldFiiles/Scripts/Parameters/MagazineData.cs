using UnityEngine;
using System.Collections.Generic;
using Ballistics;

[CreateAssetMenu(fileName = "MagazineData", menuName = "Magazine Data")]
public class MagazineData : ScriptableObject
{
    [Header("Magazine Properties")]
    public float reloadTime = 2f; // Reload time in seconds
    public float ergonomics = 1f; // Ergonomics value
    public float weight = 1f; // Weight of the magazine
    public BulletCaliber caliber; // Caliber of the bullets in the magazine
    public int ammoCount = 30; // Initial ammo count in the magazine

    [Header("Possible housed bullets")]
    [SerializeField] public List<BulletInfo> bulletTypes = new List<BulletInfo>(); // List of bullet types

}
