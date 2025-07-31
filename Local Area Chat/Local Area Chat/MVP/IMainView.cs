using System.Collections.Generic;
using System.Windows.Controls;

namespace Local_Area_Chat.MVP
{
    public interface IMainView
    {
        void SetChatrooms(List<string> chatrooms);
        void SetMessages(List<string> messages);
        string GetMessageInput();
        void ClearMessageInput();
        int GetSelectedChatroomIndex();
    }
}