# Authentication & Authorization Module Documentation

This document provides a comprehensive technical overview of the security architecture and flows implemented in the AuthProject.

## 1. Architecture Overview

The module follows a layered architecture (Clean/N-Tier) integrated with the ASP.NET Core framework.

- **Presentation Layer**: `AuthController` and `UserController` provide RESTful endpoints for authentication, session management, and user data.
- **Service Layer**: `AuthService` contains the core business logic for user management, token generation, and security policies.
- **Data Layer**: `AppDbContext` handles persistence using Entity Framework Core with SQL Server.
- **Cross-cutting Concerns**: 
    - `ExceptionMiddleware` for global error handling.
    - Built-in `Authentication` and `Authorization` middleware.
    - Custom `AuthorizationHandlers` for resource-based access control.

## 2. Security Flow

The system employs a dual-token strategy for secure and persistent authentication.

### Token Strategy
- **Access Token (JWT)**: Short-lived (30 minutes), contains user identity and permissions. Passed via the `Authorization: Bearer` header.
- **Refresh Token**: Long-lived (7 days), stored in a **HttpOnly, Secure, SameSite=Strict cookie**. This protects the token from XSS (Cross-Site Scripting) and CSRF (Cross-Site Request Forgery) attacks.

## 3. JWT Flow

### Authentication Process
1. **Login**: User provides credentials.
2. **Verification**: `AuthService` verifies the identity against the database using BCrypt.
3. **Issuance**:
    - An **Access Token** is generated with claims (`id`, `email`, `role`, `permission`).
    - A random **Refresh Token** is generated and stored in the database.
4. **Storage**: Access Token is returned in the JSON response; Refresh Token is set in a secure cookie.

### Token Rotation & Replay Attack Protection
The system implements **Refresh Token Rotation**:
- Every time a Refresh Token is used to get a new Access Token, the old Refresh Token is revoked and a new one is issued.
- **Replay Detection**: If a revoked refresh token is reused (indicating a potential stolen token), the system detects this "replay attack" and **revokes all active sessions** for that user immediately to minimize damage.

## 4. User Registration Flow

1. **Validation**: API checks for email uniqueness and validates input (e.g., email format).
2. **Password Hashing**: Passwords are hashed using the **BCrypt** algorithm (Work factor handled by the library), which includes a built-in salt to prevent rainbow table attacks.
3. **Role Assignment**: New users are automatically assigned the default "User" role.
4. **Persistence**: User record is saved to the database.

## 5. Role & Permission Flow

The system uses a **Role-Based Access Control (RBAC)** model with an underlying **Permission** system.

- **Models**: `User` -> `Role` -> `RolePermission` -> `Permission`.
- **JWT Mapping**: When a user logs in, the `AuthService` fetches all permissions associated with their role and adds them as `permission` claims in the JWT.
- **Policy Enforcement**:
    - **Simple RBAC**: `[Authorize(Roles = "Admin")]`
    - **Permission-Based**: `options.AddPolicy("CanReadUsers", policy => policy.RequireClaim("permission", "user.read"));`

## 6. Resource-Based Authorization

Used when access depends on the specific data being accessed (e.g., "A user can only edit their own profile").

- **Requirement**: `CanEditUserRequirement`.
- **Handler**: `CanEditUserHandler`.
- **Logic**:
    - Checks if the user is an **Admin** (Admins can edit anyone).
    - Otherwise, compares the `NameIdentifier` claim of the current user with the `Id` of the `User` resource being accessed.

## 7. Middleware & Global Handling

### Global Exception Middleware
A custom `ExceptionMiddleware` handles all errors:
- **`AppValidationException`**: Returns 400 Bad Request with a detailed map of field errors (RFC 7807 compliant).
- **`BusinessException`**: Returns specific business error codes (e.g., `InvalidCredentials`) to help the frontend handle logic errors gracefully.
- **System Exceptions**: Returns a clean 500 Internal Server Error without leaking internal system details.

### Model Validation Hook
The project overrides `InvalidModelStateResponseFactory` in `Program.cs` to automatically capture model validation errors and throw an `AppValidationException`, ensuring consistent error formatting across all endpoints.

## 8. Key Features & Extra Security Measures

### Session Management
- **Audit Logging**: Each Refresh Token record tracks the `IpAddress` and `DeviceName` (from the User-Agent header).
- **Manual Revocation**: Users can view their active sessions and revoke specific ones (Log out from other devices) or revoke all sessions at once.

### Password Management
- **Security Revocation**: When a user changes their password, all active sessions (Refresh Tokens) are revoked to ensure the old access is invalidated across all devices.
- **Verification**: Current password must be verified before allowing a change.

### Database Seeding
- A `DbSeeder` is implemented to initialize the database with default roles (`Admin`, `User`) and permissions, ensuring the system is ready to use immediately after deployment.

# Revora Authentication Standard

## Authentication Architecture

- JWT Access Token
- Refresh Token
- Refresh Token Rotation
- Replay Attack Detection
- BCrypt Password Hashing
- Session Management

## Access Token

Lifetime: 30 minutes

Claims:

- sub
- userId
- username
- email
- role
- permissions

## Refresh Token

Lifetime: 7 days

Storage:

- HttpOnly Cookie
- Secure
- SameSite=Strict

## Refresh Token Rotation

When a refresh token is used:

1. Revoke old refresh token
2. Generate new refresh token
3. Generate new access token

## Replay Attack Detection

If a revoked refresh token is reused:

1. Revoke all active refresh tokens
2. Force re-login

## Password Storage

Algorithm:

- BCrypt

Plain text passwords must never be stored.

## Session Management

Each refresh token stores:

public int Id { get; set; }
        public string Token { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool IsRevoked { get; set; } = false;

        public DateTime? RevokedAt { get; set; }
        public string DeviceName { get; set; } = null!;
        public string IpAddress { get; set; } = null!;

Users can:

- View active sessions
- Revoke individual sessions
- Revoke all sessions

## Password Change

When password changes:

- Verify current password
- Revoke all active refresh tokens

## Authorization Model

RBAC + Permission Based Authorization

User
→ Role
→ RolePermission
→ Permission

## Resource-Based Authorization

Examples:

Product:

- Owner can edit own product
- Admin can edit any product

Comment:

- Owner can delete own comment
- Admin can delete any comment