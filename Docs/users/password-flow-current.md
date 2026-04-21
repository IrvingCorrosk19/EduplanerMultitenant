# Password Management Flow — Users Module (Current State)

**Document type:** Technical documentation (audit)  
**Scope:** Password creation, storage, reset, update, and delivery in the Users module  
**Purpose:** Support design of a future **Mass Password Delivery** feature. No code changes.

---

## 1. Executive Summary

The platform manages user passwords through a single field **`password_hash`** in the `users` table. Passwords are hashed with **BCrypt** (no separate salt column; BCrypt embeds salt). There is **no password reset token** flow: the only “reset” is an admin action **“Send password email”** that generates a **new temporary password**, stores its hash, and sends the **plaintext** password to the user by email. Password creation happens in several places: manual user create (UserController), school admin create (SuperAdminService), bulk student/teacher creation (StudentAssignmentController, AcademicAssignmentController), and initial superadmin (AuthController / script). **Change password** is available only for the **currently logged-in** user via ChangePasswordController → UserService. There is no “forgot password” or time-limited reset link. Credential delivery is **synchronous** SMTP from the web app (no background jobs). Security settings (e.g. `password_min_length`, complexity) exist in `security_settings` but are **not** enforced in the current User password flows; validation is hardcoded in controllers.

---

## 2. Architecture Overview

| Layer | Components |
|-------|------------|
| **Controllers** | `UserController`, `AuthController`, `ChangePasswordController`, `SuperAdminController` |
| **Services** | `UserService`, `AuthService`, `SuperAdminService`, `EmailConfigurationService` |
| **Storage** | PostgreSQL `users.password_hash`; email config in `email_configurations` |
| **Delivery** | SMTP only; email built and sent inside `UserController.SendWelcomeEmailAsync` |

- **Authentication:** Login uses `AuthService.LoginAsync(email, password)` which loads user by email, verifies password (BCrypt or legacy plaintext), then creates cookie session.
- **Password creation/update:** Performed in controllers or SuperAdminService; hashing is always `BCrypt.Net.BCrypt.HashPassword(...)` before persisting.
- **Credential delivery:** Single flow: admin triggers “Send password email” → `UserController.SendPasswordEmail` → new temp password → hash saved → email sent with plaintext password (no token, no link).

---

## 3. Password Creation Flow

### 3.1 Manual user creation (Admin)

- **Entry:** `UserController.CreateJson([FromBody] CreateUserViewModel model)`
- **File:** `Controllers/UserController.cs`  
- **Method:** `CreateJson()`

Flow:

1. Validate email format and uniqueness.
2. If `model.PasswordHash` is not empty, validate with `IsStrongPassword(model.PasswordHash)` (see §8).
3. Build `User` with:
   - `PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash ?? "123456")`
4. Call `_userService.CreateAsync(user, ...)` (service persists the entity as-is; it does not hash again).

So: password is **optional** on create; if omitted, default **`123456`** is hashed and stored. No email is sent on create.

### 3.2 User update (password change by admin)

- **Entry:** `UserController.UpdateJson([FromBody] CreateUserViewModel model)`
- **File:** `Controllers/UserController.cs`  
- **Method:** `UpdateJson()`

If `model.PasswordHash` is not empty:

- `existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);`
- No strength check in update (only in create when field is present).
- No notification to user.

### 3.3 School admin creation (SuperAdmin)

- **Entry:** SuperAdmin creates a school with an admin user.
- **File:** `Services/Implementations/SuperAdminService.cs`  
- **Method:** `CreateSchoolWithAdminAsync(SchoolAdminViewModel model, ...)`

Admin user is created with:

- `PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.AdminPassword)`

Password comes from the form; no email is sent from this flow.

### 3.4 Bulk student creation (assignment upload)

- **Entry:** `StudentAssignmentController` when processing upload and creating new students.
- **File:** `Controllers/StudentAssignmentController.cs` (around line 369)

New user is created with:

- `PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")`  
- Fixed default **`123456`** for all bulk-created students. No email.

### 3.5 Bulk teacher creation (academic assignment)

- **Entry:** `AcademicAssignmentController` when creating a new teacher.
- **File:** `Controllers/AcademicAssignmentController.cs` (around line 150)

New user is created with:

