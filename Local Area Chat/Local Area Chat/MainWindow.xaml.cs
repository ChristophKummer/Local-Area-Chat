using Local_Area_Chat.Dialogs;
using Local_Area_Chat.MVP;
using Local_Area_Chat.Data;
using Local_Area_Chat.Config;  // HINZUGEFÜGT: Für DatabaseConfig
using Local_Area_Chat.Security; // NEU: Für ChatEncryption
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Linq;
using System.Threading.Tasks;
using Local_Area_Chat.MVP.Models;

namespace Local_Area_Chat
{
    //Setup für docker von Video
    //https://www.youtube.com/watch?v=gFjpv-nZO0U&t=7s
    //docker run -d --name LAC -e MONGO_INITDB_ROOT_USERNAME=Admin -e MONGO_INITDB_ROOT_PASSWORD=Admin -p 27017:27017 mongo
    //Username: Admin
    //Password: Admin
    //Port: 27017:27017
    //docker stop LAC
    //docker rm LAC

    //IP-Adressenanpassung auf Raspberry falls nötig
    //sudo ip addr add 192.168.1.2/24 dev eth0
    //mongodb://Admin:Admin@192.168.1.2:27017/?authSource=admin

    public partial class MainWindow : Window, IMainView
    {
        private MainPresenter presenter;
        private MongoRepository repository;
        private DispatcherTimer? loginCloseTimer;
        private DispatcherTimer? messageRefreshTimer; // NEU: Timer für automatische Updates
        private string? currentChatId; // NEU: Aktuelle Chat-ID verfolgen

        public MainWindow()
        {
            InitializeComponent();
            
            // Configure MessagesListBox for text wrapping
            ConfigureMessagesListBox();
            
            InitializeDatabaseConnection();
            
            presenter = new MainPresenter(this, repository);
            
            // Configure MessagesListBox for text wrapping after initialization
            ConfigureMessagesListBox();
            
            // NEU: Timer für automatische Message-Updates initialisieren
            InitializeMessageRefreshTimer();
        }

