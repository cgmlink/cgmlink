# CgmLink.Identity
The CgmLink.Identity project is a .NET library that provides authentication/authorization services for the CGM Link application. It includes features such as user registration, login.

## Usage
To register the identity services:
```csharp
services.AddIdentity(builder.Configuration.GetSection("Identity").Bind);
```

To register the identity middleware:
```csharp
app.UseIdentity();
```

To map the identity endpoints:
```csharp
app.MapIdentityEndpoints();
```

## Features
- User registration
- User login

### User Registration
`api/v1/identity/register`

There are two ways to register a user:

#### As a Patient
When calling the register endpoint, a patient is distinguished by having no `PatientId` set in the `RegisterRequest`.

#### As a Care Giver
If the `PatientId` is set, the user will be registered as a `CareGiver` with basic access to the patient until authorized to access specific patient data.

### User Login
`api/v1/identity/login`

The user can log in using the `LoginRequest` endpoint. The user must provide their email and password. If the credentials are valid, a JWT token will be returned.

### Refresh Token Lifecycle

- **Login:** Each successful login creates a new refresh token in an HTTP-only cookie. Other devices stay signed in. Refresh tokens are not returned in JSON.
- **Refresh:** `POST /api/v1/identity/refresh-token` uses the cookie to issue a new access token and refresh cookie. The previous refresh token becomes invalid.
- **Expiry:** Refresh tokens expire after `RefreshTokenExpirationInDays` (default: 30 days). Each refresh starts a new expiry period. An expired or missing token requires login again.
- **Logout:** Authenticated `POST /api/v1/identity/revoke-token` with `{}` revokes the cookie's refresh token. Other logins remain active; existing access tokens remain valid until expiry.
- **Replay:** Reusing a revoked token rejects the request and revokes its active replacements from the same login.

Clients must send cookies and retain each `Set-Cookie` response. Use refresh to resume an existing session. Coordinate refresh requests across tabs sharing a cookie so only one runs at a time.
