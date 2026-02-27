namespace ClientDesktop.Core.Events
{
    /// <summary>
    /// Signal broadcasted when a user successfully logs in or logs out.
    /// </summary>
    public class UserAuthEvent
    {
        public bool IsLoggedIn { get; }
        public string UserId { get; }

        public UserAuthEvent(bool isLoggedIn, string userId)
        {
            IsLoggedIn = isLoggedIn;
            UserId = userId;
        }
    }
}