        private async void InitializeDatabaseConnection()
        {
            try
            {
                // Show loading message
                System.Diagnostics.Debug.WriteLine("?? Starte Datenbankverbindung zum Raspberry Pi...");
                
                // Verwende die neue DatabaseConfig mit Tupel-Rückgabe
                var (repo, message) = await DatabaseConfig.ConnectToDatabase();
                repository = repo;
                
                if (repository != null)
                {
                    System.Diagnostics.Debug.WriteLine($"? {message}");
                    // Erfolgreiche Verbindung - Info anzeigen
                    MessageBox.Show(message, "Raspberry Pi Verbindung erfolgreich", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    throw new Exception("Keine verfügbare MongoDB-Verbindung gefunden");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Datenbankverbindung fehlgeschlagen: {ex.Message}");
                
                // Detaillierte Hilfsmeldung für Raspberry Pi Setup
                MessageBox.Show($"MongoDB-Verbindung zum Raspberry Pi fehlgeschlagen: {ex.Message}\n\n" +
                              "? Raspberry Pi MongoDB Status:\n" +
                              "   MongoDB läuft und ist bereit für Verbindungen\n" +
                              "   IP: 192.168.1.2:27017\n\n" +
                              "?? Mögliche Lösungen:\n" +
                              "1. Netzwerk-Test von Windows:\n" +
                              "   ping 192.168.1.2\n" +
                              "   Test-NetConnection -ComputerName 192.168.1.2 -Port 27017\n\n" +
                              "2. Windows Firewall prüfen\n" +
                              "3. MongoDB Container Status auf Raspberry Pi:\n" +
                              "   ssh christoph@192.168.1.2\n" +
                              "   docker ps\n" +
                              "   docker logs LAC\n\n" +
                              "4. Container neu starten (falls nötig):\n" +
                              "   docker stop LAC && docker rm LAC\n" +
                              "   docker run -d --name LAC -e MONGO_INITDB_ROOT_USERNAME=Admin -e MONGO_INITDB_ROOT_PASSWORD=Admin -p 0.0.0.0:27017:27017 --restart unless-stopped mongo\n\n" +
                              "Arbeite im Offline-Modus...",
                              "Raspberry Pi MongoDB-Verbindung", MessageBoxButton.OK, MessageBoxImage.Warning);
                repository = null;
            }
        }
        
        // NEU: Timer-Initialisierung
        private void InitializeMessageRefreshTimer()
        {
            messageRefreshTimer = new DispatcherTimer();
            messageRefreshTimer.Interval = TimeSpan.FromSeconds(3); // Alle 3 Sekunden prüfen
            messageRefreshTimer.Tick += MessageRefreshTimer_Tick;
        }

        // NEU: Timer-Event Handler
        private async void MessageRefreshTimer_Tick(object? sender, EventArgs e)
        {
            // Nur aktualisieren wenn ein Chat ausgewählt ist und Benutzer eingeloggt ist
            if (!string.IsNullOrEmpty(currentChatId) && presenter != null)
            {
                try
                {
                    await presenter.RefreshCurrentChatMessages(currentChatId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Fehler beim automatischen Aktualisieren: {ex.Message}");
                }
            }
        }

        private void ConfigureMessagesListBox()
        {
            // Configure the MessagesListBox for text wrapping and disable horizontal scrolling
            var itemTemplate = new DataTemplate();
            
            // Create a TextBlock with TextWrapping
            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding());
            textBlockFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            textBlockFactory.SetValue(TextBlock.MarginProperty, new Thickness(5, 2, 5, 2));
            textBlockFactory.SetValue(TextBlock.PaddingProperty, new Thickness(5));
            
            itemTemplate.VisualTree = textBlockFactory;
            MessagesListBox.ItemTemplate = itemTemplate;
            
            // Configure ScrollViewer to disable horizontal scrolling
            System.Windows.Controls.ScrollViewer.SetHorizontalScrollBarVisibility(MessagesListBox, ScrollBarVisibility.Disabled);
            System.Windows.Controls.ScrollViewer.SetVerticalScrollBarVisibility(MessagesListBox, ScrollBarVisibility.Auto);
        }

        public void SetChatrooms(List<string> chatrooms)
        {
            ChatroomListBox.ItemsSource = chatrooms;
            if (chatrooms.Count > 0)
                ChatroomListBox.SelectedIndex = 0;
        }

        public void SetMessages(List<string> messages)
        {
            MessagesListBox.ItemsSource = null;
            MessagesListBox.ItemsSource = messages;
            
            // Configure text wrapping if not already configured
            if (MessagesListBox.ItemTemplate == null)
            {
                var itemTemplate = new DataTemplate();
                var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                textBlockFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding());
                textBlockFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
                textBlockFactory.SetValue(TextBlock.MarginProperty, new Thickness(5, 2, 5, 2));
                
                itemTemplate.VisualTree = textBlockFactory;
                MessagesListBox.ItemTemplate = itemTemplate;
                
                // Disable horizontal scrolling
                System.Windows.Controls.ScrollViewer.SetHorizontalScrollBarVisibility(MessagesListBox, ScrollBarVisibility.Disabled);
                System.Windows.Controls.ScrollViewer.SetVerticalScrollBarVisibility(MessagesListBox, ScrollBarVisibility.Auto);
            }
        }

        public void ClearMessageInput() => MessageTextBox.Text = "";
        public string GetMessageInput() => MessageTextBox.Text;

        public int GetSelectedChatroomIndex() => ChatroomListBox.SelectedIndex;

        public string GetLoginUsername() => UserTextBox.Text;
        public string GetLoginPassword() => PasswordBox.Password;

        public void ShowLoginError(string message)
        {
            MessageBox.Show(message, "Login Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowLoginSuccess(string username)
        {
            MessageBox.Show($"Erfolgreich angemeldet als: {username}", "Login Erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ClearLoginFields()
        {
            UserTextBox.Text = "";
            PasswordBox.Password = "";
        }

        public void ShowRegistrationError(string message)
        {
            MessageBox.Show(message, "Registrierung Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowRegistrationSuccess(string username)
        {
            MessageBox.Show($"Benutzer '{username}' wurde erfolgreich erstellt und Sie sind jetzt angemeldet!", 
                          "Registrierung Erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // User Display-Funktionalität
        public void SetCurrentUserDisplay(string username)
        {
            // Show username next to user icon
            UserNameTextBlock.Text = username;
            UserNameTextBlock.Visibility = Visibility.Visible;
            
            // NEU: Timer starten wenn bereits ein Chat ausgewählt ist
            if (!string.IsNullOrEmpty(currentChatId))
            {
                messageRefreshTimer?.Start();
            }
        }

        public void ClearCurrentUserDisplay()
        {
            // Hide username next to user icon
            UserNameTextBlock.Text = "";
            UserNameTextBlock.Visibility = Visibility.Collapsed;
            
            // NEU: Timer stoppen beim Logout
            messageRefreshTimer?.Stop();
            currentChatId = null;
        }

        // Chat Creation-Funktionalität
        public void ShowNewChatError(string message)
        {
            MessageBox.Show(message, "Neuer Chat Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowNewChatSuccess(string chatName)
        {
            MessageBox.Show($"Chat '{chatName}' wurde erfolgreich erstellt!", "Neuer Chat", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Chat Management-Funktionalität
        public void ShowChatManagementError(string message)
        {
            MessageBox.Show(message, "Chat Management Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowChatManagementSuccess(string message)
        {
            MessageBox.Show(message, "Chat Management", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ChatroomListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (presenter != null)
            {
                var selectedIndex = GetSelectedChatroomIndex();
                presenter.OnChatroomChanged(selectedIndex);

                // NEU: Aktuelle Chat-ID speichern und Timer starten
                if (selectedIndex >= 0)
                {
                    Task.Run(async () => {
                        currentChatId = await presenter.GetChatIdByIndex(selectedIndex);
                        
                        // Timer nur starten wenn Chat ausgewählt und Benutzer eingeloggt
                        if (!string.IsNullOrEmpty(currentChatId))
                        {
                            Dispatcher.Invoke(() => messageRefreshTimer?.Start());
                        }
                    });
                }
                else
                {
                    currentChatId = null;
                    messageRefreshTimer?.Stop();
                }
            }

            SelectedChatroomTextBlock.Text = ChatroomListBox.SelectedItem?.ToString() ?? "";
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            presenter.OnSendMessage(GetSelectedChatroomIndex());
        }

        private void LoginToggleButton_Click(object sender, RoutedEventArgs e)
        {
            LoginPopup.IsOpen = true;
            
            // Set focus to username field when login popup opens
            UserTextBox.Focus();
        }

        private void LoginPopup_MouseLeave(object sender, MouseEventArgs e)
        {
            // Timer starten, wenn Maus das Popup verlässt
            if (loginCloseTimer == null)
            {
                loginCloseTimer = new DispatcherTimer();
                loginCloseTimer.Interval = TimeSpan.FromSeconds(1);
                loginCloseTimer.Tick += LoginCloseTimer_Tick;
            }
            loginCloseTimer.Start();
        }

        private void LoginPopup_MouseEnter(object sender, MouseEventArgs e)
        {
            // Timer stoppen, wenn Maus wieder im Popup ist
            loginCloseTimer?.Stop();
        }

        private void LoginCloseTimer_Tick(object? sender, EventArgs e)
        {
            loginCloseTimer?.Stop();
            LoginPopup.IsOpen = false;
            UserTextBox.Text = "";
            PasswordBox.Password = "";
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Echte Login-Logik implementieren
            var username = GetLoginUsername();
            var password = GetLoginPassword();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowLoginError("Bitte Benutzername und Passwort eingeben");
                return;
            }

            LoginPopup.IsOpen = false;
            
            // Login über Presenter (ohne Public Key)
            var loginSuccess = await presenter.LoginAsync(username, password);
            
            if (!loginSuccess)
            {
                // Bei Fehler Popup wieder öffnen
                LoginPopup.IsOpen = true;
            }
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            HamburgerButton.ContextMenu.IsOpen = true;
        }

        private void HamburgerMenu_Profile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Profil geöffnet");
            ProfilePanel.Visibility = Visibility.Visible;
        }

        private void HamburgerMenu_Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Möchten Sie sich wirklich abmelden?", "Abmelden", 
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                presenter.Logout();
                MessageBox.Show("Sie wurden erfolgreich abgemeldet", "Abgemeldet", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ProfileSaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Hier kannst du die Logik zum Speichern der Profil-Daten einfügen
            ProfilePanel.Visibility = Visibility.Collapsed;
        }

        private void ProfileCloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Felder leeren (optional)
            ProfileUserTextBox.Text = "";
            ProfilePasswordTextBox.Text = "";
            ProfileRoleComboBox.SelectedIndex = -1;
            ProfilePanel.Visibility = Visibility.Collapsed;
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void UserTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                // Move focus to password box
                PasswordBox.Focus();
                e.Handled = true;
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Trigger login when Enter is pressed in password field
                LoginButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private async void NewChatButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if user is logged in
            if (presenter == null)
            {
                ShowNewChatError("Bitte melden Sie sich zuerst an, um einen neuen Chat zu erstellen.");
                return;
            }

            // Ask for chat name
            var chatName = ShowInputDialog("Neuen Chat erstellen", "Chat-Name eingeben:");
            if (string.IsNullOrWhiteSpace(chatName))
                return;

            chatName = chatName.Trim();

            // Validate chat name
            if (chatName.Length < 2)
            {
                ShowNewChatError("Der Chat-Name muss mindestens 2 Zeichen lang sein.");
                return;
            }

            if (chatName.Length > 50)
            {
                ShowNewChatError("Der Chat-Name darf maximal 50 Zeichen lang sein.");
                return;
            }

            // Create new chat with current user
            await presenter.CreateNewChatSimple(chatName);
        }

        private async void EditMessage_Click(object sender, RoutedEventArgs e)
        {
            var chatroomIndex = GetSelectedChatroomIndex();
            if (chatroomIndex < 0) return;
            
            var selectedIndex = MessagesListBox.SelectedIndex;
            if (selectedIndex < 0) return;

            // Hole den Chat-Namen und verwende ihn als ChatId (vereinfacht)
            var selectedChatName = ChatroomListBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedChatName)) return;

            // Hole die Message aus dem Repository
            var chatMessages = await presenter.GetMessagesForEdit(selectedChatName);
            if (selectedIndex >= chatMessages.Count) return;
            
            var selectedMessage = chatMessages[selectedIndex];

            // Check if current user can edit this message
            if (!presenter.CanCurrentUserEditMessage(selectedMessage))
            {
                MessageBox.Show("Sie können nur Ihre eigenen Nachrichten bearbeiten.", 
                              "Bearbeitung nicht erlaubt", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                return;
            }

            var dialog = new EditMessageDialog(selectedMessage.Content);
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.NewContent))
            {
                await presenter.OnEditMessage(selectedMessage, dialog.NewContent);
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            LoginPopup.IsOpen = false;

            // Simple registration using only username and password
            var usernameResult = ShowInputDialog("Neuen Benutzer erstellen", "Benutzername (mindestens 3 Zeichen):");
            if (string.IsNullOrWhiteSpace(usernameResult))
                return;

            var passwordResult = ShowInputDialog("Neuen Benutzer erstellen", "Passwort (mindestens 6 Zeichen):");
            if (string.IsNullOrWhiteSpace(passwordResult))
                return;

            var username = usernameResult.Trim();
            var password = passwordResult;

            // Validate input
            if (username.Length < 3)
            {
                ShowRegistrationError("Der Benutzername muss mindestens 3 Zeichen lang sein.");
                return;
            }

            if (password.Length < 6)
            {
                ShowRegistrationError("Das Passwort muss mindestens 6 Zeichen lang.");
                return;
            }

            // Register user through presenter (Public Key will be auto-generated)
            var success = await presenter.RegisterUserAsync(username, password);
            
            if (!success)
            {
                // Error message is already shown by the presenter
                LoginPopup.IsOpen = true; // Reopen login popup for retry
            }
        }

        private string ShowInputDialog(string title, string prompt)
        {
            // Create a simple input dialog window
            var inputWindow = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var promptLabel = new TextBlock
            {
                Text = prompt,
                Margin = new Thickness(20, 20, 20, 10),
                FontSize = 14
            };
            Grid.SetRow(promptLabel, 0);

            var inputTextBox = new TextBox
            {
                Margin = new Thickness(20, 0, 20, 20),
                Height = 25,
                FontSize = 12
            };
            Grid.SetRow(inputTextBox, 1);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 0, 20, 20)
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 75,
                Height = 25,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };

            var cancelButton = new Button
            {
                Content = "Abbrechen",
                Width = 75,
                Height = 25,
                IsCancel = true
            };

            okButton.Click += (s, e) => {
                inputWindow.Tag = inputTextBox.Text;
                inputWindow.DialogResult = true;
                inputWindow.Close();
            };

            cancelButton.Click += (s, e) => {
                inputWindow.DialogResult = false;
                inputWindow.Close();
            };

            inputTextBox.KeyDown += (s, e) => {
                if (e.Key == Key.Enter)
                    okButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(promptLabel);
            grid.Children.Add(inputTextBox);
            grid.Children.Add(buttonPanel);

            inputWindow.Content = grid;
            inputTextBox.Focus();

            return inputWindow.ShowDialog() == true ? inputWindow.Tag?.ToString() ?? "" : "";
        }

        // DIESE METHODE NUR EINMAL DEFINIEREN!
        private async Task<string?> GetSelectedChatId()
        {
            var selectedIndex = GetSelectedChatroomIndex();
            if (selectedIndex < 0) return null;

            var selectedChatName = ChatroomListBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedChatName)) return null;

            // Chat-ID anhand des Chat-Namens aus der Datenbank holen
            if (presenter != null && repository != null)
            {
                var allChats = await repository.GetAllChatsAsync();
                var chat = allChats.FirstOrDefault(c => c.ChatName == selectedChatName);
                return chat?.ChatId;
            }

            return null;
        }

        // Chat management event handlers
        private async void ManageChat_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = GetSelectedChatroomIndex();
            if (selectedIndex < 0)
            {
                ShowChatManagementError("Bitte wählen Sie einen Chat aus.");
                return;
            }

            try
            {
                var chatId = await GetSelectedChatId();
                if (chatId == null) return;

                var isAdmin = await presenter.IsCurrentUserChatAdmin(chatId);
                if (!isAdmin)
                {
                    ShowChatManagementError("Sie sind nicht der Administrator dieses Chats.");
                    return;
                }

                // Get current chat information
                var chat = await presenter.GetChatByIdAsync(chatId);
                if (chat == null) return;

                var chatName = chat.ChatName;
                var isPrivate = chat.IsPrivate;
                var participants = await presenter.GetChatParticipantsAsync(chatId);
                var availableUsers = await presenter.GetAvailableUsersForChat(chatId);
                
                // Get admin name
                var adminUser = await presenter.GetUserByIdAsync(chat.AdminUserId);
                var adminName = adminUser?.UserName ?? "Unbekannt";
                
                // Open enhanced chat management dialog
                var dialog = new ChatManagementDialog(chatName, isPrivate, participants, availableUsers, adminName);
                if (dialog.ShowDialog() == true)
                {
                    // Check if chat should be deleted
                    if (dialog.DeleteChat)
                    {
                        var deleteSuccess = await presenter.DeleteChatAsync(chatId);
                        if (deleteSuccess)
                        {
                            ShowChatManagementSuccess("Chat wurde erfolgreich gelöscht.");
                            
                            // WICHTIG: Chat-Liste aus der Datenbank neu laden
                            await RefreshChatList();
                            return;
                        }
                        else
                        {
                            ShowChatManagementError("Fehler beim Löschen des Chats.");
                            return;
                        }
                    }

                    // Verfolge, ob Änderungen vorgenommen wurden
                    bool changesApplied = false;

                    // Update chat name if changed
                    if (!string.IsNullOrEmpty(dialog.NewChatName) && dialog.NewChatName != chatName)
                    {
                        var nameUpdateSuccess = await presenter.UpdateChatNameAsync(chatId, dialog.NewChatName);
                        if (nameUpdateSuccess)
                        {
                            changesApplied = true;
                        }
                        else
                        {
                            ShowChatManagementError("Fehler beim Aktualisieren des Chat-Namens.");
                            return;
                        }
                    }
                    
                    // Update status if changed
                    if (dialog.IsPrivate != isPrivate)
                    {
                        var statusUpdateSuccess = await presenter.UpdateChatStatusAsync(chatId, dialog.IsPrivate);
                        if (statusUpdateSuccess)
                        {
                            changesApplied = true;
                        }
                        else
                        {
                            ShowChatManagementError("Fehler beim Aktualisieren des Chat-Status.");
                            return;
                        }
                    }

                    // Add new users
                    foreach (var user in dialog.AddedUsers)
                    {
                        var addSuccess = await presenter.AddUserToChatAsAdmin(chatId, user.UserId);
                        if (addSuccess)
                        {
                            changesApplied = true;
                        }
                    }

                    // Remove users
                    foreach (var username in dialog.RemovedUsers)
                    {
                        var userId = await presenter.GetUserIdByUsername(username);
                        if (userId != null)
                        {
                            var removeSuccess = await presenter.RemoveUserFromChatAsAdmin(chatId, userId);
                            if (removeSuccess)
                            {
                                changesApplied = true;
                            }
                        }
                    }
                    
                    // Nur wenn Änderungen vorgenommen wurden, aktualisiere die UI
                    if (changesApplied)
                    {
                        ShowChatManagementSuccess("Chat-Einstellungen wurden erfolgreich aktualisiert.");
                        
                        // WICHTIG: Chat-Liste aus der Datenbank neu laden
                        await RefreshChatList();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowChatManagementError($"Fehler beim Verwalten des Chats: {ex.Message}");
            }
        }

        private async Task<bool> UpdateChatNameInDatabase(string chatId, string newChatName)
        {
            try
            {
                return await presenter.UpdateChatNameAsync(chatId, newChatName);
            }
            catch (Exception ex)
            {
                ShowChatManagementError($"Datenbankfehler beim Aktualisieren des Chat-Namens: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> UpdateChatStatusInDatabase(string chatId, bool isPrivate)
        {
            try
            {
                return await presenter.UpdateChatStatusAsync(chatId, isPrivate);
            }
            catch (Exception ex)
            {
                ShowChatManagementError($"Datenbankfehler beim Aktualisieren des Chat-Status: {ex.Message}");
                return false;
            }
        }

        private async Task RefreshChatList()
        {
            try
            {
                if (presenter != null)
                {
                    // Lade die Chats für den aktuellen Benutzer neu aus der Datenbank
                    await presenter.RefreshUserChatsFromDatabase();
                }
            }
            catch (Exception ex)
            {
                ShowChatManagementError($"Fehler beim Aktualisieren der Chat-Liste: {ex.Message}");
            }
        }

        private async void AddUserToChat_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = GetSelectedChatroomIndex();
            if (selectedIndex < 0)
            {
                ShowChatManagementError("Bitte wählen Sie einen Chat aus.");
                return;
            }

            try
            {
                var chatId = await GetSelectedChatId();
                if (chatId == null) return;

                var isAdmin = await presenter.IsCurrentUserChatAdmin(chatId);
                if (!isAdmin)
                {
                    ShowChatManagementError("Nur der Chat-Administrator kann Benutzer hinzufügen.");
                    return;
                }

                var availableUsers = await presenter.GetAvailableUsersForChat(chatId);
                if (!availableUsers.Any())
                {
                    ShowChatManagementError("Keine verfügbaren Benutzer zum Hinzufügen gefunden.");
                    return;
                }

                var userToAdd = ShowUserSelectionDialog("Benutzer zum Chat hinzufügen", availableUsers);
                if (userToAdd != null)
                {
                    var success = await presenter.AddUserToChatAsAdmin(chatId, userToAdd.UserId);
                    if (success)
                    {
                        ShowChatManagementSuccess($"Benutzer '{userToAdd.UserName}' wurde erfolgreich zum Chat hinzugefügt.");
                        
                        // Chat-Liste aktualisieren
                        await RefreshChatList();
                    }
                    else
                    {
                        ShowChatManagementError("Fehler beim Hinzufügen des Benutzers.");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowChatManagementError($"Fehler beim Hinzufügen des Benutzers: {ex.Message}");
            }
        }

        private async void RemoveUserFromChat_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = GetSelectedChatroomIndex();
            if (selectedIndex < 0)
            {
                ShowChatManagementError("Bitte wählen Sie einen Chat aus.");
                return;
            }

            try
            {
                var chatId = await GetSelectedChatId();
                if (chatId == null) return;

                var isAdmin = await presenter.IsCurrentUserChatAdmin(chatId);
                if (!isAdmin)
                {
                    ShowChatManagementError("Nur der Chat-Administrator kann Benutzer entfernen.");
                    return;
                }

                ShowChatManagementError("Funktion wird implementiert. Verwenden Sie 'Chat-Teilnehmer anzeigen' um die Teilnehmer zu sehen.");
            }
            catch (Exception ex)
            {
                ShowChatManagementError($"Fehler beim Entfernen des Benutzers: {ex.Message}");
            }
        }

        private async void ShowChatParticipants_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = GetSelectedChatroomIndex();
            if (selectedIndex < 0)
            {
                ShowChatManagementError("Bitte wählen Sie einen Chat aus.");
                return;
            }

            try
            {
                var chatId = await GetSelectedChatId();
                if (chatId == null) return;

                var participants = await presenter.GetChatParticipantsAsync(chatId);
                var participantsList = string.Join("\n• ", participants);
                
                var isAdmin = await presenter.IsCurrentUserChatAdmin(chatId);
                var adminText = isAdmin ? "\n\n[Sie sind Administrator dieses Chats]" : "";
                
                MessageBox.Show($"Chat-Teilnehmer:\n\n• {participantsList}{adminText}", 
                              "Chat-Teilnehmer", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowChatManagementError($"Fehler beim Laden der Chat-Teilnehmer: {ex.Message}");
            }
        }

        private User? ShowUserSelectionDialog(string title, List<User> users)
        {
            var selectionWindow = new Window
            {
                Title = title,
                Width = 350,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var promptLabel = new TextBlock
            {
                Text = "Wählen Sie einen Benutzer aus:",
                Margin = new Thickness(20, 20, 20, 10),
                FontSize = 14
            };
            Grid.SetRow(promptLabel, 0);

            var userListBox = new ListBox
            {
                Margin = new Thickness(20, 0, 20, 10),
                DisplayMemberPath = "UserName"
            };
            userListBox.ItemsSource = users;
            Grid.SetRow(userListBox, 1);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 0, 20, 20)
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 75,
                Height = 25,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };

            var cancelButton = new Button
            {
                Content = "Abbrechen",
                Width = 75,
                Height = 25,
                IsCancel = true
            };

            okButton.Click += (s, e) => {
                if (userListBox.SelectedItem != null)
                {
                    selectionWindow.Tag = userListBox.SelectedItem;
                    selectionWindow.DialogResult = true;
                }
                selectionWindow.Close();
            };

            cancelButton.Click += (s, e) => {
                selectionWindow.DialogResult = false;
                selectionWindow.Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(promptLabel);
            grid.Children.Add(userListBox);
            grid.Children.Add(buttonPanel);

            selectionWindow.Content = grid;

            return selectionWindow.ShowDialog() == true ? selectionWindow.Tag as User : null;
        }

        // NEU: Window Closing Handler
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            messageRefreshTimer?.Stop();
            base.OnClosing(e);
        }
    }
}