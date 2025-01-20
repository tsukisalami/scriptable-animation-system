using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "FlashbangGrenadeData", menuName = "Throwable Data/Flashbang Grenade")]
public class FlashbangGrenadeData : ScriptableObject
{
    public float weight = 0.15f;
    public float flashDuration = 5f;
    public float flashRange = 20f;
}
