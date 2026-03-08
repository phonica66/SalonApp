using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SalonApp
{
    /// <summary>
    /// Оптимизированная работа с SQLite БЕЗ шифрования.
    /// ✅ Версия без шифрования
    /// </summary>
    public static class WpfDatabase
    {
        // Конфиг
        private static readonly string DatabasePath = "salon_server.db";

        // Шифрование

        // Сервер
        private static readonly HttpClient httpClient = new HttpClient();
        private static string serverUrl = null;
        private static bool serverConnected = false;
        private static readonly string PooledConnectionString =
            $"Data Source={DatabasePath};Version=3;Pooling=True;Max Pool Size=100;";
        private static readonly string EncryptionKey =
            Environment.GetEnvironmentVariable("SALON_ENCRYPTION_KEY")
            ?? "CHANGE_ME_SALON_ENCRYPTION_KEY_32!";
        private static byte[] KeyBytes; // <-- Добавь это

        // ✅ Static конструктор
        static WpfDatabase()
        {
            try
            {

                var key = EncryptionKey;
                if (key.Length < 32)
                    key = key.PadRight(32, ' ');
                KeyBytes = Encoding.UTF8.GetBytes(key.Substring(0, 32));

                if (!File.Exists(DatabasePath))
                    Initialize();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка инициализации WpfDatabase: {ex.Message}", ex);
            }
        }

        #region --- Password Hashing ---

        public static string HashData(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            var saltValue = Environment.GetEnvironmentVariable("SALON_PASSWORD_SALT")
                ?? "CHANGE_ME_SALON_PASSWORD_SALT";
            var salt = Encoding.UTF8.GetBytes(saltValue);
            using var kdf = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            var hash = kdf.GetBytes(32);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        #endregion

        #region --- Connection Helpers ---

        private static SQLiteConnection GetConnection()
        {
            var conn = new SQLiteConnection($"Data Source={DatabasePath};Version=3");
            conn.Open();
            return conn;
        }

        private static int ExecuteNonQuery(
            string query,
            IEnumerable<KeyValuePair<string, object>> parameters = null,
            SQLiteConnection externalConnection = null,
            SQLiteTransaction externalTransaction = null)
        {
            var useExternal = externalConnection != null;
            using var conn = useExternal ? null : GetConnection();
            using var cmd = useExternal
                ? new SQLiteCommand(query, externalConnection, externalTransaction)
                : new SQLiteCommand(query, conn);

            if (parameters != null)
            {
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
            }

            return cmd.ExecuteNonQuery();
        }

        private static object ExecuteScalar(
            string query,
            IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            using var conn = GetConnection();
            using var cmd = new SQLiteCommand(query, conn);

            if (parameters != null)
            {
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
            }

            return cmd.ExecuteScalar();
        }

        private static DataTable ExecuteDataTable(
            string query,
            IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            using var conn = GetConnection();
            using var cmd = new SQLiteCommand(query, conn);

            if (parameters != null)
            {
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
            }

            using var adapter = new SQLiteDataAdapter(cmd);
            var table = new DataTable();
            adapter.Fill(table);
            return table;
        }

        #endregion

        #region --- Database Initialization ---

        public static void Initialize()
        {
            try
            {
                if (!File.Exists(DatabasePath))
                    SQLiteConnection.CreateFile(DatabasePath);

                CreateTables();
                CreateDefaultData();
            }
            catch (Exception ex)
            {
                throw new Exception($"Database initialization failed: {ex.Message}", ex);
            }
        }

        private static void CreateTables()
        {
            var commands = new[]
            {
                @"CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    username TEXT UNIQUE NOT NULL,
                    passwordhash TEXT NOT NULL,
                    role TEXT NOT NULL,
                    fullname TEXT,
                    email TEXT UNIQUE,
                    securityquestion TEXT,
                    securityanswer TEXT,
                    createdat TEXT DEFAULT CURRENT_TIMESTAMP,
                    isactive INTEGER DEFAULT 1,
                    lastlogin TEXT,
                    logincount INTEGER DEFAULT 0
                )",
                @"CREATE TABLE IF NOT EXISTS clients (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    phone TEXT UNIQUE NOT NULL,
                    email TEXT,
                    createdat TEXT DEFAULT CURRENT_TIMESTAMP,
                    isdeleted INTEGER DEFAULT 0,
                    deletereason TEXT
                )",
                @"CREATE TABLE IF NOT EXISTS services (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    price TEXT NOT NULL,
                    duration INTEGER NOT NULL,
                    description TEXT,
                    category TEXT,
                    isactive INTEGER DEFAULT 1,
                    createdat TEXT DEFAULT CURRENT_TIMESTAMP
                )",
                @"CREATE TABLE IF NOT EXISTS appointments (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    clientid INTEGER NOT NULL,
                    serviceid INTEGER NOT NULL,
                    appointmentdatetime TEXT NOT NULL,
                    status TEXT DEFAULT 'Новая',
                    notes TEXT,
                    createdat TEXT DEFAULT CURRENT_TIMESTAMP,
                    completedat TEXT,
                    FOREIGN KEY(clientid) REFERENCES clients(id),
                    FOREIGN KEY(serviceid) REFERENCES services(id)
                )",
                @"CREATE TABLE IF NOT EXISTS materials (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    currentstock INTEGER NOT NULL DEFAULT 0,
                    minstock INTEGER NOT NULL DEFAULT 10,
                    unit TEXT DEFAULT 'шт',
                    category TEXT,
                    supplier TEXT,
                    costperunit TEXT,
                    createdat TEXT DEFAULT CURRENT_TIMESTAMP,
                    lastupdated TEXT DEFAULT CURRENT_TIMESTAMP
                )",
                @"CREATE TABLE IF NOT EXISTS purchases (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    materialid INTEGER NOT NULL,
                    quantity INTEGER NOT NULL,
                    costperunit TEXT NOT NULL,
                    totalcost TEXT NOT NULL,
                    supplier TEXT,
                    purchasedate TEXT DEFAULT CURRENT_TIMESTAMP,
                    managerid INTEGER NOT NULL,
                    invoicenumber TEXT,
                    notes TEXT,
                    FOREIGN KEY(materialid) REFERENCES materials(id),
                    FOREIGN KEY(managerid) REFERENCES users(id)
                )",
                @"CREATE TABLE IF NOT EXISTS notifications (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    fromuserid INTEGER NOT NULL,
                    touserid INTEGER NOT NULL,
                    message TEXT NOT NULL,
                    materialid INTEGER,
                    notificationtype TEXT DEFAULT 'material_low',
                    isread INTEGER DEFAULT 0,
                    createdat TEXT DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(fromuserid) REFERENCES users(id),
                    FOREIGN KEY(touserid) REFERENCES users(id)
                )",
                @"CREATE TABLE IF NOT EXISTS deletion_log (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    deleteduserid INTEGER NOT NULL,
                    deleted_username TEXT NOT NULL,
                    adminid INTEGER NOT NULL,
                    reason TEXT NOT NULL,
                    deletedat TEXT DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(adminid) REFERENCES users(id)
                )"
            };

            using var conn = GetConnection();
            foreach (var sql in commands)
            {
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
        }

        private static void CreateDefaultData()
        {
            using var conn = GetConnection();
            using var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM users WHERE isactive = 1", conn);
            var count = Convert.ToInt32(checkCmd.ExecuteScalar());

            if (count > 0) return;

            using var transaction = conn.BeginTransaction();
            try
            {
                // Пользователи
                var adminPassword = Environment.GetEnvironmentVariable("SALON_ADMIN_PASSWORD")
                    ?? "ChangeMe_Admin_123!";
                var managerPassword = Environment.GetEnvironmentVariable("SALON_MANAGER_PASSWORD")
                    ?? "ChangeMe_Manager_123!";

                var users = new[]
                {
                    ("admin", adminPassword, "Администратор", "Администратор"),
                    ("manager", managerPassword, "Менеджер", "Менеджер салона")
                };

                foreach (var (username, password, role, fullName) in users)
                {
                    var insertUser = @"INSERT INTO users (username, passwordhash, role, fullname)
                                      VALUES (@username, @passwordhash, @role, @fullName)";

                    ExecuteNonQuery(insertUser, new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@username", username),
                        new KeyValuePair<string, object>("@passwordhash", HashData(password)),
                        new KeyValuePair<string, object>("@role", role),
                        new KeyValuePair<string, object>("@fullName", fullName)
                    }, conn, transaction);
                }

                // Услуги
                var services = new[]
                {
                    ("Стрижка женская", "1500", 60, "Парикмахерские услуги", "Классическая женская стрижка"),
                    ("Стрижка мужская", "800", 30, "Парикмахерские услуги", "Мужская стрижка"),
                    ("Окрашивание волос", "3000", 120, "Окрашивание", "Профессиональное окрашивание"),
                    ("Маникюр классический", "1200", 60, "Маникюр", "Классический маникюр"),
                    ("Педикюр", "1500", 75, "Педикюр", "Классический педикюр"),
                    ("Массаж лица", "2000", 45, "Косметология", "Расслабляющий массаж лица"),
                    ("Чистка лица", "2500", 90, "Косметология", "Глубокая чистка лица")
                };

                foreach (var (name, price, duration, category, desc) in services)
                {
                    var insertService = @"INSERT INTO services (name, price, duration, description, category)
                                         VALUES (@name, @price, @duration, @desc, @category)";

                    ExecuteNonQuery(insertService, new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@name", name),
                        new KeyValuePair<string, object>("@price", price),
                        new KeyValuePair<string, object>("@duration", duration),
                        new KeyValuePair<string, object>("@desc", desc),
                        new KeyValuePair<string, object>("@category", category)
                    }, conn, transaction);
                }

                // Материалы
                var materials = new[]
                {
                    ("Краска для волос", 25, 10, "шт", "Окрашивание", "L'Oreal", "450"),
                    ("Шампунь профессиональный", 15, 5, "л", "Уход", "Matrix", "800"),
                    ("Лак для ногтей", 50, 15, "шт", "Маникюр", "OPI", "320"),
                    ("Пилочки для ногтей", 100, 30, "шт", "Маникюр", "Staleks", "45"),
                    ("Крем для лица", 8, 3, "шт", "Косметология", "Vichy", "1200"),
                    ("Полотенца одноразовые", 200, 50, "шт", "Расходники", "EcoLux", "12"),
                    ("Перчатки нитриловые", 500, 100, "шт", "Расходники", "MedGlove", "8")
                };

                foreach (var (name, stock, minStock, unit, category, supplier, cost) in materials)
                {
                    var insertMaterial = @"INSERT INTO materials (name, currentstock, minstock, unit, category, supplier, costperunit)
                                          VALUES (@name, @stock, @min, @unit, @category, @supplier, @cost)";

                    ExecuteNonQuery(insertMaterial, new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@name", name),
                        new KeyValuePair<string, object>("@stock", stock),
                        new KeyValuePair<string, object>("@min", minStock),
                        new KeyValuePair<string, object>("@unit", unit),
                        new KeyValuePair<string, object>("@category", category),
                        new KeyValuePair<string, object>("@supplier", supplier),
                        new KeyValuePair<string, object>("@cost", cost)
                    }, conn, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        #endregion

        #region --- Login & Authentication ---

        public static async Task<string> LoginAsync(string username, string password)
        {
            try
            {
                if (serverConnected && !string.IsNullOrEmpty(serverUrl))
                {
                    var remote = await LoginServerAsync(username, password);
                    if (!string.IsNullOrEmpty(remote))
                        return remote;
                }
                return LoginLocal(username, password);
            }
            catch
            {
                return LoginLocal(username, password);
            }
        }

        private static async Task<string> LoginServerAsync(string username, string password)
        {
            try
            {
                var loginData = new { username, password };
                var json = JsonConvert.SerializeObject(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"{serverUrl}/api/login", content);

                if (!response.IsSuccessStatusCode) return null;

                var responseText = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseText);
                return result?.role?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string LoginLocal(string username, string password)
        {
            var query = @"SELECT role FROM users 
                        WHERE username = @username 
                        AND passwordhash = @passwordhash 
                        AND isactive = 1";

            var encryptedRole = ExecuteScalar(query, new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@username", username),
                new KeyValuePair<string, object>("@passwordhash", HashData(password))
            });

            if (encryptedRole == null) return null;
            return encryptedRole.ToString();
        }

        #endregion

        #region --- Server Discovery ---

        public static async Task<bool> ConnectToServerAsync()
        {
            try
            {
                var foundUrl = await FindServerAsync();
                if (!string.IsNullOrEmpty(foundUrl))
                {
                    serverUrl = foundUrl;
                    serverConnected = true;
                    return true;
                }
                serverConnected = false;
                serverUrl = null;
                return false;
            }
            catch
            {
                serverConnected = false;
                serverUrl = null;
                return false;
            }
        }

        public static async Task<string> FindServerAsync()
        {
            var urls = new List<string>
            {
                "http://127.0.0.1:5000",
                "http://localhost:5000"
            };

            try
            {
                var localIp = System.Net.Dns
                    .GetHostAddresses(System.Net.Dns.GetHostName())
                    .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?
                    .ToString();

                if (!string.IsNullOrEmpty(localIp))
                    urls.Add($"http://{localIp}:5000");

                for (int i = 1; i < 255; i++)
                    urls.Add($"http://192.168.1.{i}:5000");
            }
            catch { }

            foreach (var url in urls.Distinct())
            {
                try
                {
                    var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2));
                    var response = await httpClient.GetAsync($"{url}/api/health", cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        serverUrl = url;
                        serverConnected = true;
                        return url;
                    }
                }
                catch { }
            }

            serverConnected = false;
            return null;
        }

        #endregion

        #region --- Clients ---

        public static DataTable GetClients()
        {
            var dt = ExecuteDataTable(@"
                SELECT id, name, phone, email, createdat
                FROM clients WHERE isdeleted = 0 ORDER BY createdat DESC");

            var result = new DataTable();
            result.Columns.Add("ID", typeof(int));
            result.Columns.Add("Имя", typeof(string));
            result.Columns.Add("Телефон", typeof(string));
            result.Columns.Add("Email", typeof(string));
            result.Columns.Add("Дата регистрации", typeof(DateTime));

            foreach (DataRow r in dt.Rows)
            {
                result.Rows.Add(
                    r["id"],
                    r["name"].ToString(),
                    r["phone"].ToString(),
                    r["email"].ToString(),
                    DateTime.Parse(r["createdat"].ToString())
                );
            }
            return result;
        }

        public static bool AddClient(string name, string phone, string email = "")
        {
            try
            {
                var query = @"INSERT INTO clients (name, phone, email) 
                           VALUES (@name, @phone, @email)";

                var inserted = ExecuteNonQuery(query, new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@name", name),
                    new KeyValuePair<string, object>("@phone", phone),
                    new KeyValuePair<string, object>("@email", email)
                });
                return inserted > 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool DeleteClientWithReason(int clientId, string reason, int adminId)
        {
            try
            {
                using var conn = GetConnection();
                using var tx = conn.BeginTransaction();
                try
                {
                    var clientNameObj = ExecuteScalar(
                        "SELECT name FROM clients WHERE id = @id AND isdeleted = 0",
                        new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@id", clientId) }
                    );

                    if (clientNameObj == null) return false;

                    var clientNameEncrypted = clientNameObj.ToString();

                    ExecuteNonQuery(
                        "UPDATE clients SET isdeleted = 1, deletereason = @reason WHERE id = @id",
                        new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@reason", reason),
                            new KeyValuePair<string, object>("@id", clientId)
                        }, conn, tx
                    );

                    ExecuteNonQuery(
                        @"INSERT INTO deletion_log (deleteduserid, deleted_username, adminid, reason)
                         VALUES (@userId, @username, @adminId, @reason)",
                        new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@userId", clientId),
                            new KeyValuePair<string, object>("@username", clientNameEncrypted),
                            new KeyValuePair<string, object>("@adminId", adminId),
                            new KeyValuePair<string, object>("@reason", reason)
                        }, conn, tx
                    );

                    tx.Commit();
                    return true;
                }
                catch
                {
                    tx.Rollback();
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static List<KeyValuePair<int, string>> GetClientsForDropdown()
        {
            var list = new List<KeyValuePair<int, string>>();
            try
            {
                var dt = ExecuteDataTable("SELECT id, name, phone FROM clients WHERE isdeleted = 0 ORDER BY name");
                foreach (DataRow r in dt.Rows)
                {
                    var id = Convert.ToInt32(r["id"]);
                    var name = r["name"].ToString();
                    var phone = r["phone"].ToString();
                    list.Add(new KeyValuePair<int, string>(id, $"{name} ({phone})"));
                }
            }
            catch { }
            return list;
        }

        public static void ValidatePhone(ref string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Телефон не может быть пустым");

            var digits = new string(phone.Where(char.IsDigit).ToArray());

            if (digits.Length == 11 && digits.StartsWith("8"))
                phone = "+7" + digits.Substring(1);
            else if (digits.Length == 10 && digits.StartsWith("9"))
                phone = "+7" + digits;
            else if (digits.Length == 11 && digits.StartsWith("7"))
                phone = "+" + digits;
            else
                throw new ArgumentException("Некорректный формат телефона");
        }

        public static void ValidateEmail(ref string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email не может быть пустым");

            email = email.Trim().ToLowerInvariant();

            // Простая проверка формата
            if (!email.Contains("@") || !email.Contains(".") || email.EndsWith(".") || email.StartsWith("@"))
                throw new ArgumentException("Некорректный формат email");

            // Разбиваем на локальную часть и домен
            var parts = email.Split('@');
            if (parts.Length != 2)
                throw new ArgumentException("Некорректный формат email");

            var domain = parts[1];

            // Белый список разрешённых доменов (можно расширять)
            var allowedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com",
        "googlemail.com",
        "yandex.ru", "ya.ru",
        "mail.ru", "inbox.ru", "list.ru", "bk.ru",
        "outlook.com", "hotmail.com", "live.com",
        "icloud.com", "me.com", "mac.com",
        "rambler.ru",
        "protonmail.com", "proton.me",
        "tut.by", "ukr.net",
        "yahoo.com"
    };

            // Проверяем, есть ли домен в белом списке
            if (!allowedDomains.Contains(domain))
            {
                throw new ArgumentException(
                    $"Email должен быть с реального почтового сервиса!\n\n" +
                    $"Разрешены только популярные домены:\n" +
                    $"• gmail.com\n" +
                    $"• yandex.ru, ya.ru\n" +
                    $"• mail.ru, inbox.ru и др.\n" +
                    $"• outlook.com, hotmail.com\n" +
                    $"• icloud.com\n\n" +
                    $"Введённый домен: @{domain} — не поддерживается.");
            }
        }

        #endregion

        #region --- Materials & Stocks ---

        public static DataTable GetMaterials()
        {
            try
            {
                var dt = ExecuteDataTable(@"
                    SELECT id, name, currentstock, minstock, unit, 
                           category, supplier, costperunit,
                           CASE WHEN currentstock <= minstock THEN 'Заканчивается' ELSE 'В наличии' END as status
                    FROM materials ORDER BY currentstock ASC");

                var result = new DataTable();
                result.Columns.Add("ID", typeof(int));
                result.Columns.Add("Материал", typeof(string));
                result.Columns.Add("Остаток", typeof(int));
                result.Columns.Add("Мин. остаток", typeof(int));
                result.Columns.Add("Единица", typeof(string));
                result.Columns.Add("Категория", typeof(string));
                result.Columns.Add("Поставщик", typeof(string));
                result.Columns.Add("Цена за ед.", typeof(string));
                result.Columns.Add("Статус", typeof(string));

                foreach (DataRow r in dt.Rows)
                {
                    result.Rows.Add(
                        r["id"],
                        r["name"].ToString(),
                        Convert.ToInt32(r["currentstock"]),
                        Convert.ToInt32(r["minstock"]),
                        r["unit"].ToString(),
                        r["category"].ToString(),
                        r["supplier"].ToString(),
                        r["costperunit"].ToString() + " ₽",
                        r["status"].ToString()
                    );
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки материалов: {ex.Message}", ex);
            }
        }

        public static List<KeyValuePair<int, string>> GetMaterialsForDropdown()
        {
            var list = new List<KeyValuePair<int, string>>();
            try
            {
                var dt = ExecuteDataTable("SELECT id, name, currentstock, unit FROM materials ORDER BY name");
                foreach (DataRow r in dt.Rows)
                {
                    var id = Convert.ToInt32(r["id"]);
                    var name = r["name"].ToString();
                    var stock = Convert.ToInt32(r["currentstock"]);
                    var unit = r["unit"].ToString();
                    list.Add(new KeyValuePair<int, string>(id, $"{name} (остаток: {stock} {unit})"));
                }
            }
            catch { }
            return list;
        }

        public static bool SendLowStockNotification(int materialId, int fromAdminId, int toManagerId, string customMessage = "")
        {
            try
            {
                using var conn = GetConnection();
                var cmd = new SQLiteCommand("SELECT name, currentstock, minstock FROM materials WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", materialId);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) return false;

                var materialName = reader["name"].ToString();
                var currentStock = Convert.ToInt32(reader["currentstock"]);
                var minStock = Convert.ToInt32(reader["minstock"]);
                reader.Close();

                var message = string.IsNullOrEmpty(customMessage)
                    ? $"⚠️ ВНИМАНИЕ: Материал '{materialName}' заканчивается!\nТекущий остаток: {currentStock} шт.\nМинимальный уровень: {minStock} шт."
                    : customMessage;

                var insert = @"INSERT INTO notifications (fromuserid, touserid, message, materialid, notificationtype)
                            VALUES (@from, @to, @msg, @mat, @type)";

                var res = ExecuteNonQuery(insert, new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@from", fromAdminId),
                    new KeyValuePair<string, object>("@to", toManagerId),
                    new KeyValuePair<string, object>("@msg", message),
                    new KeyValuePair<string, object>("@mat", materialId),
                    new KeyValuePair<string, object>("@type", "material_low")
                });

                return res > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки уведомления: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region --- Notifications ---

        public static DataTable GetUnreadNotifications(int userId)
        {
            var dt = ExecuteDataTable(@"
                SELECT n.id, n.message, n.createdat,
                       u.fullname as from_user,
                       m.name as material_name,
                       n.notificationtype
                FROM notifications n
                LEFT JOIN users u ON n.fromuserid = u.id
                LEFT JOIN materials m ON n.materialid = m.id
                WHERE n.touserid = @userId AND n.isread = 0",
                new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@userId", userId) });

            var result = new DataTable();
            result.Columns.Add("ID", typeof(int));
            result.Columns.Add("Сообщение", typeof(string));
            result.Columns.Add("От кого", typeof(string));
            result.Columns.Add("Материал", typeof(string));
            result.Columns.Add("Тип", typeof(string));
            result.Columns.Add("Время", typeof(DateTime));

            foreach (DataRow r in dt.Rows)
            {
                result.Rows.Add(
                    r["id"],
                    r["message"].ToString(),
                    r["from_user"] != DBNull.Value ? r["from_user"].ToString() : "Система",
                    r["material_name"] != DBNull.Value ? r["material_name"].ToString() : "-",
                    r["notificationtype"] != DBNull.Value ? r["notificationtype"].ToString() : "",
                    DateTime.Parse(r["createdat"].ToString())
                );
            }
            return result;
        }

        public static bool MarkNotificationAsRead(int notificationId)
        {
            try
            {
                var q = "UPDATE notifications SET isread = 1 WHERE id = @id";
                return ExecuteNonQuery(q, new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@id", notificationId) }) > 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region --- Purchases ---

        public static bool AddPurchase(int materialId, int quantity, decimal costPerUnit, string supplier, int managerId, string invoiceNumber = "", string notes = "")
        {
            try
            {
                using var conn = GetConnection();
                using var tx = conn.BeginTransaction();
                try
                {
                    var totalCost = quantity * costPerUnit;

                    var insertPurchase = @"INSERT INTO purchases (materialid, quantity, costperunit, totalcost, supplier, managerid, invoicenumber, notes)
                                         VALUES (@mat, @qty, @cpu, @total, @sup, @mid, @inv, @notes)";

                    ExecuteNonQuery(insertPurchase, new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@mat", materialId),
                        new KeyValuePair<string, object>("@qty", quantity),
                        new KeyValuePair<string, object>("@cpu", costPerUnit.ToString()),
                        new KeyValuePair<string, object>("@total", totalCost.ToString()),
                        new KeyValuePair<string, object>("@sup", supplier),
                        new KeyValuePair<string, object>("@mid", managerId),
                        new KeyValuePair<string, object>("@inv", invoiceNumber),
                        new KeyValuePair<string, object>("@notes", notes)
                    }, conn, tx);

                    var updateStock = @"UPDATE materials SET currentstock = currentstock + @qty, costperunit = @cpu, supplier = @sup, lastupdated = CURRENT_TIMESTAMP WHERE id = @mat";

                    ExecuteNonQuery(updateStock, new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@qty", quantity),
                        new KeyValuePair<string, object>("@cpu", costPerUnit.ToString()),
                        new KeyValuePair<string, object>("@sup", supplier),
                        new KeyValuePair<string, object>("@mat", materialId)
                    }, conn, tx);

                    tx.Commit();
                    return true;
                }
                catch
                {
                    tx.Rollback();
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static DataTable GetPurchases()
        {
            var dt = ExecuteDataTable(@"
        SELECT p.id, m.name, p.quantity, p.costperunit,
               p.totalcost, p.supplier, p.purchasedate,
               p.invoicenumber, p.notes
        FROM purchases p
        JOIN materials m ON p.materialid = m.id
        ORDER BY p.purchasedate DESC");

            var result = new DataTable();
            result.Columns.Add("ID", typeof(int));
            result.Columns.Add("Материал", typeof(string));
            result.Columns.Add("Количество", typeof(int));
            result.Columns.Add("Цена за ед.", typeof(string));
            result.Columns.Add("Общая стоимость", typeof(string));
            result.Columns.Add("Поставщик", typeof(string));
            result.Columns.Add("purchasedate", typeof(DateTime));
            result.Columns.Add("invoicenumber", typeof(string));
            result.Columns.Add("Заметки", typeof(string));


            foreach (DataRow r in dt.Rows)
            {
                result.Rows.Add(
                    r["id"],
                    r["name"].ToString(),
                    r["quantity"],
                    r["costperunit"].ToString() + " ₽",
                    r["totalcost"].ToString() + " ₽",
                    r["supplier"].ToString(),
                    DateTime.Parse(r["purchasedate"].ToString()),
                    r["invoicenumber"] as string ?? "",
                    r["notes"] as string ?? ""
                );
            }
            return result;

        }


        #endregion

        #region --- Statistics ---

        public static dynamic GetFinancialStatistics()
        {
            try
            {
                using var conn = GetConnection();
                decimal income = 0;
                decimal expenses = 0;

                // Доходы
                using (var cmd = new SQLiteCommand(@"
                    SELECT s.price
                    FROM appointments a
                    JOIN services s ON a.serviceid = s.id
                    WHERE a.status = @status", conn))
                {
                    cmd.Parameters.AddWithValue("@status", "Выполнена");
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var priceStr = reader["price"].ToString();
                        if (decimal.TryParse(priceStr, out var price))
                            income += price;
                    }
                }

                // Расходы
                using (var cmd = new SQLiteCommand("SELECT totalcost FROM purchases", conn))
                {
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var costStr = reader["totalcost"].ToString();
                        if (decimal.TryParse(costStr, out var cost))
                            expenses += cost;
                    }
                }

                var totalClients = Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM clients WHERE isdeleted = 0"));
                var totalAppointments = Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM appointments"));
                var completedAppointments = Convert.ToInt32(ExecuteScalar(@"
                    SELECT COUNT(*) FROM appointments
                    WHERE status = @status",
                    new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@status", "Выполнена") }));

                return new
                {
                    TotalIncome = income,
                    TotalExpenses = expenses,
                    Profit = income - expenses,
                    TotalClients = totalClients,
                    TotalAppointments = totalAppointments,
                    CompletedAppointments = completedAppointments
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения статистики: {ex.Message}", ex);
            }
        }

        #endregion

        #region --- Services ---

        public static List<KeyValuePair<int, string>> GetServicesForDropdown()
        {
            var list = new List<KeyValuePair<int, string>>();
            try
            {
                var dt = ExecuteDataTable("SELECT id, name, price FROM services WHERE isactive = 1 ORDER BY name");
                foreach (DataRow r in dt.Rows)
                {
                    var id = Convert.ToInt32(r["id"]);
                    var name = r["name"].ToString();
                    var price = r["price"].ToString();
                    list.Add(new KeyValuePair<int, string>(id, $"{name} ({price} ₽)"));
                }
            }
            catch { }
            return list;
        }

        #endregion

        #region --- Appointments ---

        public static DataTable GetAppointments()
        {
            var dt = ExecuteDataTable(@"
                SELECT a.id, c.name as client_name, c.phone,
                       s.name as service_name, s.price,
                       a.appointmentdatetime, a.status, a.notes
                FROM appointments a
                JOIN clients c ON a.clientid = c.id
                JOIN services s ON a.serviceid = s.id
                WHERE c.isdeleted = 0");

            var result = new DataTable();
            result.Columns.Add("ID", typeof(int));
            result.Columns.Add("Клиент", typeof(string));
            result.Columns.Add("Телефон", typeof(string));
            result.Columns.Add("Услуга", typeof(string));
            result.Columns.Add("Цена", typeof(string));
            result.Columns.Add("Дата и время", typeof(DateTime));
            result.Columns.Add("Статус", typeof(string));
            result.Columns.Add("Заметки", typeof(string));

            foreach (DataRow r in dt.Rows)
            {
                result.Rows.Add(
                    r["id"],
                    r["client_name"].ToString(),
                    r["phone"].ToString(),
                    r["service_name"].ToString(),
                    r["price"].ToString() + " ₽",
                    DateTime.Parse(r["appointmentdatetime"].ToString()),
                    r["status"].ToString(),
                    r["notes"] as string ?? ""
                );
            }
            return result;
        }

        public static bool AddAppointment(int clientId, int serviceId, DateTime appointmentDateTime)
        {
            try
            {
                var q = @"INSERT INTO appointments (clientid, serviceid, appointmentdatetime, status)
                        VALUES (@clientId, @serviceId, @datetime, @status)";

                return ExecuteNonQuery(q, new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@clientId", clientId),
                    new KeyValuePair<string, object>("@serviceId", serviceId),
                    new KeyValuePair<string, object>("@datetime", appointmentDateTime.ToString("yyyy-MM-dd HH:mm:ss")),
                    new KeyValuePair<string, object>("@status", "Новая")
                }) > 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool UpdateAppointmentStatus(int appointmentId, string newStatus)
        {
            try
            {
                var q = @"UPDATE appointments
                        SET status = @status,
                            completedat = CASE WHEN @status = 'Выполнена' THEN CURRENT_TIMESTAMP ELSE completedat END
                        WHERE id = @id";

                return ExecuteNonQuery(q, new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@status", newStatus),
                    new KeyValuePair<string, object>("@id", appointmentId)
                }) > 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region --- User Helpers ---

        public static int GetUserId(string username)
        {
            var res = ExecuteScalar(
                "SELECT id FROM users WHERE username = @username AND isactive = 1",
                new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@username", username) });

            return res != null ? Convert.ToInt32(res) : -1;
        }

        public static int GetManagerId()
        {
            var res = ExecuteScalar(
                "SELECT id FROM users WHERE role = @role AND isactive = 1 LIMIT 1",
                new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("@role", "Менеджер") });

            return res != null ? Convert.ToInt32(res) : -1;
        }

        #endregion
    }
}