- `PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")`  
- Same fixed default **`123456`**; no email.

### 3.6 Superadmin initial creation

- **API:** `GET /api/auth/create-superadmin`  
- **File:** `Controllers/AuthController.cs`  
- **Method:** `CreateSuperAdmin()`

Creates a single superadmin if none exists:

- `PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!")`  
- Credentials returned in API response (and documented in script).

- **Script:** `Scripts/CreateInitialSuperAdminScript.cs`  
- **Method:** `RunAsync(SchoolDbContext context)`  
- Same password **`Admin123!`**; creates or updates superadmin.

---

## 4. Password Storage Mechanism

- **Algorithm:** BCrypt (via `BCrypt.Net`).
- **Column:** `users.password_hash` (max length 100 in schema; BCrypt hashes are ~60 characters).
- **Salt:** BCrypt includes salt in the hash string; no separate `password_salt` column.
- **Legacy:** The system still supports **plaintext** passwords stored in `password_hash` for backward compatibility:
  - **AuthService.LoginAsync:** If the value does not look like a BCrypt hash (`$2a$`, `$2b$`, etc.), it compares `password == user.PasswordHash` and, on success, re-hashes and updates the user.
  - **UserService.ChangePasswordAsync:** Same: if not “hashed”, compares current password in plaintext.
- **Detection of hashed value:**  
  - **AuthService:** `IsPasswordHashed(string)` checks for `$2a$`, `$2b$`, `$2x$`, `$2y$`, `$2$`.  
  - **UserService:** `IsPasswordHashed(string)` checks `StartsWith("$2")` and length > 20.

There are **no** fields for:

- `temporary_password`
- `reset_token`
- `token_expiration`
- `must_change_password`
- `last_password_change`

---

## 5. Password Reset Process

There is **no** classic “forgot password” or “reset by link” flow. The only “reset” is:

### 5.1 “Send password email” (admin-triggered)

- **Trigger:** Admin clicks “Send password” (or equivalent) for a user in the User list.
- **Endpoint:** `POST /User/SendPasswordEmail/{id}`  
- **File:** `Controllers/UserController.cs`  
- **Method:** `SendPasswordEmail(Guid id)`

Steps:

1. Load user by `id`; require current user’s school (email config is per school).
2. Load email configuration: `_emailConfigurationService.GetBySchoolIdAsync(currentUser.SchoolId)`.
3. Generate a new temporary password: `GenerateTemporaryPassword()` (12 chars, upper, lower, digit, special).
4. Update user: `user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword)` then `_userService.UpdateAsync(user)`.
5. Send email with **plaintext** password: `SendWelcomeEmailAsync(user, newPassword, emailConfig)`.

No token, no expiration, no “must change on first login” flag. The old password is immediately replaced.

---

## 6. Notification / Credential Delivery

### 6.1 Channel

- **Email only** (SMTP). No SMS, no in-app notification queue, no background job: the HTTP request builds and sends the email synchronously.

### 6.2 Where it is triggered

- **UserController.SendPasswordEmail** → **SendWelcomeEmailAsync** (private method in the same controller).

### 6.3 Configuration

- **Table:** `email_configurations` (per school).
- **Service:** `IEmailConfigurationService.GetBySchoolIdAsync(schoolId)` returns SMTP settings (server, port, username, password, SSL/TLS, From name).
- **File:** `Controllers/UserController.cs`  
- **Method:** `SendWelcomeEmailAsync(User user, string password, EmailConfigurationDto emailConfig)`

### 6.4 Template and data

- **Subject:** `"Credenciales de Acceso - Eduplaner"`.
- **Body:** HTML built inline in C# (no separate template file). Contains:
  - Greeting with user name/last name.
  - **Email:** `user.Email`.
  - **Contraseña temporal:** the **plaintext** `password` passed in.
  - Link: `https://eduplaner.net/`.
  - Role and status.
  - Note recommending changing the password on first login (no enforcement).

### 6.5 Sending

- `System.Net.Mail.SmtpClient` + `MailMessage`; credentials from `emailConfig` (SmtpUsername, SmtpPassword). Sent with `client.SendMailAsync(message)` inside the same request. No retry or queue.

---

## 7. Database Tables Involved

### 7.1 `users`

