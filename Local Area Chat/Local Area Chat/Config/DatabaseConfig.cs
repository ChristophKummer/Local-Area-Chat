// Neue Datei: Config/DatabaseConfig.cs
using System;
using System.Threading.Tasks;
using System.Windows;
using Local_Area_Chat.Data;

namespace Local_Area_Chat.Config
{
    public static class DatabaseConfig
    {
        public static readonly ConnectionConfig[] AvailableConnections = new[]
        {
            new ConnectionConfig 
            { 
                IP = "192.168.1.2", 
                Name = "Raspberry Pi (Ethernet)",
                HasAuth = true 
            },
            new ConnectionConfig 
            { 
                IP = "10.0.0.56", 
                Name = "Raspberry Pi (WLAN)",
                HasAuth = true 
            },
            new ConnectionConfig 
            { 
                IP = "localhost", 
                Name = "Lokale MongoDB",
                HasAuth = false 
            }
        };

        public static string GetConnectionString(ConnectionConfig config)
        {
            if (config.HasAuth)
            {
                return $"mongodb://Admin:Admin@{config.IP}:27017/LAC?authSource=admin&connectTimeoutMS=10000&serverSelectionTimeoutMS=10000&retryWrites=true";
            }
            else
            {
                return $"mongodb://{config.IP}:27017/LAC?connectTimeoutMS=10000&serverSelectionTimeoutMS=10000";
            }
        }

        public static async Task<(MongoRepository?, string)> ConnectToDatabase()
        {
            foreach (var config in AvailableConnections)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Versuche Verbindung zu {config.Name} ({config.IP})...");
                    
                    var connectionString = GetConnectionString(config);
                    var repo = new MongoRepository(connectionString, "LAC");
                    
                    // Test connection mit einfachem Ping
                    try
                    {
                        await repo.GetAllChatsAsync();
                        System.Diagnostics.Debug.WriteLine($"Erfolgreich verbunden mit {config.Name}");
                        return (repo, $"? Verbunden mit: {config.Name} ({config.IP})");
                    }
                    catch (Exception testEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Test fehlgeschlagen für {config.Name}: {testEx.Message}");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Verbindung zu {config.Name} fehlgeschlagen: {ex.Message}");
                }
            }
            
            return (null, "Keine Datenbankverbindung verfügbar");
        }
        
        // Synchrone Version für Tests
        public static MongoRepository? TryConnectSync(string ip, bool hasAuth = true)
        {
            try
            {
                var config = new ConnectionConfig { IP = ip, HasAuth = hasAuth };
                var connectionString = GetConnectionString(config);
                return new MongoRepository(connectionString, "LAC");
            }
            catch
            {
                return null;
            }
        }
        
        // Test-Methode für direkten Raspberry Pi Test
        public static async Task<bool> TestRaspberryPiConnection()
        {
            try
            {
                var connectionString = "mongodb://Admin:Admin@192.168.1.2:27017/LAC?authSource=admin&connectTimeoutMS=5000";
                var repo = new MongoRepository(connectionString, "LAC");
                await repo.GetAllChatsAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class ConnectionConfig
    {
        public string IP { get; set; } = "";
        public string Name { get; set; } = "";
        public bool HasAuth { get; set; } = true;
    }
}