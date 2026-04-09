using System.Collections.Generic;

namespace ClickerTowerDefense
{
    public static class EnemyRegistry
    {
        private static readonly List<Enemy> Enemies = new List<Enemy>();

        public static IReadOnlyList<Enemy> All => Enemies;

        public static void Register(Enemy enemy)
        {
            if (enemy != null && !Enemies.Contains(enemy))
            {
                Enemies.Add(enemy);
            }
        }

        public static void Unregister(Enemy enemy)
        {
            if (enemy != null)
            {
                Enemies.Remove(enemy);
            }
        }
    }
}
