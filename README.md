# ECommerce API — ASP.NET Core 9 Clean Architecture

## Stack
- ASP.NET Core 9 Web API
- Clean Architecture (4 projects)
- CQRS + MediatR
- EF Core 9 (Code First — writes)
- JWT Authentication (Access + Refresh token)
- FluentValidation
- PBKDF2 Password Hashing (SHA-512, 350k iterations)
- SQL Server

---

## Folder Structure

```
ECommerceAPI/
│
├── ECommerceAPI.Domain/               ← Zero dependencies. Pure business.
│   ├── Common/
│   │   └── BaseEntity.cs              ← Id, CreatedAt, UpdatedAt, IsDeleted
│   ├── Entities/
│   │   └── AppUser.cs                 ← User entity with factory method + rules
│   ├── Enums/
│   │   └── UserRole.cs                ← User, Admin
│   └── Interfaces/
│       └── IUserRepository.cs         ← Contract only, no implementation
│
├── ECommerceAPI.Application/          ← Business logic. Depends only on Domain.
│   ├── Behaviours/
│   │   └── ValidationBehaviour.cs     ← Runs validators before every handler
│   ├── Common/
│   │   └── Models/
│   │       └── Result.cs              ← Unified success/failure response wrapper
│   ├── DTOs/
│   │   └── Auth/
│   │       └── AuthDtos.cs            ← Request/Response records
│   ├── Features/
│   │   └── Auth/
│   │       └── Commands/
│   │           ├── Register/          ← Command + Handler + Validator
│   │           ├── Login/             ← Command + Handler + Validator
│   │           └── RefreshToken/      ← Command + Handler
│   ├── Interfaces/
│   │   ├── IJwtService.cs
│   │   └── IPasswordService.cs
│   └── DependencyInjection.cs
│
├── ECommerceAPI.Infrastructure/       ← Talks to DB, external services.
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/
│   │   │   └── AppUserConfiguration.cs  ← EF Core table mapping
│   │   └── Repositories/
│   │       └── UserRepository.cs        ← Implements IUserRepository
│   ├── Services/
│   │   ├── JwtService.cs               ← Generates/validates JWT tokens
│   │   └── PasswordService.cs          ← PBKDF2 hash + verify
│   └── DependencyInjection.cs
│
└── ECommerceAPI.API/                  ← HTTP entry point only.
    ├── Controllers/
    │   └── AuthController.cs           ← Thin: receives → MediatR → return
    ├── Extensions/
    │   ├── JwtExtensions.cs            ← JWT setup + policies
    │   └── SwaggerExtensions.cs        ← Swagger with JWT support
    ├── Middleware/
    │   └── ExceptionMiddleware.cs      ← Global error handler
    ├── appsettings.json
    └── Program.cs
```

---

## Setup Steps

### 1. Clone and open
```bash
git clone <your-repo>
cd ECommerceAPI
```

### 2. Update connection string
Edit `ECommerceAPI.API/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=ECommerceDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 3. Update JWT secret key
```json
"JwtSettings": {
  "SecretKey": "YOUR_SUPER_LONG_SECRET_KEY_MINIMUM_64_CHARACTERS_HERE_!!!"
}
```
> Keep this in environment variables in production. Never commit real secrets.

### 4. Add EF Core migration
```bash
# From solution root
dotnet ef migrations add InitialCreate \
  --project ECommerceAPI.Infrastructure \
  --startup-project ECommerceAPI.API

dotnet ef database update \
  --project ECommerceAPI.Infrastructure \
  --startup-project ECommerceAPI.API
```

### 5. Run the API
```bash
cd ECommerceAPI.API
dotnet run
```

Open Swagger: `https://localhost:5001/swagger`

---

## API Endpoints

### Auth
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/v1/auth/register` | None | Register new user |
| POST | `/api/v1/auth/login` | None | Login, get tokens |
| POST | `/api/v1/auth/refresh-token` | None | Get new access token |
| GET | `/api/v1/auth/me` | JWT | Get current user info |
| GET | `/api/v1/auth/admin-test` | Admin only | Test admin policy |

---

## Register Request
```json
{
  "fullName": "Pranta Hossen",
  "email": "pranta@example.com",
  "phoneNumber": "01712345678",
  "password": "Admin@1234",
  "confirmPassword": "Admin@1234"
}
```

## Login Request
```json
{
  "email": "pranta@example.com",
  "password": "Admin@1234"
}
```

## Auth Response
```json
{
  "userId": "guid-here",
  "fullName": "Pranta Hossen",
  "email": "pranta@example.com",
  "role": "User",
  "accessToken": "eyJhbGci...",
  "refreshToken": "base64-random-string",
  "accessTokenExpiresAt": "2026-05-10T12:15:00Z"
}
```

## Refresh Token Request
```json
{
  "accessToken": "expired-or-valid-jwt",
  "refreshToken": "your-refresh-token"
}
```

---

## Security Details

| Concern | Implementation |
|---------|----------------|
| Password hashing | PBKDF2 + SHA-512, 350,000 iterations |
| Salt | 32-byte cryptographically random per user |
| Timing attack | `CryptographicOperations.FixedTimeEquals` |
| Access token | JWT HS512, 15 min expiry |
| Refresh token | 64-byte random, 7 days, stored in DB |
| Token rotation | New refresh token issued on every use |
| Soft delete | `IsDeleted` filter on all user queries |
| Role storage | Stored as string in DB ("User"/"Admin") |

---

## How to Create Admin User

Currently users register as `User` role. To make an admin, either:

**Option A** — Seed in code (add to `AppDbContext` or a seeder class):
```csharp
AppUser.Create("Admin", "admin@shop.com", "01700000000", hash, salt, UserRole.Admin);
```

**Option B** — Direct DB update (dev only):
```sql
UPDATE Users SET Role = 'Admin' WHERE Email = 'admin@shop.com';
```

**Option C** — Add an admin-registration endpoint protected by a secret header (recommended for production).

---

## Next Module
After testing auth, the next module is:
`Category → SubCategory → Brand → Product (CRUD with images)`
