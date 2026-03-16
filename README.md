# Bitbucket Custom Services

A web-based utility for managing Bitbucket repositories, pull requests, and automated merge workflows with Telegram notifications.

## Features

- **Repository Management**: Manage projects and repositories
- **User Management**: Role-based access control (Admin, Moderator, User)
- **Automated Merges**: Configure cascade merge workflows between branches
- **Telegram Notifications**: Receive notifications for pull request events
- **Web Interface**: Easy-to-use web UI for all operations

## Prerequisites

- **For Local Setup**:
  - .NET 10.0 SDK
  - SQLite (included with .NET)

- **For Docker Setup**:
  - Docker Desktop or Docker Engine
  - Docker Compose

## Quick Start with Docker Compose (Recommended)

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   cd BitbucketCustomServices
   ```

2. **Create seed configuration**:
   ```bash
   cd BitbucketCustomServices
   cp seed-config.example.json seed-config.json
   ```

3. **Edit `seed-config.json`**:
   - Add your users (username, email, password, role)
   - Configure your projects and repositories
   - Add Bitbucket tokens and Telegram credentials (if needed)
   - See the [Seed Configuration](#seed-configuration) section below for detailed options
   - **Note**: The `seed-config.json` file is automatically mounted into the Docker container via volume

4. **Start the application**:
   ```bash
   docker-compose up -d
   ```

5. **Access the application**:
   - Open your browser and navigate to: `http://localhost:7799`
   - Login with your admin credentials (from `seed-config.json` or default: `admin` / `Admin!123`)

6. **Stop the application**:
   ```bash
   docker-compose down
   ```

## Local Setup

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   cd BitbucketCustomServices
   ```

2. **Create seed configuration**:
   ```bash
   cd BitbucketCustomServices
   cp seed-config.example.json seed-config.json
   ```

3. **Edit `seed-config.json`**:
   - Add your users, projects, and repositories
   - Configure Bitbucket tokens and Telegram credentials
   - See the [Seed Configuration](#seed-configuration) section below for detailed options

4. **Restore dependencies and build**:
   ```bash
   dotnet restore
   dotnet build
   ```

5. **Run the application**:
   ```bash
   dotnet run --project BitbucketCustomServices/BitbucketCustomServices.csproj
   ```

6. **Access the application**:
   - Open your browser and navigate to: `http://localhost:7799`
   - Login with your admin credentials

## Testing

Run unit tests:
```bash
dotnet test BitbucketCustomServices.slnx
```

Run tests with code coverage:
```bash
dotnet test BitbucketCustomServices.slnx --collect:"XPlat Code Coverage" --results-directory TestResults
```
Coverage report is written to `TestResults/<run-id>/coverage.cobertura.xml`.

## Seed Configuration

The application uses `seed-config.json` to initialize the database with users, projects, and repositories. This file is **not stored in git** for security reasons and must be created manually.

### Setup

1. Copy the example file:
   ```bash
   cp seed-config.example.json seed-config.json
   ```

2. Edit `seed-config.json` with your actual values:
   - Replace `YOUR_TELEGRAM_BOT_TOKEN` with your Telegram bot token
   - Replace `YOUR_TELEGRAM_CHAT_ID` with your Telegram chat ID
   - **Bitbucket auth** (choose one per repository):
     - `BitbucketToken`: API token (AuthToken auth)
     - `UserEmail` + `UserToken`: Atlassian email and API token (Basic auth for Bitbucket Cloud)
     - `AuthType`: optional `"Basic"` or `"AuthToken"` to set explicitly; otherwise inferred from credentials
   - Update user credentials (username, email, password, role)
   - Configure projects, repositories, and branch mappings

3. The `seed-config.json` file is **gitignored** and will not be committed to the repository.

### Behavior

- **Built-in Roles**: Only Admin, Moderator, and User roles are supported. These are **always created** automatically
- **Admin user**: An admin user is **always created** if it doesn't exist:
  - If an Admin user is specified in `Users` array: Uses those credentials
  - If no Admin user is specified: Creates a default admin user:
    - Username: `admin`
    - Email: `admin@example.com`
    - Password: `Admin!123`
    - **⚠️ IMPORTANT**: Change this password immediately after first login!
- **Users**: All users specified in the `Users` array will be created with their assigned roles (Admin, Moderator, or User only)
- **Projects and repositories**: Only created if specified in the seed configuration
- **Idempotent**: Running the seed multiple times is safe - existing users and projects will not be duplicated

### Configuration File Structure

The configuration file uses a simple, flat structure:

```json
{
  "Users": [
    {
      "UserName": "admin",
      "Email": "admin@example.com",
      "Password": "Admin!123",
      "Role": "Admin"
    },
    {
      "UserName": "developer",
      "Email": "developer@example.com",
      "Password": "Dev!1234",
      "Role": "User"
    }
  ],
  "Projects": [
    {
      "Name": "myproject",
      "Repositories": [
        {
          "Name": "main-repo",
          "MergeStrategy": "merge_commit",
          "TelegramBotToken": "YOUR_TELEGRAM_BOT_TOKEN",
          "TelegramChatId": "YOUR_TELEGRAM_CHAT_ID",
          "BitbucketToken": "YOUR_BITBUCKET_TOKEN",
          "BranchMappings": [
            { "From": "main", "To": "develop" },
            { "From": "develop", "To": "staging" }
          ],
          "NotificationSettings": {
            "IgnoreAutoMergeNotifications": true
          },
          "UserNames": ["developer"]
        }
      ]
    }
  ]
}
```

