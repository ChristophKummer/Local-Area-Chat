using Local_Area_Chat.Dialogs;
using Local_Area_Chat.MVP;
using Local_Area_Chat.Data;
using Local_Area_Chat.Config;
using Local_Area_Chat.Security;
using Local_Area_Chat.MVP.ViewModels;
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
    /// <summary>
    /// WPF View-Klasse für MVP-Pattern - implementiert IMainView Interface
    /// Hauptfenster für Local Area Chat mit verschlüsselter MongoDB-Kommunikation
    /// </summary>
    /// 
    // Raspberry SSH-PW: ITS2025
    // sudo ip addr add 192.168.1.2/24 dev eth0

    //String fuer MongoDB Compass
    //mongodb://Admin:Admin@192.168.1.2:27017/?authSource=admin

    public partial class MainWindow : Window, IMainView
    {
        private MainPresenter presenter;
        private MongoRepository repository;
        private DispatcherTimer? loginCloseTimer;
        private DispatcherTimer? messageRefreshTimer;
        private string? currentChatId;

        public MainWindow()
        {
            InitializeComponent();
            ConfigureMessagesListBox();
            InitializeDatabaseConnection();
            presenter = new MainPresenter(this, repository);
            ConfigureMessagesListBox();
            InitializeMessageRefreshTimer();
        }

        /// <summary>
        /// Stellt Verbindung zur MongoDB auf Raspberry Pi her
        /// </summary>
        private async void InitializeDatabaseConnection()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Starte Datenbankverbindung zum Raspberry Pi...");
                
                var (repo, message) = await DatabaseConfig.ConnectToDatabase();
                repository = repo;
                
                if (repository != null)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ {message}");
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
                System.Diagnostics.Debug.WriteLine($"❌ Datenbankverbindung fehlgeschlagen: {ex.Message}");
                
                MessageBox.Show($"MongoDB-Verbindung zum Raspberry Pi fehlgeschlagen: {ex.Message}\n\n" +
                              "✅ Raspberry Pi MongoDB Status:\n" +
                              "   MongoDB läuft und ist bereit für Verbindungen\n" +
                              "   IP: 192.168.1.2:27017\n\n" +
                              "🔍 Mögliche Lösungen:\n" +
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
        
        /// <summary>
        /// Initialisiert Timer für automatische Message-Updates (alle 3 Sekunden)
        /// </summary>
        private void InitializeMessageRefreshTimer()
        {
            messageRefreshTimer = new DispatcherTimer();
            messageRefreshTimer.Interval = TimeSpan.FromSeconds(3);
            messageRefreshTimer.Tick += MessageRefreshTimer_Tick;
        }

        /// <summary>
        /// Timer-Event: Aktualisiert Nachrichten automatisch wenn Chat aktiv
        /// </summary>
        private async void MessageRefreshTimer_Tick(object? sender, EventArgs e)
        {
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

        /// <summary>
        /// Konfiguriert MessagesListBox für Textwrapping und Scrollverhalten
        /// </summary>
        private void ConfigureMessagesListBox()
        {
            var itemTemplate = new DataTemplate();
            
            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding());
            textBlockFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            textBlockFactory.SetValue(TextBlock.MarginProperty, new Thickness(5, 2, 5, 2));
            textBlockFactory.SetValue(TextBlock.PaddingProperty, new Thickness(5));
            
            itemTemplate.VisualTree = textBlockFactory;
            MessagesListBox.ItemTemplate = itemTemplate;
            
            System.Windows.Controls.ScrollViewer.SetHorizontalScrollBarVisibility(MessagesListBox, ScrollBarVisibility.Disabled);
            System.Windows.Controls.ScrollViewer.SetVerticalScrollBarVisibility(MessagesListBox, ScrollBarVisibility.Auto);
        }

        #region IMainView Implementation

        /// <summary>
        /// Setzt Chat-Liste in der UI und wählt ersten Chat aus
        /// </summary>
        public void SetChatrooms(List<string> chatrooms)
        {
            ChatroomListBox.ItemsSource = chatrooms;
            if (chatrooms.Count > 0)
                ChatroomListBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Zeigt Nachrichten in der UI mit automatischem Textwrapping
        /// </summary>
        public void SetMessages(List<string> messages)
        {
            MessagesListBox.ItemsSource = null;
            MessagesListBox.ItemsSource = messages;
            
            if (MessagesListBox.ItemTemplate == null)
            {
                var itemTemplate = new DataTemplate();
                var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                textBlockFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding());
                textBlockFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
                textBlockFactory.SetValue(TextBlock.MarginProperty, new Thickness(5, 2, 5, 2));
                
                itemTemplate.VisualTree = textBlockFactory;
                MessagesListBox.ItemTemplate = itemTemplate;
                
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

        /// <summary>
        /// Zeigt Benutzername in der UI an und startet Message-Timer
        /// </summary>
        public void SetCurrentUserDisplay(string username)
        {
            UserNameTextBlock.Text = username;
            UserNameTextBlock.Visibility = Visibility.Visible;
            
            if (!string.IsNullOrEmpty(currentChatId))
            {
                messageRefreshTimer?.Start();
            }
        }

        /// <summary>
        /// Versteckt Benutzeranzeige und stoppt Timer
        /// </summary>
        public void ClearCurrentUserDisplay()
        {
            UserNameTextBlock.Text = "";
            UserNameTextBlock.Visibility = Visibility.Collapsed;
            
            messageRefreshTimer?.Stop();
            currentChatId = null;
        }

        public void ShowNewChatError(string message)
        {
            MessageBox.Show(message, "Neuer Chat Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowNewChatSuccess(string chatName)
        {
            MessageBox.Show($"Chat '{chatName}' wurde erfolgreich erstellt!", "Neuer Chat", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowChatManagementError(string message)
        {
            MessageBox.Show(message, "Chat Management Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowChatManagementSuccess(string message)
        {
            MessageBox.Show(message, "Chat Management", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Zeigt Chat-Management-Dialog mit ViewModel-Pattern
        /// </summary>
        public void ShowChatManagementDialog(ChatManagementViewModel viewModel)
        {
            var dialog = new ChatManagementDialog(
                viewModel.ChatName, 
                viewModel.IsPrivate, 
                viewModel.Participants, 
                viewModel.AvailableUsers, 
                viewModel.AdminName);
            
            viewModel.DialogResult = dialog.ShowDialog() == true;
            
            if (viewModel.DialogResult)
            {
                viewModel.NewChatName = dialog.NewChatName;
                viewModel.NewIsPrivate = dialog.IsPrivate;
                viewModel.AddedUsers = dialog.AddedUsers;
                viewModel.RemovedUsers = dialog.RemovedUsers;
                viewModel.DeleteChat = dialog.DeleteChat;
            }
        }

        /// <summary>
        /// Zeigt Input-Dialog mit ViewModel-Pattern
        /// </summary>
        public void ShowInputDialog(InputDialogViewModel viewModel)
        {
            var result = ShowInputDialog(viewModel.Title, viewModel.Prompt);
            viewModel.DialogResult = !string.IsNullOrEmpty(result);
            viewModel.InputValue = result ?? "";
        }

        /// <summary>
        /// Zeigt Benutzer-Auswahl-Dialog mit ViewModel-Pattern
        /// </summary>
        public void ShowUserSelectionDialog(UserSelectionViewModel viewModel)
        {
            var user = ShowUserSelectionDialog(viewModel.Title, viewModel.AvailableUsers);
            viewModel.DialogResult = user != null;
            viewModel.SelectedUser = user;
        }

        /// <summary>
        /// Erstellt dynamischen Input-Dialog für Texteingaben
        /// </summary>
        public string ShowInputDialog(string title, string prompt)
        {
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

        /// <summary>
        /// Erstellt dynamischen Benutzer-Auswahl-Dialog
        /// </summary>
        public User? ShowUserSelectionDialog(string title, List<User> users)
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

        /// <summary>
        /// Aktualisiert ausgewählten Chat-Namen in der UI
        /// </summary>
        public void UpdateSelectedChatroom(string chatroomName)
        {
            SelectedChatroomTextBlock.Text = chatroomName;
        }

        public void StartMessageRefresh()
        {
            messageRefreshTimer?.Start();
        }

        public void StopMessageRefresh()
        {
            messageRefreshTimer?.Stop();
        }

        /// <summary>
        /// Aktiviert/deaktiviert Admin-spezifische UI-Optionen
        /// </summary>
        public void ShowAdminOptions(bool isAdmin)
        {
            System.Diagnostics.Debug.WriteLine($"Admin-Optionen {(isAdmin ? "aktiviert" : "deaktiviert")}");
        }

        /// <summary>
        /// Lädt Chat-Liste asynchron aus der Datenbank neu
        /// </summary>
        public void RefreshChatList()
        {
            _ = Task.Run(async () => await presenter.RefreshUserChatsFromDatabase());
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Chat-Auswahl geändert: Lädt Nachrichten und startet Timer
        /// </summary>
        private void ChatroomListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (presenter != null)
            {
                var selectedIndex = GetSelectedChatroomIndex();
                presenter.OnChatroomChanged(selectedIndex);

                if (selectedIndex >= 0)
                {
                    Task.Run(async () => {
                        currentChatId = await presenter.GetChatIdByIndex(selectedIndex);
                        
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

        /// <summary>
        /// Login-Popup öffnen und Fokus setzen
        /// </summary>
        private void LoginToggleButton_Click(object sender, RoutedEventArgs e)
        {
            LoginPopup.IsOpen = true;
            UserTextBox.Focus();
        }

        /// <summary>
        /// Auto-Close Timer für Login-Popup starten
        /// </summary>
        private void LoginPopup_MouseLeave(object sender, MouseEventArgs e)
        {
            if (loginCloseTimer == null)
            {
                loginCloseTimer = new DispatcherTimer();
                loginCloseTimer.Interval = TimeSpan.FromSeconds(1);
                loginCloseTimer.Tick += LoginCloseTimer_Tick;
            }
            loginCloseTimer.Start();
        }

        /// <summary>
        /// Auto-Close Timer für Login-Popup stoppen
        /// </summary>
        private void LoginPopup_MouseEnter(object sender, MouseEventArgs e)
        {
            loginCloseTimer?.Stop();
        }

        /// <summary>
        /// Login-Popup automatisch schließen nach Timeout
        /// </summary>
        private void LoginCloseTimer_Tick(object? sender, EventArgs e)
        {
            loginCloseTimer?.Stop();
            LoginPopup.IsOpen = false;
            UserTextBox.Text = "";
            PasswordBox.Password = "";
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
            => await presenter.HandleUserLogin(GetLoginUsername(), GetLoginPassword());

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
            => await presenter.HandleUserRegistration();

        private async void NewChatButton_Click(object sender, RoutedEventArgs e)
            => await presenter.HandleNewChatCreation();

        private async void ManageChat_Click(object sender, RoutedEventArgs e)
            => await presenter.HandleChatManagement(GetSelectedChatroomIndex());

        private async void AddUserToChat_Click(object sender, RoutedEventArgs e)
            => await presenter.HandleUserAddition(GetSelectedChatroomIndex());

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            HamburgerButton.ContextMenu.IsOpen = true;
        }

        private void HamburgerMenu_Profile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Profil geöffnet");
            ProfilePanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Logout mit Sicherheitsabfrage und Verschlüsselungs-Cleanup
        /// </summary>
        private void HamburgerMenu_Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Möchten Sie sich wirklich abmelden?", "Abmelden", 
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                ChatEncryption.ClearAllChatKeys();
                System.Diagnostics.Debug.WriteLine("🔒 Alle Chat-Schlüssel entfernt");
                
                presenter.Logout();
                MessageBox.Show("Sie wurden erfolgreich abgemeldet", "Abgemeldet", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ProfileSaveButton_Click(object sender, RoutedEventArgs e)
        {
            ProfilePanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Profil-Panel schließen und Felder zurücksetzen
        /// </summary>
        private void ProfileCloseButton_Click(object sender, RoutedEventArgs e)
        {
            ProfileUserTextBox.Text = "";
            ProfilePasswordTextBox.Text = "";
            ProfileRoleComboBox.SelectedIndex = -1;
            ProfilePanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Enter-Taste sendet Nachricht
        /// </summary>
        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        /// <summary>
        /// Enter/Tab wechselt zu Passwort-Feld
        /// </summary>
        private void UserTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                PasswordBox.Focus();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Enter startet Login-Prozess
        /// </summary>
        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        /// <summary>
        /// Nachricht bearbeiten: Prüft Berechtigung und öffnet Dialog
        /// </summary>
        private async void EditMessage_Click(object sender, RoutedEventArgs e)
        {
            var chatroomIndex = GetSelectedChatroomIndex();
            if (chatroomIndex < 0) return;
            
            var selectedIndex = MessagesListBox.SelectedIndex;
            if (selectedIndex < 0) return;

            var selectedChatName = ChatroomListBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedChatName)) return;

            var chatMessages = await presenter.GetMessagesForEdit(selectedChatName);
            if (selectedIndex >= chatMessages.Count) return;
            
            var selectedMessage = chatMessages[selectedIndex];

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

        /// <summary>
        /// Ermittelt Chat-ID des aktuell ausgewählten Chats
        /// </summary>
        private async Task<string?> GetSelectedChatId()
        {
            var selectedIndex = GetSelectedChatroomIndex();
            if (selectedIndex < 0) return null;

            var selectedChatName = ChatroomListBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedChatName)) return null;

            if (presenter != null && repository != null)
            {
                var allChats = await repository.GetAllChatsAsync();
                var chat = allChats.FirstOrDefault(c => c.ChatName == selectedChatName);
                return chat?.ChatId;
            }

            return null;
        }

        /// <summary>
        /// Benutzer aus Chat entfernen (Admin-Funktion)
        /// </summary>
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

        /// <summary>
        /// Zeigt alle Teilnehmer des ausgewählten Chats an
        /// </summary>
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

        #endregion

        /// <summary>
        /// Cleanup beim Schließen: Timer und Verschlüsselungs-Cache leeren
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            messageRefreshTimer?.Stop();
            ChatEncryption.ClearAllChatKeys();
            base.OnClosing(e);
        }
    }
}