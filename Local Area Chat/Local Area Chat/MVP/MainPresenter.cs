using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Local_Area_Chat.Data;
using System.Linq;
using Local_Area_Chat.MVP.Models;

namespace Local_Area_Chat.MVP
{
    public class MainPresenter
    {
        private readonly IMainView view;
        private List<Chat> userChats = new();
        private MongoRepository _repository;
        private string currentUserId = null; // Null bedeutet nicht eingeloggt
        private User currentUser = null; // Aktuell eingeloggter Benutzer

        public MainPresenter(IMainView view, MongoRepository repository)
        {
            this.view = view;
            this._repository = repository;
            
            // Prüfe ob Repository verfügbar ist
            if (_repository != null)
            {
                // Warte auf Login, bevor Daten geladen werden
                InitializeWithoutLogin();
            }
            else
            {
                // Fallback zu lokalen Daten ohne DB
                InitializeLocalData();
            }
        }

        private void InitializeWithoutLogin()
        {
            // Zeige Login-Aufforderung
            var loginPrompt = new List<string> 
            { 
                "Willkommen beim Local Area Chat!",
                "",
                "Bitte melde dich an oder erstelle einen neuen Account:",
                "• Klicke auf das Benutzer-Symbol oben rechts",
                "• Gib deine Anmeldedaten ein",
                "• Oder erstelle einen neuen Benutzer",
                "",
                "Neuen Account erstellen:",
                "• Klicke auf 'Neuen Benutzer erstellen'",
                "• Wähle einen eindeutigen Benutzernamen",
                "• Erstelle ein sicheres Passwort",
                "• Sofort loslegen und chatten!"
            };
            
            view.SetChatrooms(new List<string> { "Anmeldung erforderlich" });
            view.SetMessages(loginPrompt);
            view.ClearCurrentUserDisplay(); // Clear user display when not logged in
        }

        // Login-Methode
        public async Task<bool> LoginAsync(string username, string password)
        {
            if (_repository == null)
            {
                view.ShowLoginError("Datenbankverbindung nicht verfügbar");
                return false;
            }

            try
            {
                // Suche Benutzer nach Username (verbesserte Methode)
                var user = await _repository.GetUserByUsernameAsync(username);
                
                if (user == null)
                {
                    view.ShowLoginError($"Benutzer '{username}' nicht gefunden. Möchten Sie einen neuen Account erstellen?");
                    return false;
                }

                // Prüfe Passwort
                if (user.UserPassword != password)
                {
                    view.ShowLoginError("Falsches Passwort");
                    return false;
                }

                // Login erfolgreich
                currentUser = user;
                currentUserId = user.UserId;
                
                view.ShowLoginSuccess(user.UserName);
                view.ClearLoginFields();
                view.SetCurrentUserDisplay(user.UserName); // Display logged-in user

                // Lade Chats für den eingeloggten Benutzer
                await LoadUserChats();
                
                return true;
            }
            catch (Exception ex)
            {
                view.ShowLoginError($"Login fehlgeschlagen: {ex.Message}");
                return false;
            }
        }

        // Registration-Methode
        public async Task<bool> RegisterUserAsync(string username, string password)
        {
            if (_repository == null)
            {
                view.ShowRegistrationError("Datenbankverbindung nicht verfügbar");
                return false;
            }

            try
            {
                // Prüfe ob Benutzername verfügbar ist
                var isAvailable = await _repository.IsUsernameAvailableAsync(username);
                if (!isAvailable)
                {
                    view.ShowRegistrationError($"Der Benutzername '{username}' ist bereits vergeben. Bitte wählen Sie einen anderen.");
                    return false;
                }

                // Erstelle neuen Benutzer (Public Key wird automatisch generiert)
                var newUser = await _repository.CreateUserAsync(username, password);

                view.ShowRegistrationSuccess(newUser.UserName);
                
                // Automatisch einloggen nach erfolgreicher Registrierung
                currentUser = newUser;
                currentUserId = newUser.UserId;
                
                view.SetCurrentUserDisplay(newUser.UserName); // Display registered user
                
                // Lade Chats für den neuen Benutzer
                await LoadUserChats();
                
                return true;
            }
            catch (Exception ex)
            {
                view.ShowRegistrationError($"Registrierung fehlgeschlagen: {ex.Message}");
                return false;
            }
        }

