using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


#if (!SILANTRO_FIXED)
/// <summary>
/// 
/// </summary>
namespace Oyedoyin.FixedWing.Editors
{
    /// <summary>
    /// 
    /// </summary>
    public class FixedExtraElements
    {
        /// <summary>
        /// 
        /// </summary>
        [MenuItem("Oyedoyin/Fixed Wing/Download", false, 8000)]
        private static void DownloadFW()
        {
            Application.OpenURL("https://assetstore.unity.com/packages/tools/physics/silantro-flight-simulator-toolkit-128025");
        }
    }
}
#endif
