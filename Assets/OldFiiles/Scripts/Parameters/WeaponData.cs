using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon Base Data")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Properties")]
    public float durability;
    public float moaAccuracy;
    public float heat;
    public float ergonomics;
    public float verticalRecoil;
    public float horizontalRecoil;
    // Add other relevant stats as needed

    [Header("Base Multipliers")]
    [Range(0, 1)] public float durabilityBurnPerShot;
    [Range(0, 1)] public float malfunctionChance = 0.01f;
    [Range(0, 1)] public float misfireChance = 0.005f;

}