        // Füge neuen Benutzer zu Standard-Chats hinzu
        private async Task AddUserToDefaultChats(string userId)
        {
            try
            {              
                // Lade alle verfügbaren Chats
                var allChats = await _repository.GetAllChatsAsync();
                
                // Füge Benutzer zu allgemeinen Chats hinzu
                var generalChats = allChats.Where(c => c.ChatName.Contains("Allgemein") || c.ChatName.Contains("General")).ToList();
                foreach (var chat in generalChats)
                {
                    if (!chat.UserIds.Contains(userId))
                    {
                        await _repository.AddUserToChatAsync(chat.ChatId, userId);
                    }
                }
                
                // Falls keine Chats vorhanden sind, erstelle Standard-Chats für den Benutzer
                if (!allChats.Any())
                {
                    await CreateDefaultChatsForUser(userId);
                }
            }
            catch (Exception ex)
            {
                // Ignoriere Fehler hier, da die Chats später erstellt werden können
                System.Diagnostics.Debug.WriteLine($"Fehler beim Hinzufügen zu Standard-Chats: {ex.Message}");
            }
        }

        private async Task CreateDefaultChatsForUser(string userId)
        {
            var defaultChats = new List<Chat>
            {
                new Chat
                {
                    ChatId = "general_" + Guid.NewGuid().ToString("N")[..8],
                    ChatName = "Allgemein",
                    UserIds = new List<string> { userId }
                }
            };

            foreach (var chat in defaultChats)
            {
                await _repository.AddChatAsync(chat);
            }
        }

        // Hilfsmethode um alle Benutzer zu laden
        private async Task<List<User>> GetAllUsersAsync()
        {
            // Da MongoDB kein "GetAllUsers" hat, simulieren wir es
            var testUserIds = new[] { "user001", "user002", "user003", "user004", "user005", "CurrentUser" };
            var users = new List<User>();
            
            foreach (var userId in testUserIds)
            {
                var user = await _repository.GetUserByIdAsync(userId);
                if (user != null)
                    users.Add(user);
            }
            
            return users;
        }

        // Logout-Methode
        public void Logout()
        {
            currentUser = null;
            currentUserId = null;
            userChats.Clear();
            
            view.ClearCurrentUserDisplay(); // Clear user display on logout
            InitializeWithoutLogin();
        }

        private void InitializeLocalData()
        {
            var fallbackRooms = new List<string> { "Allgemein", "Technik", "Sport" };
            view.SetChatrooms(fallbackRooms);
            view.SetMessages(new List<string> 
            { 
                "Offline-Modus aktiv",
                "MongoDB-Verbindung nicht verfügbar",
                "Nachrichten werden nicht gespeichert",
                "",
                "Container neu starten:",
                "docker stop LAC && docker rm LAC",
                "docker run -d --name LAC -e MONGO_INITDB_ROOT_USERNAME=Admin -e MONGO_INITDB_ROOT_PASSWORD=Admin -p 27017:27017 mongo"
            });
        }

        private async Task InitializeDataAndLoadChats()
        {
            try
            {               
                // Lade Chats für den aktuellen Benutzer
                await LoadUserChats();
            }
            catch (Exception ex)
            {
                // Fallback zu lokalen Chatrooms wenn DB nicht erreichbar
                var fallbackRooms = new List<string> { "Allgemein", "Technik", "Sport" };
                view.SetChatrooms(fallbackRooms);
                view.SetMessages(new List<string> { $"Fehler beim Laden der Chats: {ex.Message}" });
            }
        }

