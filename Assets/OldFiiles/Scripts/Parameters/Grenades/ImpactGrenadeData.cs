using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ImpactGrenadeData", menuName = "Throwable Data/Impact Grenade")]
public class ImpactGrenadeData : ScriptableObject
{
    public float weight = 0.15f;
    public float explosionRadius = 10f;
}
