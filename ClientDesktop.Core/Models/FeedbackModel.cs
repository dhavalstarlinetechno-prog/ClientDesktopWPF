namespace ClientDesktop.Core.Models
{
    public class FeedbackModel
    {
        public string OperatorId { get; set; }
        public int FeedbackId { get; set; }
        public string FeedbackSubject { get; set; }
        public DateTime FeedbackDate { get; set; }
        public string DealerId { get; set; }
        public string DealerName { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public bool IsClosed { get; set; }
        public bool IsReadPending { get; set; }
    }

    public class FeedbackResponse
    {
        public FeedbackModel Data { get; set; }
        public string Exception { get; set; }
        public string SuccessMessage { get; set; }
        public int ReturnID { get; set; }
        public int Action { get; set; }
        public bool IsSuccess { get; set; }
    }
    public class FeedbackData
    {
        public int FeedbackId { get; set; }
        public string FeedbackSubject { get; set; }
        public string FeedbackMessage { get; set; }
        public DateTime CreatedOn { get; set; }
        public List<string> FilePath { get; set; }
        public List<ChatList> ChatList { get; set; }
    }
    public class ChatList
    {
        public string operatorId { get; set; }
        public int feedbackId { get; set; }
        public string userName { get; set; }
        public string userRole { get; set; }
        public string feedbackMessage { get; set; }
        public bool isReply { get; set; }
        public bool isRead { get; set; }
        public List<string> filePath { get; set; }
        public DateTime createdOn { get; set; }
    }

    public class FeedbackReplyResponse
    {
        public FeedbackReplyData Data { get; set; }
        public object Exception { get; set; }
        public string SuccessMessage { get; set; }
        public int ReturnID { get; set; }
        public int Action { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class FeedbackReplyData
    {
        public string ClientId { get; set; }
        public string OperatorId { get; set; }
        public int FeedbackId { get; set; }
        public string UserName { get; set; }
        public string UserRole { get; set; }
        public string FeedbackMessage { get; set; }
        public bool IsReply { get; set; }
        public bool IsRead { get; set; }
        public List<string> FilePath { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
