using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "FragGrenadeData", menuName = "Throwable Data/Frag Grenade")]
public class FragGrenadeData : ScriptableObject
{
    public float weight = 0.15f;
    public float fuseTime = 5f;
    public float explosionRadius = 10f;
    public float explosionForce = 10f;
    public GameObject explosionEffect;
    public int numberOfFragments = 100;
    public List<Transform> fragmentSpawns = new List<Transform>();
}
