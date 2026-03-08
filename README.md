# SalonKrasotok / Салон красоты

Русская версия ниже, English version follows.

---

## RU

### О проекте
`SalonKrasotok` — desktop-приложение на **WPF (.NET 9)** для автоматизации салона красоты.  
Проект включает управление клиентами, услугами, записями, материалами и базовую финансовую статистику.

### Основные возможности
- Авторизация пользователей (роли: администратор и менеджер)
- Работа с клиентами (добавление, просмотр, удаление с причиной)
- Запись клиентов на услуги и изменение статуса записи
- Учет материалов и закупок
- Уведомления о низком остатке материалов
- Автоматическое локальное резервное копирование SQLite-базы
- Попытка подключения к серверу API, с fallback на локальный режим

### Технологии
- C#
- .NET 9 (WPF)
- SQLite (`System.Data.SQLite.Core`)
- JSON / HTTP (`Newtonsoft.Json`, `HttpClient`)

### Структура проекта
- `SalonKrasot/` — исходный код приложения
- `SalonKrasot.slnx` — solution-файл
- `.gitignore` — исключения для сборочных и локальных файлов

### Быстрый старт
1. Установите **.NET SDK 9+**.
2. В корне проекта выполните:

```bash
dotnet restore
dotnet build SalonKrasot.slnx
dotnet run --project SalonKrasot/SalonKrasot.csproj
```

### Переменные окружения (рекомендуется)
Для безопасной публикации секреты вынесены в переменные окружения:

- `SALON_SERVER_URL` — URL backend API для уведомлений/синхронизации
- `SALON_ENCRYPTION_KEY` — ключ шифрования
- `SALON_PASSWORD_SALT` — соль для хеширования паролей
- `SALON_ADMIN_PASSWORD` — пароль администратора при первичной инициализации БД
- `SALON_MANAGER_PASSWORD` — пароль менеджера при первичной инициализации БД

Пример (PowerShell):

```powershell
$env:SALON_SERVER_URL="http://127.0.0.1:5000"
$env:SALON_ENCRYPTION_KEY="your_32+_char_secret_key"
$env:SALON_PASSWORD_SALT="your_unique_salt"
$env:SALON_ADMIN_PASSWORD="StrongAdminPassword"
$env:SALON_MANAGER_PASSWORD="StrongManagerPassword"
```

Важно:
- Если переменные не заданы, используются fallback-значения для локального запуска.
- Пароли `SALON_ADMIN_PASSWORD` и `SALON_MANAGER_PASSWORD` применяются при создании новой БД.

### Безопасность и GitHub
- В репозиторий не должны попадать `bin/`, `obj/`, `.vs/`, `*.db`, `Backups/`, `*.user`.
- Не храните реальные пароли, ключи и URL в исходниках.

### Статус проекта
Учебный проект. Возможны доработки архитектуры, UI и обработки nullable-предупреждений.

---

## EN

### About
`SalonKrasotok` is a **WPF desktop app (.NET 9)** for beauty salon management.  
It covers clients, services, appointments, materials, and basic financial statistics.

### Key Features
- User authentication (roles: admin and manager)
- Client management (create, list, delete with reason)
- Appointment scheduling and status updates
- Material inventory and purchase tracking
- Low-stock notifications
- Automatic local SQLite backups
- API server connection attempt with local fallback mode

### Tech Stack
- C#
- .NET 9 (WPF)
- SQLite (`System.Data.SQLite.Core`)
- JSON / HTTP (`Newtonsoft.Json`, `HttpClient`)

### Project Structure
- `SalonKrasot/` — application source code
- `SalonKrasot.slnx` — solution file
- `.gitignore` — build/local ignore rules

### Quick Start
1. Install **.NET SDK 9+**.
2. Run in the project root:

```bash
dotnet restore
dotnet build SalonKrasot.slnx
dotnet run --project SalonKrasot/SalonKrasot.csproj
```

### Environment Variables (recommended)
Sensitive configuration is externalized via environment variables:

- `SALON_SERVER_URL` — backend API URL for notifications/sync
- `SALON_ENCRYPTION_KEY` — encryption key
- `SALON_PASSWORD_SALT` — password hashing salt
- `SALON_ADMIN_PASSWORD` — admin password on first DB initialization
- `SALON_MANAGER_PASSWORD` — manager password on first DB initialization

PowerShell example:

```powershell
$env:SALON_SERVER_URL="http://127.0.0.1:5000"
$env:SALON_ENCRYPTION_KEY="your_32+_char_secret_key"
$env:SALON_PASSWORD_SALT="your_unique_salt"
$env:SALON_ADMIN_PASSWORD="StrongAdminPassword"
$env:SALON_MANAGER_PASSWORD="StrongManagerPassword"
```

Notes:
- If variables are not set, local fallback defaults are used.
- `SALON_ADMIN_PASSWORD` and `SALON_MANAGER_PASSWORD` are applied when a new DB is created.

### Security and GitHub
- Do not commit `bin/`, `obj/`, `.vs/`, `*.db`, `Backups/`, `*.user`.
- Never store real credentials, keys, or private URLs in source code.

### Project Status
Educational project. Architecture/UI polish and nullable warning cleanup are planned improvements.