| Field | Type | Description |
|-------|------|-------------|
| `id` | uuid | PK |
| `email` | varchar(100) | Unique, used for login |
| `password_hash` | varchar(100) | BCrypt hash (or legacy plaintext) |
| `role`, `status`, `school_id`, … | … | Other user attributes |

No other password-related columns (no reset token, no expiration, no “must change”).

### 7.2 `email_configurations`

Used for sending credential emails (SMTP). Key fields: `school_id`, `smtp_server`, `smtp_port`, `smtp_username`, `smtp_password`, `from_name`, etc.

### 7.3 `security_settings`

Per-school settings including `password_min_length`, `require_uppercase`, `require_lowercase`, `require_numbers`, `require_special`, `expiry_days`, `prevent_reuse`, etc. **Not used** in the current User password creation, update, or “send password email” flows; validation in those flows is hardcoded (e.g. min 8 chars, complexity in `UserController.IsStrongPassword`).

---

## 8. Security Mechanisms

### 8.1 Hashing

- **Algorithm:** BCrypt (`BCrypt.Net.BCrypt.HashPassword`).
- **Salt:** Embedded in BCrypt hash.

### 8.2 Complexity (current enforcement)

- **UserController.CreateJson:** When a password is provided, `IsStrongPassword` requires:
  - Length ≥ 8.
  - At least one digit, one uppercase, one lowercase, one non-alphanumeric character.
- **ChangePasswordViewModel:** `[StringLength(100, MinimumLength = 8)]` for new password.
- **UserService.ChangePasswordAsync:** Rejects if `newPassword` is null/whitespace or length < 8; no use of `security_settings`.

### 8.3 Login and legacy plaintext

- **AuthService.LoginAsync:** Verifies with BCrypt if stored value looks like a hash; otherwise compares plaintext and then re-hashes and saves. This allows migration from plaintext to BCrypt without breaking existing users.

### 8.4 FixPasswords (one-off migration)

- **Endpoint:** `GET /Auth/FixPasswords` (AllowAnonymous).  
- **File:** `Controllers/AuthController.cs`  
- **Method:** `FixPasswords()`  
- Iterates all users; if `!IsPasswordHashed(user.PasswordHash)`, hashes with BCrypt and updates. One-time migration helper; not part of normal flows.

---

## 9. Sequence Diagram of the Flow

### 9.1 “Send password email” (current “reset”)

```
Admin (browser)     User/Index.cshtml    UserController     UserService    EmailConfigurationService    SMTP
      |                    |                    |                  |                    |                    |
      |  Click "Send pwd"   |                    |                  |                    |                    |
      |-------------------->|                    |                  |                    |                    |
      |                    | POST SendPasswordEmail(id)             |                    |                    |
      |                    |------------------->|                  |                    |                    |
      |                    |                    | GetByIdAsync(id)  |                    |                    |
      |                    |                    |----------------->|                    |                    |
      |                    |                    |<-----------------|                    |                    |
      |                    |                    | GetBySchoolIdAsync(schoolId)          |                    |
      |                    |                    |--------------------------------------->|                    |
      |                    |                    |<---------------------------------------|                    |
      |                    |                    | GenerateTemporaryPassword()            |                    |
      |                    |                    | BCrypt.HashPassword(newPassword)       |                    |
      |                    |                    | UpdateAsync(user)                      |                    |
      |                    |                    |----------------->|                    |                    |
      |                    |                    | SendWelcomeEmailAsync(user, plainPwd, config)               |
      |                    |                    | SmtpClient.SendMailAsync(...)          |                    |
      |                    |                    |--------------------------------------------------------------------------------->|
      |                    |                    |<---------------------------------------------------------------------------------|
      |                    |<-------------------| OK { message }     |                    |                    |
      |<--------------------|                    |                  |                    |                    |
```

### 9.2 User change password (logged-in user)

