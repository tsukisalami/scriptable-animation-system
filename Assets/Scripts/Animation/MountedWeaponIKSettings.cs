using KINEMATION.FPSAnimationFramework.Runtime.Layers.IkLayer;
using KINEMATION.KAnimationCore.Runtime.Input;
using UnityEngine;

[CreateAssetMenu(menuName = "FPS Animation Framework/Layers/Mounted Weapon IK Settings")]
public class MountedWeaponIKSettings : IkLayerSettings
{
    public new string rightHandTarget = "RightHandTarget";
    public new string leftHandTarget = "LeftHandTarget";
    public new bool attachHands = true;
} 