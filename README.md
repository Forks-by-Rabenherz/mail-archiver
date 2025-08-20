# 📧 Mail-Archiver - IMAP Email Archiving System

**A comprehensive solution for archiving, searching, and exporting emails from IMAP accounts**

<div style="display: flex; flex-wrap: wrap; gap: 10px; margin-bottom: 20px;">
  <a href="#"><img src="https://img.shields.io/badge/Docker-2CA5E0?style=for-the-badge&logo=docker&logoColor=white" alt="Docker"></a>
  <a href="#"><img src="https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET"></a>
  <a href="#"><img src="https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white" alt="PostgreSQL"></a>
  <a href="#"><img src="https://img.shields.io/badge/Bootstrap-563D7C?style=for-the-badge&logo=bootstrap&logoColor=white" alt="Bootstrap"></a>
  <a href="https://github.com/s1t5/mail-archiver"><img src="https://img.shields.io/github/stars/s1t5/mail-archiver?style=for-the-badge&logo=github" alt="GitHub Stars"></a>
</div>

## ✨ Key Features

### 📌 General
- Automated archiving of incoming and outgoing emails
- Support for multiple IMAP accounts
- Storage of email content and attachments
- Scheduled synchronization at configurable intervals
- Mobile and desktop optimized responsive UI
- MBox Import

### 🔍 Advanced Search
- Search across all archived emails
- Filter by date range, sender, recipient, and more
- Preview emails with attachment list

### 👥 Multi User
- Create multiple user accounts
- Assignment of different mail accounts to users

### 📊 Dashboard & Statistics
- Account-specific statistics and overview
- Storage usage monitoring
- Top senders analysis

### 📤 Export Functions
- Export individual emails in EML format
- Bulk export search results to CSV or JSON
- Attachment download with original filenames

### 📤 Restore Function
- Restore a selection of emails or an entire mailbox to a destination mailbox

### 🗑️ Retention Policies
- Configure automatic deletion of archived emails from the IMAP server after a specified number of days
- Set retention period per email account (e.g., delete emails after 30, 90, or 365 days)
- Emails are only deleted from the server after they have been successfully archived
- Helps manage storage space on the IMAP server while maintaining a complete archive


> 🚨 **Important note for retention policies**
> - Requires IMAP Expunge support from the mail server to permanently delete emails
> - For Gmail accounts, Auto-Expunge must be enabled in Gmail settings under the "Forwarding and POP/IMAP" tab

## 🖼️ Screenshots

### Dashboard
![Mail-Archiver Dashboard](https://github.com/s1t5/mail-archiver/blob/main/Screenshots/dashboard.jpg?raw=true)

### Archive
![Mail-Archiver Archive](https://github.com/s1t5/mail-archiver/blob/main/Screenshots/archive.jpg?raw=true)

### Email Details
![Mail-Archiver Mail](https://github.com/s1t5/mail-archiver/blob/main/Screenshots/details.jpg?raw=true)

## 🚀 Quick Start

### Prerequisites
- [Docker](https://www.docker.com/products/docker-desktop)
- [Docker Compose](https://docs.docker.com/compose/install/)
- IMAP email account credentials

### 🛠️ Installation

1. Install the prerequisites on your system

2. Create a `docker-compose.yml` file 
```yaml
services:
  mailarchive-app:
    image: s1t5/mailarchiver:latest
    restart: always
    environment:
      # Database Connection
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=MailArchiver;Username=mailuser;Password=masterkey;

      # Authentication Settings
      - Authentication__Enabled=true
      - Authentication__Username=admin
      - Authentication__Password=secure123!
      - Authentication__SessionTimeoutMinutes=60
      - Authentication__CookieName=MailArchiverAuth

      # MailSync Settings
      - MailSync__IntervalMinutes=15
      - MailSync__TimeoutMinutes=60
      - MailSync__ConnectionTimeoutSeconds=180
      - MailSync__CommandTimeoutSeconds=300

      # BatchRestore Settings
      - BatchRestore__AsyncThreshold=50
      - BatchRestore__MaxSyncEmails=150
      - BatchRestore__MaxAsyncEmails=50000
      - BatchRestore__SessionTimeoutMinutes=30
      - BatchRestore__DefaultBatchSize=50

      # BatchOperation Settings
      - BatchOperation__BatchSize=50
      - BatchOperation__PauseBetweenEmailsMs=50
      - BatchOperation__PauseBetweenBatchesMs=250

      # Npgsql Settings
      - Npgsql__CommandTimeout=600
    ports:
      - "5000:5000"
    networks:
      - postgres
    depends_on:
      postgres:
        condition: service_healthy


  postgres:
    image: postgres:17-alpine
    restart: always
    environment:
      POSTGRES_DB: MailArchiver
      POSTGRES_USER: mailuser
      POSTGRES_PASSWORD: masterkey
    volumes:
      - ./postgres-data:/var/lib/postgresql/data
    networks:
      - postgres
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U mailuser -d MailArchiver"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 10s

networks:
  postgres:
```

3. Edit the database configuration in the `docker-compose.yml` and set a secure password in the `POSTGRES_PASSWORD` variable and the `ConnectionString`.

4. If you want to use authentication (which i'd strongly recommend) definie a `Authentication__Username` and `Authentication__Password` which is used for the admin user.

4. Configure a reverse proxy of your choice with https and authentification to secure access to the application. 

> ⚠️ **Attention**
> 
> The application itself does not provide encrypted access via https! It must be set up via a reverse proxy! Moreover the application is not build for public internet access!

4. Initial start of the containers:
```bash
docker compose up -d
```

5. Restart containers:
```bash
docker compose restart
```

6. Access the application

7. Login with your defined credentials and add your first email account:
- Navigate to "Email Accounts" section
- Click "New Account"
- Enter your IMAP server details and credentials
- Save and start archiving!
- If you want, create other users and assign accounts.

## 🔐 Security Note
- Use strong passwords and change default credentials
- Consider implementing HTTPS with a reverse proxy in production
- Regular backups of the PostgreSQL database recommended

## 📝 How To - Mailbox migration
It is also possible to migrate a mailbox to another target mailbox, for example when changing mail provider.
The following steps are planned for this
1. add the source account under the accounts
2. synchronisation of the source account
3. adding the target account
4. synchronisation of the possibly still empty target account
5. select ‘Copy All Emails to Another Mailbox’ in the details under the accounts for the source account
6. select the target account and the target folder in this account and start the migration. If there is a large amount of emails to be moved, this is carried out as a background task. The status and progress of this can be viewed in the Jobs tab.
7. after the successful transfer, set the source account to ‘Disabled’ under the accounts so that it is no longer archived in future.

## 📋 Technical Details

### Architecture
- ASP.NET Core 8 MVC application
- PostgreSQL database for email storage
- MailKit library for IMAP communication
- Background service for email synchronization
- Bootstrap 5 and Chart.js for frontend

## 🤝 Contributing
Contributions welcome! Please open an Issue or Pull Request.

## 🚀 Roadmap / Ideas
The roadmap is now maintained on the repos project page.

---

📄 *License: GNU GENERAL PUBLIC LICENSE Version 3 (see LICENSE file)*
