using System.Collections.Generic;

namespace Local_Area_Chat.MVP
{
    public class MainPresenter
    {
        private readonly IMainView view;
        private List<string> chatrooms = new() { "Allgemein", "Technik", "Sport" };
        private Dictionary<string, List<string>> messages = new();

        public MainPresenter(IMainView view)
        {
            this.view = view;
            foreach (var room in chatrooms)
                messages[room] = new List<string>();
            view.SetChatrooms(chatrooms);
            view.SetMessages(messages[chatrooms[0]]);
        }

        public void OnChatroomChanged(int index)
        {
            if (index >= 0 && index < chatrooms.Count)
                view.SetMessages(messages[chatrooms[index]]);
        }

        public void OnSendMessage(int chatroomIndex)
        {
            var msg = view.GetMessageInput();
            if (!string.IsNullOrWhiteSpace(msg) && chatroomIndex >= 0 && chatroomIndex < chatrooms.Count)
            {
                messages[chatrooms[chatroomIndex]].Add("Du: " + msg);
                view.SetMessages(messages[chatrooms[chatroomIndex]]);
                view.ClearMessageInput();
            }
        }
    }
}