using UnityEngine;
using UnityEngine.SceneManagement;

namespace dev.susybaka.TurnBasedGame.Events
{
    [CreateAssetMenu(fileName = "New Remove Object Event", menuName = "Turn Based Game/Events/Remove Object Event")]
    public class RemoveObjectEvent : ScriptableObject
    {
        public string gameObjectName = string.Empty;

        public void TriggerEvent()
        {
            if (!Application.isPlaying)
                return;

            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

            //Debug.Log($"[RemoveObjectEvent] Attempting to remove object with name containing: {gameObjectName}");

            foreach (GameObject root in roots)
            {
                if (!root.activeInHierarchy)
                    continue;

                //Debug.Log($"[RemoveObjectEvent] Checking root object: {root.name}");

                if (root.name.ToLower().Contains(gameObjectName.ToLower()))
                {
                    //Debug.Log($"[RemoveObjectEvent] Found and removing root object: {root.name}");
                    Destroy(root);
                    return;
                }
                else
                {
                    DestroyByNameInHierarchy(root.transform, gameObjectName);
                }
            }
        }

        private void DestroyByNameInHierarchy(Transform parent, string targetName)
        {
            foreach (Transform child in parent)
            {
                //Debug.Log($"[RemoveObjectEvent] Checking child object: {child.name}");

                if (child.name.ToLower().Contains(targetName.ToLower()))
                {
                    //Debug.Log($"[RemoveObjectEvent] Found and removing child object: {child.name}");
                    Destroy(child.gameObject);
                    return;
                }
                DestroyByNameInHierarchy(child, targetName);
            }
        }
    }
}