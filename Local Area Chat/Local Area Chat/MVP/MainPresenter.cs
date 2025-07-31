using System.Collections.Generic;
using System.Threading.Tasks;
using Local_Area_Chat.Data;
using Local_Area_Chat.Models;

namespace Local_Area_Chat.MVP
{
    public class MainPresenter
    {
        private readonly IMainView view;
        private List<string> chatrooms = new() { "Allgemein", "Technik", "Sport" };
        private Dictionary<string, List<string>> messages = new();
        private IRepository _repository;

        public MainPresenter(IMainView view, IRepository repository)
        {
            this.view = view;
            this._repository = repository;
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

        // Beispielhafte Ergänzung
        public async void OnSendMessage(string chatId, string user, string content)
        {
            var message = new ChatMessage
            {
                ChatId = chatId,
                Timestamp = DateTime.Now,
                User = user,
                Content = content
            };
            await _repository.AddMessageAsync(message);
            await LoadMessages(chatId);
        }

        public async void OnEditMessage(ChatMessage message, string newContent)
        {
            message.Content = newContent;
            await _repository.UpdateMessageAsync(message);
            await LoadMessages(message.ChatId);
        }

        private async Task LoadMessages(string chatId)
        {
            var chatMessages = await _repository.GetMessagesAsync(chatId);
            if (chatMessages != null)
            {
                var messageStrings = new List<string>();
                foreach (var msg in chatMessages)
                    messageStrings.Add($"{msg.User}: {msg.Content} ({msg.Timestamp:g})");
                view.SetMessages(messageStrings);
            }
        }
    }
}