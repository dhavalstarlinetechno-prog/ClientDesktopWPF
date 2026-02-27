namespace ClientDesktop.Core.Events
{
    /// <summary>
    /// Signal broadcasted when the OS power mode changes (e.g., PC goes to sleep or wakes up).
    /// </summary>
    public class SystemPowerStateEvent
    {
        public bool IsWakingUp { get; }

        public SystemPowerStateEvent(bool isWakingUp)
        {
            IsWakingUp = isWakingUp;
        }
    }
}