        private async Task LoadUserChats()
        {
            try
            {
                // Lade alle Chats für den aktuellen Benutzer
                userChats = await _repository.GetChatsByUserIdAsync(currentUserId);
                
                // Add user to public chats automatically if not already added
                await JoinPublicChats();
                
                // Reload chats after potentially joining public chats
                userChats = await _repository.GetChatsByUserIdAsync(currentUserId);


                var chatroomNames = userChats.Select(c => c.ChatName).ToList();
                view.SetChatrooms(chatroomNames);
                
                if (userChats.Any())
                {
                    await LoadMessages(userChats[0].ChatId);
                }
            }
            catch (Exception ex)
            {
                // Fallback zu lokalen Chatrooms wenn DB nicht erreichbar
                var fallbackRooms = new List<string> { "Allgemein", "Technik", "Sport" };
                view.SetChatrooms(fallbackRooms);
                view.SetMessages(new List<string> { $"Fehler beim Laden der Chats: {ex.Message}" });
            }
        }

        // Join public chats automatically
        private async Task JoinPublicChats()
        {
            try
            {
                var allChats = await _repository.GetAllChatsAsync();
                var publicChats = allChats.Where(c => !c.IsPrivate && !c.UserIds.Contains(currentUserId));
                
                foreach (var chat in publicChats)
                {
                    await _repository.AddUserToChatAsync(chat.ChatId, currentUserId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Beitreten zu öffentlichen Chats: {ex.Message}");
            }
        }
        public async void OnChatroomChanged(int index)
        {
            if (index >= 0 && index < userChats.Count)
            {
                string chatId = userChats[index].ChatId;
                await LoadMessages(chatId);
            }
        }
        public void OnSendMessage(int chatroomIndex)
        {
            var msg = view.GetMessageInput();
            if (!string.IsNullOrWhiteSpace(msg) && chatroomIndex >= 0 && chatroomIndex < userChats.Count)
            {
                if (_repository != null)
                {
                    string chatId = userChats[chatroomIndex].ChatId;
                    OnSendMessage(chatId, currentUserId, msg);
                }
                else
                {
                    // Offline-Modus: Zeige Nachricht lokal an
                    var offlineMessages = new List<string>
                    {
                        "WARNUNG: Offline-Modus: Nachricht nicht gespeichert",
                        $"Du: {msg}",
                        "",
                        "MongoDB-Verbindung erforderlich zum Speichern"
                    };
                    view.SetMessages(offlineMessages);
                    view.ClearMessageInput();
                }
            }
        }

        public async void OnSendMessage(string chatId, string user, string content)
        {
            if (_repository == null) return;
            
            var message = new Message
            {
                ChatId = chatId,
                UserId = user,
                Content = content,
                Timestamp = DateTime.Now
            };
            await _repository.AddMessageAsync(message);
            await LoadMessages(chatId);
            view.ClearMessageInput();
        }

        public async Task OnEditMessage(Message message, string newContent)
        {
            // Check if current user owns the message
            if (!CanEditMessage(message))
            {
                view.ShowLoginError("Sie können nur Ihre eigenen Nachrichten bearbeiten.");
                return;
            }

            message.Content = newContent;
            await _repository.UpdateMessageAsync(message);
            await LoadMessages(message.ChatId);
        }

        // Check if current user can edit the message (ownership validation)
        private bool CanEditMessage(Message message)
        {
            // User must be logged in
            if (string.IsNullOrEmpty(currentUserId))
                return false;

            // User can only edit their own messages
            return message.UserId == currentUserId;
        }

        // Method to check if a message can be edited (for UI validation)
        public bool CanCurrentUserEditMessage(Message message)
        {
            return CanEditMessage(message);
        }

        // NEU: Methode zum automatischen Aktualisieren von Nachrichten
        public async Task RefreshCurrentChatMessages(string chatId)
        {
            if (_repository == null || string.IsNullOrEmpty(currentUserId))
                return;

            try
            {
                // Lade aktuelle Nachrichten aus der Datenbank
                await LoadMessages(chatId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Aktualisieren der Nachrichten: {ex.Message}");
            }
        }

        // NEU: Optimierte LoadMessages Methode mit Caching
        private List<Message> lastLoadedMessages = new();

        private async Task LoadMessages(string chatId)
        {
            var chatMessages = await _repository.GetMessagesByChatIdAsync(chatId);
            if (chatMessages != null)
            {
                // Nur UI aktualisieren wenn sich Nachrichten geändert haben
                if (!MessagesAreEqual(lastLoadedMessages, chatMessages))
                {
                    lastLoadedMessages = new List<Message>(chatMessages);
                    
                    var messageStrings = new List<string>();
                    foreach (var msg in chatMessages)
                    {
                        // Verbesserte Anzeige der Nachrichten mit Benutzernamen
                        var user = await _repository.GetUserByIdAsync(msg.UserId);
                        var displayName = user?.UserName ?? msg.UserId;
                        
                        // Add ownership indicator for current user's messages
                        var ownershipIndicator = (msg.UserId == currentUserId) ? "[Du] " : "";
                        messageStrings.Add($"{ownershipIndicator}{displayName}: {msg.Content} ({msg.Timestamp:g})");
                    }
                    view.SetMessages(messageStrings);
                }
            }
        }

        // NEU: Hilfsmethode zum Vergleichen von Message-Listen
        private bool MessagesAreEqual(List<Message> list1, List<Message> list2)
        {
            if (list1.Count != list2.Count) return false;
            
            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i].Id != list2[i].Id || 
                    list1[i].Content != list2[i].Content || 
                    list1[i].Timestamp != list2[i].Timestamp)
                {
                    return false;
                }
            }
            return true;
        }

