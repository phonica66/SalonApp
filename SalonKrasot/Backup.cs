using System;
using System.IO;
using System.Linq;
using System.Timers;
using System.Data.SQLite;

namespace SalonApp
{
    /// <summary>
    /// Автоматическое резервное копирование БД и автоматическое восстановление.
    /// Работает тихо в фоне.
    /// </summary>
    public static class SimpleBackup
    {
        private static System.Timers.Timer timer;
        private static readonly string appFolder = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string dbFile = Path.Combine(appFolder, "salon_server.db");
        private static readonly string backupFolder = Path.Combine(appFolder, "Backups");

        public static void Start()
        {
            try
            {
                Directory.CreateDirectory(backupFolder);

                // Восстановить при необходимости
                RestoreFromBackupIfNeeded();

                // Сохранить первый бэкап при старте
                SaveBackup();

                // Настроить таймер – каждый час
                timer = new System.Timers.Timer(3600000);
                timer.Elapsed += (s, e) => SaveBackup();
                timer.AutoReset = true;
                timer.Start();
            }
            catch { }
        }

        public static void Stop()
        {
            try
            {
                timer?.Stop();
                timer?.Dispose();
            }
            catch { }
        }

        /// <summary>
        /// Попытка восстановить базу из бэкапа, если она отсутствует или повреждена.
        /// </summary>
        private static void RestoreFromBackupIfNeeded()
        {
            try
            {
                // Если нет базы — восстанавливаем
                if (!File.Exists(dbFile))
                {
                    RestoreLatestBackup();
                    return;
                }

                // Проверяем на повреждение — можем ли открыть SQLite
                try
                {
                    using var conn = new SQLiteConnection($"Data Source={dbFile}");
                    conn.Open();
                }
                catch
                {
                    // база битая → восстановление
                    RestoreLatestBackup();
                }
            }
            catch { }
        }

        /// <summary>
        /// Восстановление последнего пригодного бэкапа.
        /// </summary>
        private static void RestoreLatestBackup()
        {
            try
            {
                if (!Directory.Exists(backupFolder))
                    return;

                var latest = Directory
                    .GetFiles(backupFolder, "salon_backup_*.db")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .FirstOrDefault();

                if (latest == null)
                    return; // нет бэкапов

                File.Copy(latest, dbFile, overwrite: true);
            }
            catch { }
        }

        /// <summary>
        /// Создание нового бэкапа и удаление старых.
        /// </summary>
        private static void SaveBackup()
        {
            try
            {
                if (!File.Exists(dbFile))
                    return;

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string backupFile = Path.Combine(backupFolder, $"salon_backup_{timestamp}.db");

                using (FileStream src = new FileStream(dbFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (FileStream dst = new FileStream(backupFile, FileMode.Create, FileAccess.Write))
                {
                    src.CopyTo(dst);
                }

                // Удаление старше 24 часов
                foreach (var file in Directory.GetFiles(backupFolder, "salon_backup_*.db"))
                {
                    if ((DateTime.Now - File.GetLastWriteTime(file)).TotalHours > 24)
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
            }
            catch { }
        }
    }
}
