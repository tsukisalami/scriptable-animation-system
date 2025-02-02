using UnityEngine;

namespace Ballistics
{
    /// <summary>
    /// Attribute that conditionally shows/hides a field in the inspector based on the value of another field
    /// </summary>
    public class ShowIfAttribute : PropertyAttribute
    {
        public string ConditionalSourceField;

        public ShowIfAttribute(string conditionalSourceField)
        {
            ConditionalSourceField = conditionalSourceField;
        }
    }
} 