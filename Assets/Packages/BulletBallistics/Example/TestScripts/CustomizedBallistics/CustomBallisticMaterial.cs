using Ballistics;
using Unity.Mathematics;
using UnityEngine;

// example of a custom ballistic material.
// Sadly, without any of the custom inspector magic (so far..).

// ! IMPORTANT !
// Instances have to be referenced somewhere in the scene, or moved to a 'Resource' folder, so it is included in the build!

[CreateAssetMenu(fileName = "CustomBallisticMaterial", menuName = "Ballistics/Demo/Custom BallisticMaterial Example")]
public class CustomBallisticMaterial : InitializableScriptableObject, IBallisticMaterial
{
    [Header("PhysicMaterial")]
    public PhysicsMaterial Target;

    [Header("Impact")]
    [Tooltip("Energyloss of a bullet penetrating through 1 unit of this material")]
    public float EnergyLossPerUnit = 1000;

    [Tooltip("Spread angle when the bullet exits this material")]
    [PhysicalUnit(PhysicalType.ANGLE)] public float Spread = .01f;

    [Tooltip("Handler for bullet interactions with this material (can be null)")]
    public ImpactHandlerObject ImpactHandler;

    public override void Initialize() // called on game start.
    {
        BallisticMaterialCache.Add(Target, this);   // associate the assigned PhysicMaterial with this BallisticMaterial
    }

    public MaterialImpact HandleImpact(in BulletNative bullet, BulletInfo info, in RaycastHit rayHit)
    {
        // your custom impact logic here

        Debug.Log("Hit custom ballistic material. " + name);

        return MaterialImpact.Ignore();
    }

    public float GetEnergyLossPerUnit()
    {
        return EnergyLossPerUnit;
    }

    public float GetSpreadAngle()
    {
        return Spread;
    }

    public IImpactHandler GetImpactHandler()
    {
        return ImpactHandler;
    }
}
