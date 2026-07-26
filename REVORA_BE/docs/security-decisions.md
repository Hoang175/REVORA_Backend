# Revora Security Decisions

## Authentication

Use:

- JWT Access Token
- Refresh Token

Do not use server-side sessions.

## Password Security

Use BCrypt.

Never store plain text passwords.

## Authorization

Use:

- Role-Based Access Control (RBAC)
- Permission-Based Authorization

## Token Security

Refresh Token Rotation: Enabled

Replay Attack Detection: Enabled

## Account Security

Password change:

- Revoke all sessions

Logout:

- Revoke current refresh token

Logout All:

- Revoke all refresh tokens

## Audit Information

Track:

- IP Address
- Device Name
- Login Time

for every session.

## Error Handling

Use global exception middleware.

Do not expose stack traces to clients.

## API Security

All protected endpoints require JWT authentication.

Anonymous access only for:

- Login
- Register
- Refresh Token
- Public Product Browsing