```
User (browser)   ChangePassword/Index   ChangePasswordController   UserService (ChangePasswordAsync)   DB
      |                    |                         |                              |                    |
      |  Submit form       |                         |                              |                    |
      |------------------->|                         |                              |                    |
      |                    | POST ChangePassword    |                              |                    |
      |                    | (CurrentPassword,      |                              |                    |
      |                    |  NewPassword)           |                              |                    |
      |                    |----------------------->|                              |                    |
      |                    |                         | Get current user (from auth) |                    |
      |                    |                         | ChangePasswordAsync(userId, current, new)          |
      |                    |                         |----------------------------->|                    |
      |                    |                         |                              | Find user, Verify  |
      |                    |                         |                              | BCrypt.Verify      |
      |                    |                         |                              | HashPassword(new) |
      |                    |                         |                              | SaveChanges        |
      |                    |                         |                              |------------------>|
      |                    |                         |<-----------------------------|                    |
      |                    |<-------------------------| { success, message }       |                    |
      |<--------------------|                         |                              |                    |
```

---

## 10. Potential Weaknesses

- **Plaintext in email:** The temporary password is sent in clear text over SMTP. If email is compromised, the password is exposed.
- **No reset token:** There is no time-limited, single-use link; “reset” is “admin sets new password and emails it.” No invalidation of previous sessions or “use once” token.
- **Default passwords:** Bulk-created students and teachers get fixed default `123456`; manual create uses `123456` if password is omitted. These are weak and often not changed unless the admin sends the “password email” or the user changes password.
- **GetUserJson exposes hash:** `UserController.GetUserJson(id)` returns `user.PasswordHash` in the JSON. Even though it is a hash, exposing it to the client is unnecessary and increases risk if the API is misused.
- **AuthenticateAsync legacy:** `UserService.AuthenticateAsync(email, password)` compares `u.PasswordHash == password` (plaintext). This method appears legacy; login uses `AuthService.LoginAsync` instead. If `AuthenticateAsync` is ever used, it would only work for plaintext-stored passwords.
- **Security settings unused:** `security_settings` (e.g. `password_min_length`, complexity, expiry) are not applied in User create/update or change-password flows; complexity is hardcoded and there is no expiry or reuse check.
- **Synchronous email:** Sending email inside the request can cause timeouts or failures that the admin sees as a generic error; no retry or queue.
- **FixPasswords AllowAnonymous:** The endpoint that hashes all plaintext passwords is `AllowAnonymous`, so anyone who discovers it could trigger a full scan/update of all users (no direct password leak, but availability/audit concern).
- **No “must change password”:** After “send password email” or first login with default password, the system does not force a change; it only recommends it in the email body.

---

## Code Reference Summary

| Area | File | Method / note |
|------|------|----------------|
| User create (admin) | `Controllers/UserController.cs` | `CreateJson()` — hashes `model.PasswordHash ?? "123456"` |
| User update (admin) | `Controllers/UserController.cs` | `UpdateJson()` — hashes `model.PasswordHash` if provided |
| Send password email | `Controllers/UserController.cs` | `SendPasswordEmail(Guid id)`, `GenerateTemporaryPassword()`, `SendWelcomeEmailAsync()` |
| Password strength | `Controllers/UserController.cs` | `IsStrongPassword(string)` |
| Login | `Services/Implementations/AuthService.cs` | `LoginAsync(string email, string password)`, `IsPasswordHashed()` |
| Change password | `Services/Implementations/UserService.cs` | `ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)` |
| Change password endpoint | `Controllers/ChangePasswordController.cs` | `ChangePassword([FromBody] ChangePasswordViewModel model)` |
| Superadmin create | `Controllers/AuthController.cs` | `CreateSuperAdmin()` — hash `"Admin123!"` |
| Superadmin script | `Scripts/CreateInitialSuperAdminScript.cs` | `RunAsync(SchoolDbContext context)` |
| School admin create | `Services/Implementations/SuperAdminService.cs` | `CreateSchoolWithAdminAsync()` — hash `model.AdminPassword` |
| Bulk student | `Controllers/StudentAssignmentController.cs` | New user with `HashPassword("123456")` |
| Bulk teacher | `Controllers/AcademicAssignmentController.cs` | New user with `HashPassword("123456")` |
| User entity | `Models/User.cs` | `PasswordHash` property |
| Users table | `Models/SchoolDbContext.cs` | Entity `User`, column `password_hash` |
| Email config | `Dtos/EmailConfigurationDto.cs` | SMTP fields for `SendWelcomeEmailAsync` |

---

*End of document. This describes the current behavior only; no changes were made to the codebase.*
