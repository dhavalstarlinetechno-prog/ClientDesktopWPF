namespace ClientDesktop.Core.Models
{
    public class AuthResponse
    {
        public AuthResponseData data { get; set; }
        public object exception { get; set; }
        public string successMessage { get; set; }
        public int returnID { get; set; }
        public int action { get; set; }
        public bool isSuccess { get; set; }
    }

    public class AuthResponseData
    {
        public string token { get; set; }
        public string name { get; set; }
        public string expiration { get; set; }
    }

    public class AuthResponseDataList
    {
        public string sub { get; set; }
        public string role { get; set; }
        public string iss { get; set; }
        public DateTimeOffset exp { get; set; }
        public string ip { get; set; }
        public bool isreadonlypassword { get; set; }
        public string intime { get; set; }
        public string serverIP { get; set; }
    }

    public class AuthResponseObj
    {
        public AuthResponseDataList data { get; set; }

        public string exception { get; set; }

        public string successMessage { get; set; }

        public int returnID { get; set; }

        public int action { get; set; }

        public bool isSuccess { get; set; }
    }

}
