using UnityEngine;
using UnityEngine.EventSystems;

namespace ClickerTowerDefense
{
    public class EnsureEventSystem : MonoBehaviour
    {
        private void Awake()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
