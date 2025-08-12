using System.Collections.Generic;

namespace Local_Area_Chat.MVP.Models
{
    public class ChatManagementInfo
    {
        public string ChatName { get; set; } = "";
        public bool IsPrivate { get; set; }
        public List<string> Participants { get; set; } = new();
        public List<User> AvailableUsers { get; set; } = new();
        public string AdminName { get; set; } = "";
    }
}