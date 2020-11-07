using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;

namespace JgBackUp
{
    internal static class Helper
    {
        public static void Backup(string nameDatenbank, string conString, string dateiAusgabe)
        {
            var connection = new SqlConnection(conString);
            var serverConnection = new ServerConnection(connection);
            var server = new Server(serverConnection);
            var database = server.Databases[nameDatenbank];
            Backup backup = new Backup
            {
                Action = BackupActionType.Database,
                BackupSetDescription = $"{nameDatenbank} - full backup",
                BackupSetName = $"{nameDatenbank} backup",
                Database = nameDatenbank,
                PercentCompleteNotification = 10,
                Incremental = false,
                LogTruncation = BackupTruncateLogType.Truncate,
            };

            var deviceItem = new BackupDeviceItem(dateiAusgabe, DeviceType.File);
            backup.Devices.Add(deviceItem);

            backup.Information += (sender, e) => Ausgabe(ConsoleColor.White, $"DB Info: {e.ToString().Replace("Microsoft.Data.SqlClient.SqlError: ", "")}");
            backup.NextMedia += (sender, e) => Ausgabe(ConsoleColor.Gray, $"DB Media: {e.Error}");
            backup.PercentComplete += (sender, e) => Ausgabe(ConsoleColor.White, $"DB Message: {e.Message}");
            backup.Complete += (sender, e) => Ausgabe(ConsoleColor.Yellow, "DB BackUp abgeschlossen.");

            backup.SqlBackup(server);

            return;
        }

        public static void Ausgabe(ConsoleColor farbe, string text)
        {
            Console.ForegroundColor = farbe;
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " " + text);
        }
    }
}
