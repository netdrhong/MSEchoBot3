using System.ComponentModel.DataAnnotations;

namespace EchoBot3.Models
{
    public class SendMessageRequest
    {
        [Required]
        public string Text { get; set; }

        [Required]
        public string ChatId { get; set; } // Channel ID or conversation ID

        [Required]
        public string ServiceUrl { get; set; } // Teams service URL (e.g., https://smba.trafficmanager.net/amer/)

        public string TenantId { get; set; } // Optional, will use default from config if not provided
    }

    public class SendMessageResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ActivityId { get; set; }
    }
}