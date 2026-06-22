namespace SEENG_ES
{
    public static class SEENG_VolumeManager1
    {

        private static float _masterVolume = 100f;

        public static float MasterVolume
        {
            get => _masterVolume;
            set => _masterVolume = value;
        }

        public static float GetMultiplier()
        {
            return _masterVolume / 100f;
        }
        public static void SetVolume(float volume)
        {
            _masterVolume = volume;
        }
    }
}