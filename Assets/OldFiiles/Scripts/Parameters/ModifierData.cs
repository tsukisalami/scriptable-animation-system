using System.Collections.Generic;
using UnityEngine;

namespace Demo.Scripts.Runtime.AttachmentSystem
{
    public enum ModifierType
    {
        Heat,
        DurabilityBurn,
        Recoil,
        Ergonomics,
        Accuracy,
        MuzzleVelocity,
        HeatSink
        // Add more as needed
    }

    public enum ValueType
    {
        Flat,
        Percentage,
        GameObject
        //Add more as needed
    }

    [System.Serializable]
    public class Modifier
    {
        public ModifierType modifierType;
        public ValueType valueType;
        public float value;
    }

    [CreateAssetMenu(fileName = "AttachmentModifiers", menuName = "Attachments/Modifier")]
    public class ModifierData : ScriptableObject
    {
        public List<Modifier> modifiers = new List<Modifier>();
    }
}
