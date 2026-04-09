using UnityEngine;

namespace ClickerTowerDefense
{
    public class Path : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;

        public Transform[] Waypoints => waypoints;

        public Transform GetWaypoint(int index)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Length)
            {
                return null;
            }

            return waypoints[index];
        }
    }
}
