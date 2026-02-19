using UnityEngine;

namespace ClickerTowerDefense
{
    public static class AudioSettings
    {
        public static float SfxVolume { get; private set; } = 1f;
        public static float MusicVolume { get; private set; } = 1f;

        public static void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
        }

        public static void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
        }
    }
}