        public async Task<List<Message>> GetMessagesForEdit(string chatId)
        {
            return await _repository.GetMessagesByChatIdAsync(chatId);
        }

        // Neue Methoden für Chat-Management
        public async Task AddUserToChat(string chatId, string userId)
        {
            await _repository.AddUserToChatAsync(chatId, userId);
            await LoadUserChats(); // Chats neu laden
        }

        public async Task RemoveUserFromChat(string chatId, string userId)
        {
            await _repository.RemoveUserFromChatAsync(chatId, userId);
            await LoadUserChats(); // Chats neu laden
        }

        public async Task CreateNewChat(string chatName, List<string> userIds)
        {
            var chat = new Chat
            {
                ChatId = Guid.NewGuid().ToString(),
                ChatName = chatName,
                UserIds = userIds
            };
            await _repository.AddChatAsync(chat);
            await LoadUserChats(); // Chats neu laden
        }

        // Simplified method for creating a new chat with only the current user
        public async Task CreateNewChatSimple(string chatName)
        {
            if (_repository == null)
            {
                view.ShowNewChatError("Datenbankverbindung nicht verfügbar");
                return;
            }

            if (string.IsNullOrEmpty(currentUserId))
            {
                view.ShowNewChatError("Sie müssen angemeldet sein, um einen Chat zu erstellen.");
                return;
            }

            try
            {
                var chat = new Chat
                {
                    ChatId = Guid.NewGuid().ToString(),
                    ChatName = chatName,
                    UserIds = new List<string> { currentUserId },
                    AdminUserId = currentUserId, // Creator becomes admin
                    IsPrivate = true // New chats are private by default
                };
                
                await _repository.AddChatAsync(chat);
                await LoadUserChats(); // Reload chats to show the new one
                
                view.ShowNewChatSuccess(chatName);
            }
            catch (Exception ex)
            {
                view.ShowNewChatError($"Fehler beim Erstellen des Chats: {ex.Message}");
            }
        }

        // Administrator functionality methods
        public async Task<bool> IsCurrentUserChatAdmin(string chatId)
        {
            if (_repository == null || string.IsNullOrEmpty(currentUserId))
                return false;

            return await _repository.IsUserChatAdminAsync(chatId, currentUserId);
        }

        public async Task<List<User>> GetAvailableUsersForChat(string chatId)
        {
            if (_repository == null)
                return new List<User>();

            return await _repository.GetUsersNotInChatAsync(chatId);
        }

