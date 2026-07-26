# REVORA_AUTH_CONTEXT.md

## Project Overview

Project: Revora

Type: Second-hand Fashion Marketplace

Backend Stack:

* ASP.NET Core
* Entity Framework Core
* SQL Server
* JWT Authentication
* Refresh Token Rotation

Architecture:

* Controller
* Service
* EF Core DbContext

Current Focus:

* Authentication & Authorization Module

---

# Authentication Strategy

Authentication Model:

* JWT Access Token
* Refresh Token Rotation

Authorization Model:

* RBAC (Role-Based Access Control)
* Permission-Based Authorization

Current Roles:

* Admin
* User

OTP Verification:

* Deferred (Not part of MVP)

Login History:

* Deferred (Not part of MVP)

Refresh Token Hashing:

* Deferred (Refresh token currently stored as plain string in DB)

Replay Attack Detection:

* Deferred

Token Family Tracking:

* Deferred

---

# Database Design

## User

Important fields:

* UserId (long)
* Username
* Email
* PasswordHash
* RoleId
* IsActive
* IsFirstLogin
* CreatedAt

Navigation:

* Role
* RefreshTokens

---

## Role

Fields:

* RoleId
* RoleName

Current Roles:

* Admin
* User

Navigation:

* Users
* RolePermissions

---

## Permission

Fields:

* PermissionId
* Name
* Description

Examples:

* product.create
* product.update
* product.delete

---

## RolePermission

Composite Key:

(RoleId, PermissionId)

Purpose:

Many-to-Many relationship between Roles and Permissions.

---

## RefreshToken

Current Final Model:

* Id (long)

* Token (string)

* UserId (long)

* CreatedAt

* ExpiresAt

* IsRevoked

* RevokedAt

* DeviceName

* IpAddress

Navigation:

* User

Notes:

* ReplacedByTokenId removed from MVP.
* Refresh token hashing removed from MVP.
* LoginHistory table not implemented.

---

# DbContext Rules

Unique Indexes:

Users:

* Email
* Username

Roles:

* RoleName

Permissions:

* Name

RefreshTokens:

* Token

Relationships:

User -> Role

* Restrict Delete

User -> RefreshTokens

* Cascade Delete

Role -> RolePermissions

* Cascade Delete

Permission -> RolePermissions

* Cascade Delete

---

# JWT Infrastructure

JwtSettings:

* Key
* Issuer
* Audience
* AccessTokenExpirationMinutes
* RefreshTokenExpirationDays

Current Values:

Access Token:

* 30 minutes

Refresh Token:

* 7 days

---

# JWT Claims

Generated Claims:

* sub
* email
* unique_name
* role
* permission

Example:

sub = 15
email = [user@gmail.com](mailto:user@gmail.com)
role = User

permission:

* product.create
* product.update

---

# Services

## IJwtService

Responsibilities:

* GenerateAccessToken(...)
* GetPrincipalFromExpiredToken(...)

Responsibilities explicitly excluded:

* Refresh Token generation
* Refresh Token persistence

---

## IRefreshTokenService

Responsibilities:

* GenerateTokenString()
* GetActiveTokenAsync()
* RevokeTokenAsync()

Purpose:

Manage refresh token lifecycle and session persistence.

---

## IAuthService

Methods:

* RegisterAsync(...)
* LoginAsync(...)
* RefreshAsync(...)
* LogoutAsync(...)
* LogoutAllAsync(...)
* ChangePasswordAsync(...)

All methods support CancellationToken.

---

# DTOs

RegisterDto

* Username
* Email
* Password
* FullName

LoginDto

* Email
* Password

TokenDto

* AccessToken
* RefreshToken

ChangePasswordDto

* CurrentPassword
* NewPassword
* ConfirmPassword

SessionInfoDto

* TokenId
* DeviceName
* IpAddress
* CreatedAt
* RevokedAt

---

# Completed Work

Database

Completed:

* Permissions table
* RolePermissions table
* RefreshTokens table
* User update
* Role update

Migration:

* Approved
* Successfully generated

---

JWT Infrastructure

Completed:

* JwtSettings
* JwtService
* JWT Authentication Configuration
* Middleware Registration

Verified:

* Build Successful

---

AuthService

Completed:

RegisterAsync

Features:

* BCrypt password hashing
* Email uniqueness validation
* Username uniqueness validation
* Default User role assignment
* UTC timestamps
* Async EF Core
* Race condition mitigation
* BusinessException usage

Status:

* Approved

---

LoginAsync

Features:

* Load User + Role + Permissions
* BCrypt.Verify
* User active validation
* Permission extraction
* JWT generation
* Refresh token creation
* Device/IP capture
* Session persistence

Status:

* Approved

Notes:

AuthService uses IHttpContextAccessor to capture:

* User-Agent
* RemoteIpAddress

---

# Current Task

IMPLEMENTATION TARGET:

RefreshAsync()

Approved Design:

1. Validate refresh token.
2. Load user + role + permissions.
3. Validate user status.
4. Revoke old refresh token.
5. Generate new refresh token.
6. Insert new refresh token.
7. Generate new access token.
8. Save changes.
9. Commit transaction.
10. Return TokenDto.

Transaction Required:
YES

Reason:
Prevent partial token rotation failures.

Business Exceptions:

InvalidRefreshToken

Thrown when:

* Token not found
* Token revoked

SessionExpired

Thrown when:

* Refresh token expired

UserInactive

Thrown when:

* User disabled

---

# Next Planned Features

After RefreshAsync:

1. LogoutAsync
2. LogoutAllAsync
3. ChangePasswordAsync

Then:

1. AuthController
2. AccountController
3. Permission Policies
4. Resource-Based Authorization
5. End-to-End Testing

---

# Important Decisions

Already Finalized:

* Roles = Admin + User
* No OTP in MVP
* No LoginHistory in MVP
* No Refresh Token Hashing in MVP
* No Replay Detection in MVP
* No Token Family Tracking in MVP

Do not redesign these decisions unless a critical issue is found.