#### Users Array

Each user object contains:
- **UserName** (required): The username for the user
- **Email** (required): The email address (must be unique)
- **Password** (required): The password for the user
- **Role** (required): The role name. Must be one of: "Admin", "Moderator", or "User"

**Notes:**
- You can create as many users as needed
- Each user must be assigned one of the three built-in roles: Admin, Moderator, or User

#### Projects Array

Each project object contains:
- **Name** (required): The project name
- **Repositories** (required): Array of repository configurations

#### Repository Configuration

Each repository object contains:
- **Name** (required): The repository name
- **MergeStrategy** (optional, default: "merge_commit"): One of `merge_commit`, `squash`, or `fast_forward`
- **TelegramBotToken** (optional): Telegram bot token for notifications
- **TelegramChatId** (optional): Telegram chat ID for notifications
- **BitbucketToken** (optional): Bitbucket API token for repository access
- **BranchMappings** (optional): Array of branch mapping objects
  - **From**: Source branch name
  - **To**: Target branch name
- **NotificationSettings** (optional): Notification configuration
  - **IgnoreAutoMergeNotifications** (optional, default: false): Whether to ignore auto-merge notifications
- **UserNames** (optional): Array of usernames that should have access to this repository

**Notes:**
- Optional fields can be set to `null` or omitted entirely
- `UserNames` should reference usernames from users defined in the `Users` array
- If `UserNames` is empty or null, only Admin and Moderator roles will have access (they have automatic access to all repositories)

### Configuration Examples

#### Minimal Configuration

```json
{
  "Users": [
    {
      "UserName": "admin",
      "Email": "admin@example.com",
      "Password": "Admin!123",
      "Role": "Admin"
    }
  ],
  "Projects": []
}
```

#### Multiple Users with Different Roles

```json
{
  "Users": [
    {
      "UserName": "admin",
      "Email": "admin@example.com",
      "Password": "Admin!123",
      "Role": "Admin"
    },
    {
      "UserName": "moderator",
      "Email": "moderator@example.com",
      "Password": "Moderator!123",
      "Role": "Moderator"
    },
    {
      "UserName": "developer1",
      "Email": "dev1@example.com",
      "Password": "Dev!1234",
      "Role": "User"
    },
    {
      "UserName": "developer2",
      "Email": "dev2@example.com",
      "Password": "Dev!1234",
      "Role": "User"
    }
  ],
  "Projects": [
    {
      "Name": "myproject",
      "Repositories": [
        {
          "Name": "main-repo",
          "MergeStrategy": "merge_commit",
          "BitbucketToken": "YOUR_TOKEN",
          "UserNames": ["developer1"]
        }
      ]
    }
  ]
}
```

### Configuration Notes

- The seed configuration is loaded at application startup
- Changes to `seed-config.json` require an application restart
- The `seed-config.json` file is automatically ignored by git (see `.gitignore`)
- See `seed-config.example.json` for a complete example with all possible options

## Usage

### Accessing the Web Interface

1. Navigate to `http://localhost:7799` in your web browser
2. Login with your credentials
3. You'll be redirected to the Management page

### User Roles

- **Admin**: Full access to all features including user management and admin tools
- **Moderator**: Can manage projects and repositories, but cannot manage users
- **User**: Can only view projects and repositories they have been granted access to

### Webhooks

Configure **one webhook URL per repository** in Bitbucket: `https://your-host:7799/webhook`

The single `/webhook` endpoint handles all events (cascade merge, Telegram notifications). Events are routed internally by type. Optionally set a Webhook Secret in the repository Auth tab to verify `X-Hub-Signature`.

### Managing Repositories

1. Go to the **Projects** tab
2. Select a project to view its repositories
3. Click on a repository to view and edit its settings:
   - Merge strategy
   - Branch mappings (cascade merge configuration)
   - Telegram notification settings
   - Bitbucket authentication (including optional webhook secret)

### Managing Users

1. Go to the **Users** tab (Admin only)
2. Create, edit, or delete users
3. Assign roles to users
4. Manage repository access for User role accounts

### Viewing Logs

- Access logs at: `http://localhost:7799/logs` (requires authentication)

## Data Storage

- **Database**: SQLite database stored in `wwwroot/data/app.db` (local) or `./data/app.db` (Docker)
- **Logs**: Application logs stored in SQLite database
- **Configuration**: `seed-config.json` file (gitignored)

## Troubleshooting

### Application won't start

- Check if port 7799 is already in use
- Verify Docker is running (for Docker setup)
- Check application logs for errors

### Can't login

- Verify your credentials in `seed-config.json`
- If using default admin, use: `admin` / `Admin!123`
- Check that the database was initialized correctly

### Database issues

- Delete the database file (`wwwroot/data/app.db` or `./data/app.db`) to reset
- Restart the application to reinitialize the database
- Ensure the data directory has write permissions

### Docker volume issues

- Ensure the `./data` directory exists and has proper permissions
- Check Docker volume mount configuration in `docker-compose.yaml`

### Configuration issues

- Verify `seed-config.json` is valid JSON
- Check that usernames in `UserNames` arrays match usernames in the `Users` array
- Ensure required fields are present (UserName, Email, Password, Role for users; Name for projects and repositories)

## Support

For issues or questions, please refer to the project repository.
