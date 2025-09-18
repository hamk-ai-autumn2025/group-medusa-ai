using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dev.susybaka.TurnBasedGame.Misc
{
	//Source: https://discussions.unity.com/t/cinemachine-confiner-2d-problem/942332
    public class CameraInitializer : MonoBehaviour
    {
        private void OnEnable()
        {
            // Invalidate the confiner cache to ensure it's up to date
            var confiner = GetComponent<Cinemachine.CinemachineConfiner2D>();
            if (confiner != null)
            {
                StartCoroutine(InvalidateConfinerCache(confiner));
            }
        }

        private IEnumerator InvalidateConfinerCache(Cinemachine.CinemachineConfiner2D confiner)
        {
            yield return new WaitForEndOfFrame();
            confiner.InvalidateCache();
        }
    }
}