        public async Task<bool> AddUserToChatAsAdmin(string chatId, string userId)
        {
            if (_repository == null)
                return false;

            // Check if current user is admin of this chat
            var isAdmin = await IsCurrentUserChatAdmin(chatId);
            if (!isAdmin)
                return false;

            try
            {
                await _repository.AddUserToChatAsync(chatId, userId);
                await LoadUserChats(); // Refresh chat list
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveUserFromChatAsAdmin(string chatId, string userId)
        {
            if (_repository == null)
                return false;

            // Check if current user is admin of this chat
            var isAdmin = await IsCurrentUserChatAdmin(chatId);
            if (!isAdmin)
                return false;

            // Don't allow admin to remove themselves
            if (userId == currentUserId)
                return false;

            try
            {
                await _repository.RemoveUserFromChatAsync(chatId, userId);
                await LoadUserChats(); // Refresh chat list
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Hilfsmethode zur Anzeige der Benutzer in einem Chat
        public async Task<List<string>> GetChatParticipantsAsync(string chatId)
        {
            if (_repository == null)
                return new List<string>();

            try
            {
                var chat = await _repository.GetChatByIdAsync(chatId);
                if (chat == null) return new List<string>();

                var participants = new List<string>();
                foreach (var userId in chat.UserIds)
                {
                    var user = await _repository.GetUserByIdAsync(userId);
                    participants.Add(user?.UserName ?? userId);
                }
                return participants;
            }
            catch
            {
                return new List<string>();
            }
        }

        // Get ChatId by index for proper admin checks
        public async Task<string?> GetChatIdByIndex(int index)
        {
            if (index < 0 || index >= userChats.Count)
                return null;
            
            return userChats[index].ChatId;
        }

        // New methods for ChatManagementDialog functionality
        public async Task<bool> UpdateChatStatusAsync(string chatId, bool isPrivate)
        {
            if (_repository == null)
                return false;

            try
            {
                var chat = await _repository.GetChatByIdAsync(chatId);
                if (chat == null)
                    return false;

                chat.IsPrivate = isPrivate;
                
                // If changing to public, add all users to the chat
                if (!isPrivate)
                {
                    var allUsers = await _repository.GetAllUsersAsync();
                    foreach (var user in allUsers)
                    {
                        if (!chat.UserIds.Contains(user.UserId))
                        {
                            chat.UserIds.Add(user.UserId);
                        }
                    }
                }

                await _repository.UpdateChatAsync(chat);
                await LoadUserChats(); // Refresh chat list
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Chat Management Methoden - NUR EINMAL definieren:

        public async Task<bool> UpdateChatNameAsync(string chatId, string newName)
        {
            if (_repository == null) return false;
            return await _repository.UpdateChatNameAsync(chatId, newName);
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            if (_repository == null) return null;
            return await _repository.GetUserByIdAsync(userId);
        }

        public async Task<bool> DeleteChatAsync(string chatId)
        {
            if (_repository == null) return false;
            return await _repository.DeleteChatAsync(chatId);
        }

        public async Task<string?> GetUserIdByUsername(string username)
        {
            if (_repository == null) return null;
            var user = await _repository.GetUserByUsernameAsync(username);
            return user?.UserId;
        }

        public async Task<Chat?> GetChatByIdAsync(string chatId)
        {
            if (_repository == null) return null;
            return await _repository.GetChatByIdAsync(chatId);
        }

        // KEINE zweite UpdateChatStatusAsync Methode hier!

        public async Task RefreshUserChatsFromDatabase()
        {
            if (_repository == null || string.IsNullOrEmpty(currentUserId))
                return;

            try
            {
                // Lade alle Chats für den aktuellen Benutzer neu aus der Datenbank
                userChats = await _repository.GetChatsByUserIdAsync(currentUserId);
                
                // Aktualisiere die UI mit den neuen Chat-Namen
                var chatroomNames = userChats.Select(c => c.ChatName).ToList();
                view.SetChatrooms(chatroomNames);
                
                // Wenn Chats vorhanden sind, lade Nachrichten für den ersten Chat
                // Andernfalls zeige eine leere Nachrichtenliste
                if (userChats.Any())
                {
                    await LoadMessages(userChats[0].ChatId);
                }
                else
                {
                    view.SetMessages(new List<string> { "Keine Chats verfügbar. Erstellen Sie einen neuen Chat!" });
                }
            }
            catch (Exception ex)
            {
                view.SetMessages(new List<string> { $"Fehler beim Laden der Chats: {ex.Message}" });
            }
        }
    }
}