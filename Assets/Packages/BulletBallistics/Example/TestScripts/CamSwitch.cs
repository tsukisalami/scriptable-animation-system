using UnityEngine;

namespace Ballistics
{
    public class CamSwitch : MonoBehaviour
    {
        public Camera TopDownCam;
        private bool isTopDown = false;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab)) {
                isTopDown = !isTopDown;
                TopDownCam.enabled = isTopDown;
            }
        }
    }
}