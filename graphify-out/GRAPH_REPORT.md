# Graph Report - wardkitten  (2026-09-02)

## Corpus Check
- 192 files · ~50,981 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1970 nodes · 4142 edges · 138 communities (124 shown, 7 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 376 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `588059f1`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- MongoRepository
- CheckIn
- Watch
- MongoContext
- PingProbe
- .ReplaceAsync
- StatusPage
- OnCallSchedule
- IClock
- Instrucciones para agentes — Wardkitten
- WardkittenApiClient
- ChannelType
- .AddWardkittenInfrastructure
- .GetByIdAsync
- Wardkitten.Domain.Watches
- NotificationLog
- Wardkitten.Application.Abstractions.Persistence
- NotificationResult
- WatchEdit.razor
- .CreateAsync
- Result
- User
- Incident
- INotificationChannel
- TwilioChannelBase
- WatchStatus
- Home.razor
- SampleDocument
- WatchdogEndToEndTests.cs
- WardkittenMcpTools
- .AddWardkittenIntegrations
- .ChargeForMessageAsync
- AuthContracts.cs
- http
- Wardkitten.Domain.Identity
- .ToDtoAsync
- .ToDto
- CreditTransaction
- Subscription
- Severity
- Wardkitten.Application.Services
- Wardkitten.Infrastructure
- wardkitten.slnx
- http
- PlanLimits
- BillingService
- Wallet
- EmailOptions
- Wardkitten.Domain.Common
- NotificationMessage
- CancellationToken
- EvaluationWorker
- F04 — Motor de evaluación e incidentes
- Wardkitten.Web/_Imports.razor
- wardkitten/MainActivity.kt
- StatusPages.razor
- Teams.razor
- .EvaluateWatchAsync
- Schedule
- Tolerance
- TelegramChannel
- ITokenStore
- JwtAuthStateProvider
- Guía de publicación — Wardkitten (web + apps móviles)
- WatchTemplateCatalog
- ICreditTransactionRepository
- App.razor
- Wardkitten.IntegrationTests.csproj
- Wallet.razor
- StripeWebhookProcessor
- List
- Wardkitten.Shared.UI.Auth
- .AddWardkittenClient
- WatchStatus
- Wardkitten.Tests
- StripePaymentGateway
- IWatchEventPublisher
- IncidentService
- ChannelType
- StripeOptions
- HttpWatchEventPublisher
- StatusContracts.cs
- MainLayout.razor
- Política de seguridad — Wardkitten
- IChannelRateRepository
- .AddWardkittenApplication
- WatchStatus
- BillingContracts.cs
- .SendAsync
- Apps móviles nativas
- F02.01 — Watch (tarea vigilada)
- F03.03 — Banco de pruebas de la URL de ping (dry-run)
- F06 — Wallet de créditos (canales metered)
- LiveHubConnection
- Wardkitten.Api
- Incidents.razor
- Login.razor
- Register.razor
- Templates.razor
- ChannelBinding
- ScheduleKind
- Wardkitten.Worker
- 🐾 Wardkitten
- F14 — Endpoints MCP (Model Context Protocol)
- Directivas de documentación — Wardkitten
- SignalRWatchEventPublisher
- StatusView.razor
- IPaymentGateway
- MaintenanceWindow
- Wardkitten.Api.RealTime
- ADR · Apps móviles nativas en lugar de MAUI
- Futura mejora — NSwag: operationIds estables
- 1. WEB (Blazor WASM) — la más sencilla
- 4. iOS y watchOS
- .WithConfiguration_UsesConfiguredValues
- Wardkitten.Application
- QuietHours
- UserRepository
- ScheduleTests
- ToDo — Wardkitten
- C4 — Diagrama de contexto
- Tech debt — Wardkitten
- ClaimsPrincipal
- Wardkitten
- ChargeOutcome
- Wardkitten.Tests.Domain
- Hardcodeos activos en Wardkitten
- Wardkitten.Domain
- CreditLots.cs
- RedirectToLogin.razor
- Package.swift

## God Nodes (most connected - your core abstractions)
1. `Watch` - 84 edges
2. `Incident` - 56 edges
3. `WardkittenApiClient` - 53 edges
4. `Wardkitten.Domain.Watches` - 52 edges
5. `User` - 51 edges
6. `ApiResult` - 51 edges
7. `MongoContext` - 39 edges
8. `Wardkitten.Application.Abstractions.Persistence` - 36 edges
9. `Result` - 36 edges
10. `PingProbe` - 34 edges

## Surprising Connections (you probably didn't know these)
- `SignalRWatchEventPublisher` --references--> `WatchHub`  [EXTRACTED]
  NimitaCo/wardkitten/src/Wardkitten.Api/RealTime/SignalRWatchEventPublisher.cs → NimitaCo/wardkitten/src/Wardkitten.Api/RealTime/WatchHub.cs
- `SignalRWatchEventPublisher` --implements--> `IWatchEventPublisher`  [EXTRACTED]
  NimitaCo/wardkitten/src/Wardkitten.Api/RealTime/SignalRWatchEventPublisher.cs → NimitaCo/wardkitten/src/Wardkitten.Application/RealTime/IWatchEventPublisher.cs
- `EvaluationEngine` --references--> `IClock`  [EXTRACTED]
  NimitaCo/wardkitten/src/Wardkitten.Application/Evaluation/EvaluationEngine.cs → NimitaCo/wardkitten/src/Wardkitten.Application/Abstractions/IClock.cs
- `NotificationDispatcher` --references--> `IClock`  [EXTRACTED]
  NimitaCo/wardkitten/src/Wardkitten.Application/Notifications/NotificationDispatcher.cs → NimitaCo/wardkitten/src/Wardkitten.Application/Abstractions/IClock.cs
- `AuthService` --references--> `IClock`  [EXTRACTED]
  NimitaCo/wardkitten/src/Wardkitten.Application/Services/AuthService.cs → NimitaCo/wardkitten/src/Wardkitten.Application/Abstractions/IClock.cs

## Import Cycles
- None detected.

## Communities (138 total, 7 thin omitted)

### Community 0 - "MongoRepository"
Cohesion: 0.05
Nodes (46): FilterDefinition, CancellationToken, DateTime, IAsyncEnumerable, IReadOnlyList, Task, ICheckInRepository, IEscalationPolicyRepository (+38 more)

### Community 1 - "CheckIn"
Cohesion: 0.06
Nodes (41): HttpRequest, IMongoRunner, IResult, ServiceProvider, CancellationToken, HttpContext, IEndpointRouteBuilder, Task (+33 more)

### Community 2 - "Watch"
Cohesion: 0.07
Nodes (29): DateTime, List, Watch, BestStreak, ChannelBindings, ConsecutiveMisses, CurrentIncidentId, CurrentStreak (+21 more)

### Community 3 - "MongoContext"
Cohesion: 0.06
Nodes (35): IMongoDatabase, IServiceProvider, Lease, DateTime, RefreshToken, CreatedByIp, ExpiresAtUtc, ReplacedByTokenHash (+27 more)

### Community 4 - "PingProbe"
Cohesion: 0.08
Nodes (25): DateTime, List, TimeSpan, PingProbe, ExpiresAtUtc, HitCount, Hits, LastHitAtUtc (+17 more)

### Community 5 - ".ReplaceAsync"
Cohesion: 0.14
Nodes (15): HttpContext, IEndpointRouteBuilder, AuthEndpoints, CancellationToken, Task, DateTime, AccessToken, ITokenService (+7 more)

### Community 6 - "StatusPage"
Cohesion: 0.10
Nodes (25): GeneratedRegex, Regex, IEndpointRouteBuilder, StatusPageEndpoints, CancellationToken, IReadOnlyList, Task, IStatusPageRepository (+17 more)

### Community 7 - "OnCallSchedule"
Cohesion: 0.07
Nodes (27): CancellationToken, IReadOnlyList, Task, DateTime, List, OnCallOverride, EndUtc, StartUtc (+19 more)

### Community 8 - "IClock"
Cohesion: 0.09
Nodes (22): Wardkitten.Tests, DateTimeOffset, DateTime, IClock, UtcNow, SystemClock, UtcNow, IMagicLinkValidator (+14 more)

### Community 9 - "Instrucciones para agentes — Wardkitten"
Cohesion: 0.06
Nodes (32): Autorización, Bloqueos de MongoDB (CPU alta / COLLSCAN), Concurrencia del worker, Despliegue Linux vs. desarrollo Windows, Documentación, Estado en wardkitten: **adoptado por `PackageReference` 26.7.5** (2026-07-28), Estructura del repositorio, Hardcodeos y tech-debt (+24 more)

### Community 10 - "WardkittenApiClient"
Cohesion: 0.19
Nodes (8): HttpClient, VerifyCodeRequest, ApiResult, HttpResponseMessage, JsonElement, Task, WardkittenApiClient, BaseAddress

### Community 11 - "ChannelType"
Cohesion: 0.10
Nodes (24): IReadOnlyDictionary, IAckLinkBuilder, CancellationToken, ChannelBinding, DateTime, ILogger, Task, TimeZoneInfo (+16 more)

### Community 12 - ".AddWardkittenInfrastructure"
Cohesion: 0.14
Nodes (21): Harness, IMongoClient, ISubscriptionRepository, IWalletRepository, IRefreshTokenRepository, IUserRepository, IRepository, ITeamRepository (+13 more)

### Community 13 - ".GetByIdAsync"
Cohesion: 0.17
Nodes (13): IEndpointRouteBuilder, InternalEndpoints, CancellationToken, IReadOnlyList, Task, CancellationToken, DateTime, IReadOnlyList (+5 more)

### Community 14 - "Wardkitten.Domain.Watches"
Cohesion: 0.17
Nodes (8): Wardkitten.Worker, Wardkitten.Domain.Incidents, Wardkitten.Application.Evaluation, Wardkitten.Application.Notifications, Wardkitten.Domain.Watches, Wardkitten.Infrastructure.Notifications, Wardkitten.Application.RealTime, System.Net.Http.Json

### Community 15 - "NotificationLog"
Cohesion: 0.11
Nodes (22): CancellationToken, IReadOnlyList, Task, INotificationLogRepository, DateTime, NotificationLog, Channel, CreditsCharged (+14 more)

### Community 16 - "Wardkitten.Application.Abstractions.Persistence"
Cohesion: 0.20
Nodes (7): Wardkitten.Domain.Teams, Wardkitten.Application.Abstractions, Wardkitten.Application.Common, Wardkitten.Infrastructure.Mongo.Repositories, Wardkitten.Domain.StatusPages, Wardkitten.Application.Abstractions.Persistence, Wardkitten.Domain.Billing

### Community 17 - "NotificationResult"
Cohesion: 0.14
Nodes (16): FirebaseApp, NotificationResult, Error, ProviderMessageId, Success, CancellationToken, Task, PushChannel (+8 more)

### Community 18 - "WatchEdit.razor"
Cohesion: 0.08
Nodes (24): PingTestHitDto, route:/watches/{Id}/edit, route:/watches/new, Apply, BuildBindings, CopyAsync, DisposeAsync, Hace (+16 more)

### Community 19 - ".CreateAsync"
Cohesion: 0.18
Nodes (14): IEndpointRouteBuilder, WatchEndpoints, SecureTokenGenerator, CancellationToken, ChannelBinding, IReadOnlyList, List, Task (+6 more)

### Community 20 - "Result"
Cohesion: 0.27
Nodes (9): IEndpointRouteBuilder, Result, CancellationToken, DateTime, IReadOnlyList, List, Task, Team (+1 more)

### Community 21 - "User"
Cohesion: 0.08
Nodes (23): DateTime, List, Plan, Roles, User, DisplayName, Email, EmailVerificationCodeHash (+15 more)

### Community 22 - "Incident"
Cohesion: 0.09
Nodes (22): DateTime, List, Severity, Incident, AcknowledgedAtUtc, AcknowledgedBy, CurrentEscalationStep, Deliveries (+14 more)

### Community 23 - "INotificationChannel"
Cohesion: 0.10
Nodes (20): CancellationToken, IEnumerable, Task, CancellationToken, Task, INotificationChannel, Channel, IsMetered (+12 more)

### Community 24 - "TwilioChannelBase"
Cohesion: 0.11
Nodes (17): CancellationToken, IHttpClientFactory, Task, TwilioChannelBase, Channel, From, TwilioOptions, AccountSid (+9 more)

### Community 25 - "WatchStatus"
Cohesion: 0.13
Nodes (16): Codable, Foundation, Identifiable, WatchStatus, WardkittenApi, Watch, WatchStatus, failing (+8 more)

### Community 26 - "Home.razor"
Cohesion: 0.10
Nodes (19): CriticalityBadge, route:/, CheckInAsync, DeleteAsync, Dispose, FormatDue, HandleChanged, LoadAsync (+11 more)

### Community 27 - "SampleDocument"
Cohesion: 0.12
Nodes (16): SampleDocument, SampleState, MongoDbConfigurator, DateTime, Fact, MongoConventionsGuardTests, SampleDocument, BalanceCredits (+8 more)

### Community 28 - "WatchdogEndToEndTests.cs"
Cohesion: 0.13
Nodes (10): Wardkitten.Application.DependencyInjection, Wardkitten.Infrastructure.Time, Wardkitten.IntegrationTests, Wardkitten.Api.Mcp, Wardkitten.Tests.Infrastructure, Wardkitten.Infrastructure.Mongo, Wardkitten.Infrastructure.DependencyInjection, Program (+2 more)

### Community 29 - "WardkittenMcpTools"
Cohesion: 0.45
Nodes (7): IHttpContextAccessor, JsonSerializerOptions, McpServerTool, CancellationToken, Task, WardkittenMcpTools, Description

### Community 30 - ".AddWardkittenIntegrations"
Cohesion: 0.13
Nodes (12): IPasswordHasher, IConfiguration, IServiceCollection, IntegrationsRegistration, PushOptions, ServiceAccountJson, BcryptPasswordHasher, JwtOptions (+4 more)

### Community 31 - ".ChargeForMessageAsync"
Cohesion: 0.22
Nodes (7): CancellationToken, IReadOnlyList, Task, ChargeResult, IsCharged, IsInsufficient, WalletService

### Community 32 - "AuthContracts.cs"
Cohesion: 0.21
Nodes (11): DateTime, IReadOnlyList, AuthResponse, LoginRequest, PhoneOtpRequest, PushTokenRequest, RefreshRequest, RegisterRequest (+3 more)

### Community 33 - "http"
Cohesion: 0.12
Nodes (17): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, inspectUri, launchBrowser, applicationUrl (+9 more)

### Community 34 - "Wardkitten.Domain.Identity"
Cohesion: 0.17
Nodes (6): Wardkitten.Domain.Identity, Wardkitten.Application.Billing, Wardkitten.Tests.Application, Wardkitten.Infrastructure.Security, Wardkitten.Infrastructure.Billing, Wardkitten.Application.Security

### Community 35 - ".ToDtoAsync"
Cohesion: 0.21
Nodes (14): CancellationToken, Task, TeamDto, TeamEndpoints, DateTime, List, AddMemberRequest, AddOnCallOverrideRequest (+6 more)

### Community 36 - ".ToDto"
Cohesion: 0.20
Nodes (9): DtoMappings, DateTime, List, CheckInDto, PingTestHitDto, PingTestStateDto, StartPingTestRequest, WatchDto (+1 more)

### Community 37 - "CreditTransaction"
Cohesion: 0.12
Nodes (16): CreditTransaction, AmountCredits, BalanceAfter, Channel, IdempotencyKey, ProviderReference, Reason, Type (+8 more)

### Community 38 - "Subscription"
Cohesion: 0.12
Nodes (16): DateTime, Subscription, CancelAtPeriodEnd, CurrentPeriodEndUtc, GrantsPaidFeatures, Plan, Status, StripeCustomerId (+8 more)

### Community 39 - "Severity"
Cohesion: 0.17
Nodes (12): Dictionary, CriticalityCatalog, CriticalityPolicy, Severity, Critical, High, Low, Medium (+4 more)

### Community 40 - "Wardkitten.Application.Services"
Cohesion: 0.35
Nodes (6): Wardkitten.Application.Services, Wardkitten.Domain.CheckIns, Wardkitten.Api.Security, Wardkitten.Api.Mapping, Wardkitten.Api.Endpoints, Wardkitten.Shared.Contracts

### Community 41 - "Wardkitten.Infrastructure"
Cohesion: 0.12
Nodes (16): Anthropic (12.39.0), BCrypt.Net-Next (4.2.0), Es.Nimita.Infra.Mongo (26.7.5), FirebaseAdmin (3.6.0), MailKit (4.17.0), Microsoft.Extensions.Configuration.Binder (10.0.10), Microsoft.Extensions.Hosting.Abstractions (10.0.10), Microsoft.Extensions.Options.ConfigurationExtensions (10.0.10) (+8 more)

### Community 42 - "wardkitten.slnx"
Cohesion: 0.16
Nodes (15): Microsoft.AspNetCore.Components.Authorization (10.0.10), Microsoft.AspNetCore.Components.Web (10.0.10), Microsoft.AspNetCore.Components.WebAssembly (10.0.10), Microsoft.AspNetCore.Components.WebAssembly.DevServer (10.0.10), Microsoft.AspNetCore.SignalR.Client (9.0.18), Microsoft.Extensions.Hosting (10.0.10), Microsoft.NET.Sdk.BlazorWebAssembly, Microsoft.NET.Sdk.Razor (+7 more)

### Community 43 - "http"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 44 - "PlanLimits"
Cohesion: 0.16
Nodes (14): Dictionary, Plan, Free, Pro, Team, PlanCatalog, PlanLimits, EscalationPolicies (+6 more)

### Community 45 - "BillingService"
Cohesion: 0.26
Nodes (7): IConfiguration, IEndpointRouteBuilder, MoneyEndpoints, CancellationToken, DateTime, Task, BillingService

### Community 46 - "Wallet"
Cohesion: 0.17
Nodes (12): Wallet, AutoTopUpAmountCredits, AutoTopUpEnabled, BalanceCredits, Currency, IsBelowThreshold, MinThresholdCredits, UserId (+4 more)

### Community 47 - "EmailOptions"
Cohesion: 0.15
Nodes (12): CancellationToken, Task, EmailChannel, Channel, EmailOptions, FromAddress, FromName, Host (+4 more)

### Community 48 - "Wardkitten.Domain.Common"
Cohesion: 0.14
Nodes (8): Wardkitten.Domain.Common, Wardkitten.Domain.Notifications, SubscriptionStatus, Active, Canceled, Incomplete, PastDue, Trialing

### Community 49 - "NotificationMessage"
Cohesion: 0.15
Nodes (13): IReadOnlyList, Severity, NotificationAction, NotificationMessage, AckUrl, Actions, Body, Channel (+5 more)

### Community 50 - "CancellationToken"
Cohesion: 0.33
Nodes (5): CancellationToken, IReadOnlyList, Task, SubscriptionRepository, WalletRepository

### Community 51 - "EvaluationWorker"
Cohesion: 0.24
Nodes (10): BackgroundService, PeriodicTimer, CancellationToken, IConfiguration, IHttpClientFactory, ILeaseStore, ILogger, Task (+2 more)

### Community 52 - "F04 — Motor de evaluación e incidentes"
Cohesion: 0.17
Nodes (8): Componentes, Descripción, F04 — Motor de evaluación e incidentes, Metadata, Modelo de datos, Reglas de negocio, Verificación, Feature Registry — Wardkitten

### Community 53 - "Wardkitten.Web/_Imports.razor"
Cohesion: 0.15
Nodes (11): Microsoft.AspNetCore.Components.Forms, Microsoft.AspNetCore.Components.Routing, Microsoft.AspNetCore.Components.Web.Virtualization, Microsoft.AspNetCore.Components.WebAssembly.Http, Microsoft.JSInterop, Microsoft.AspNetCore.Components.Authorization, Microsoft.AspNetCore.Components.Web, System.Net.Http (+3 more)

### Community 54 - "wardkitten/MainActivity.kt"
Cohesion: 0.24
Nodes (9): DashboardScreen(), Bundle, ComponentActivity, MainActivity, Bundle, ComponentActivity, MainActivity, WearApp() (+1 more)

### Community 55 - "StatusPages.razor"
Cohesion: 0.15
Nodes (12): route:/status-pages, CreateAsync, DeleteAsync, LoadAsync, OnInitializedAsync, PublicUrl, NavigationManager, PageTitle (+4 more)

### Community 56 - "Teams.razor"
Cohesion: 0.15
Nodes (12): route:/teams, AddMemberAsync, CreateAsync, DeleteAsync, LoadAsync, OnCallName, OnInitializedAsync, PageTitle (+4 more)

### Community 57 - ".EvaluateWatchAsync"
Cohesion: 0.28
Nodes (7): CancellationToken, DateTime, ILogger, Task, EvaluationEngine, CancellationToken, Task

### Community 58 - "Schedule"
Cohesion: 0.22
Nodes (9): DateTime, List, TimeZoneInfo, Schedule, CalendarDatesLocal, CronExpression, IntervalSeconds, Kind (+1 more)

### Community 59 - "Tolerance"
Cohesion: 0.15
Nodes (9): TimeSpan, Tolerance, Grace, GraceSeconds, None, SkipTolerance, InlineData, Theory (+1 more)

### Community 60 - "TelegramChannel"
Cohesion: 0.17
Nodes (11): CancellationToken, IHttpClientFactory, Task, TelegramChannel, Channel, TelegramMessage, TelegramOptions, BotToken (+3 more)

### Community 61 - "ITokenStore"
Cohesion: 0.33
Nodes (4): IJSRuntime, ValueTask, ITokenStore, LocalStorageTokenStore

### Community 62 - "JwtAuthStateProvider"
Cohesion: 0.23
Nodes (8): AuthenticationState, AuthenticationStateProvider, Claim, Dictionary, IEnumerable, JsonElement, Task, JwtAuthStateProvider

### Community 63 - "Guía de publicación — Wardkitten (web + apps móviles)"
Cohesion: 0.17
Nodes (12): 0. Estado actual del proyecto, 2.1 Herramientas, 2.2 Assets pendientes, 2. MÓVIL — preparación común, 3.1 Clave de subida (una sola vez), 3.2 Compilar el bundle, 3.3 Subir, 3.4 Requisitos de ficha (+4 more)

### Community 64 - "WatchTemplateCatalog"
Cohesion: 0.20
Nodes (8): IEndpointRouteBuilder, TemplateEndpoints, ChannelBinding, IReadOnlyList, List, WatchTemplate, WatchTemplateCatalog, All

### Community 65 - "ICreditTransactionRepository"
Cohesion: 0.27
Nodes (5): CancellationToken, IReadOnlyList, Task, ICreditTransactionRepository, CreditTransactionRepository

### Community 66 - "App.razor"
Cohesion: 0.18
Nodes (10): AuthorizeRouteView, Authorizing, CascadingAuthenticationState, FocusOnNavigate, Found, RedirectToLogin, Router, Microsoft.AspNetCore.Components.Authorization (+2 more)

### Community 67 - "Wardkitten.IntegrationTests.csproj"
Cohesion: 0.18
Nodes (10): net10.0, EphemeralMongo (3.2.0), coverlet.collector (10.0.1), Microsoft.Extensions.Configuration (10.0.10), Microsoft.Extensions.DependencyInjection (10.0.10), Microsoft.NET.Test.Sdk (18.8.1), Shouldly (4.3.0), xunit (2.9.3) (+2 more)

### Community 68 - "Wallet.razor"
Cohesion: 0.18
Nodes (10): route:/wallet, LoadAsync, OnInitializedAsync, PortalAsync, CreditTransactionDto, NavigationManager, PageTitle, WardkittenApiClient (+2 more)

### Community 69 - "StripeWebhookProcessor"
Cohesion: 0.33
Nodes (6): Session, CancellationToken, ILogger, Subscription, Task, StripeWebhookProcessor

### Community 70 - "List"
Cohesion: 0.20
Nodes (5): DateTime, CreditTransactionDto, IncidentDto, WatchTemplateDto, List

### Community 71 - "Wardkitten.Shared.UI.Auth"
Cohesion: 0.24
Nodes (4): Wardkitten.Shared.UI.Services, Wardkitten.Shared.UI.Auth, Microsoft.AspNetCore.Components.Authorization, Microsoft.AspNetCore.Components.Web

### Community 72 - ".AddWardkittenClient"
Cohesion: 0.20
Nodes (7): Wardkitten.Shared.UI.DependencyInjection, AuthenticationStateProvider, IServiceCollection, ClientServiceCollectionExtensions, Type, Uri, Wardkitten.Web

### Community 73 - "WatchStatus"
Cohesion: 0.22
Nodes (7): WardkittenApi, Watch, WatchStatus, Failing, Late, Ok, Paused

### Community 74 - "Wardkitten.Tests"
Cohesion: 0.20
Nodes (10): NSubstitute (5.3.0), Wardkitten.Tests, coverlet.collector (10.0.1), Microsoft.Extensions.Configuration (10.0.10), Microsoft.Extensions.DependencyInjection (10.0.10), Microsoft.NET.Test.Sdk (18.8.1), Shouldly (4.3.0), xunit (2.9.3) (+2 more)

### Community 75 - "StripePaymentGateway"
Cohesion: 0.42
Nodes (5): SessionCreateOptions, SessionLineItemOptions, CancellationToken, Task, StripePaymentGateway

### Community 76 - "IWatchEventPublisher"
Cohesion: 0.42
Nodes (4): CancellationToken, Task, IWatchEventPublisher, NoopWatchEventPublisher

### Community 77 - "IncidentService"
Cohesion: 0.38
Nodes (5): CancellationToken, IReadOnlyList, Task, TimeSpan, IncidentService

### Community 78 - "ChannelType"
Cohesion: 0.20
Nodes (10): ChannelType, Discord, Email, MicrosoftTeams, Push, Slack, Sms, Telegram (+2 more)

### Community 79 - "StripeOptions"
Cohesion: 0.20
Nodes (10): StripeOptions, AutomaticTaxEnabled, CreditCurrency, CreditTaxBehavior, CreditUnitAmountCents, PriceCredit, PriceProMonthly, PriceTeamMonthly (+2 more)

### Community 80 - "HttpWatchEventPublisher"
Cohesion: 0.47
Nodes (6): InternalEventRequest, CancellationToken, IHttpClientFactory, ILogger, Task, HttpWatchEventPublisher

### Community 81 - "StatusContracts.cs"
Cohesion: 0.29
Nodes (6): DateTime, List, PublicStatusPageDto, StatusItemDto, StatusPageDto, StatusPageRequest

### Community 82 - "MainLayout.razor"
Cohesion: 0.22
Nodes (8): Authorized, AuthorizeView, LayoutComponentBase, NavLink, LogoutAsync, ClientAuthService, NavigationManager, NotAuthorized

### Community 83 - "Política de seguridad — Wardkitten"
Cohesion: 0.22
Nodes (8): 1. Gestión de secretos, 2. Autenticación y autorización, 3. Endpoints públicos (sin sesión), 4. Wallet / canales metered (anti-abuso), 5. Datos y privacidad (GDPR), 6. Dependencias y supply chain, 7. Reporte de vulnerabilidades, Política de seguridad — Wardkitten

### Community 84 - "IChannelRateRepository"
Cohesion: 0.25
Nodes (7): IChannelRateRepository, ChannelRate, Channel, CountryPrefix, CreditsPerMessage, Dictionary, ChannelRateRepository

### Community 85 - ".AddWardkittenApplication"
Cohesion: 0.25
Nodes (5): IServiceCollection, ApplicationRegistration, DefaultAckLinkBuilder, NotificationOptions, PublicBaseUrl

### Community 86 - "WatchStatus"
Cohesion: 0.22
Nodes (7): ChannelTypeExtensions, WatchStatus, Down, Grace, New, Paused, Up

### Community 87 - "BillingContracts.cs"
Cohesion: 0.28
Nodes (4): CheckoutResponse, SubscribeRequest, TopUpRequest, WalletDto

### Community 88 - ".SendAsync"
Cohesion: 0.25
Nodes (6): DelegatingHandler, HttpRequestMessage, CancellationToken, HttpResponseMessage, Task, BearerHandler

### Community 89 - "Apps móviles nativas"
Cohesion: 0.25
Nodes (5): Android, Apps móviles nativas, Contratos, Estado, iOS + watchOS

### Community 90 - "F02.01 — Watch (tarea vigilada)"
Cohesion: 0.25
Nodes (8): Dependencias, Descripción, Elementos UI, Endpoints, F02.01 — Watch (tarea vigilada), Metadata, Modelo de datos (MongoDB, `watches`), Reglas de negocio

### Community 91 - "F03.03 — Banco de pruebas de la URL de ping (dry-run)"
Cohesion: 0.25
Nodes (8): Dependencias / Sub-features, Descripción, Elementos UI, Endpoints, F03.03 — Banco de pruebas de la URL de ping (dry-run), Metadata, Modelo de datos (MongoDB, `pingProbes`), Reglas de negocio

### Community 92 - "F06 — Wallet de créditos (canales metered)"
Cohesion: 0.25
Nodes (8): Descripción, Elementos UI, Endpoints, F06 — Wallet de créditos (canales metered), Metadata, Modelo de datos (MongoDB), Reglas de negocio, Verificación

### Community 93 - "LiveHubConnection"
Cohesion: 0.25
Nodes (6): HubConnection, IAsyncDisposable, Task, ValueTask, LiveHubConnection, IsConnected

### Community 94 - "Wardkitten.Api"
Cohesion: 0.25
Nodes (8): Microsoft.AspNetCore.Authentication.JwtBearer (10.0.10), Microsoft.AspNetCore.Components.WebAssembly.Server (10.0.10), Microsoft.AspNetCore.OpenApi (10.0.10), Microsoft.OpenApi (2.11.0), ModelContextProtocol.AspNetCore (1.4.1), Swashbuckle.AspNetCore (7.3.2), Microsoft.NET.Sdk.Web, Wardkitten.Api

### Community 95 - "Incidents.razor"
Cohesion: 0.25
Nodes (7): route:/incidents, AckAsync, LoadAsync, OnInitializedAsync, IncidentDto, PageTitle, WardkittenApiClient

### Community 96 - "Login.razor"
Cohesion: 0.25
Nodes (7): route:/login, ClientAuthService, EditForm, InputText, NavigationManager, PageTitle, SubmitAsync

### Community 97 - "Register.razor"
Cohesion: 0.25
Nodes (7): route:/register, ClientAuthService, EditForm, InputText, NavigationManager, PageTitle, SubmitAsync

### Community 98 - "Templates.razor"
Cohesion: 0.25
Nodes (7): route:/templates, OnInitializedAsync, NavigationManager, PageTitle, WardkittenApiClient, WatchTemplateDto, UseAsync

### Community 99 - "ChannelBinding"
Cohesion: 0.25
Nodes (8): TimeSpan, ChannelBinding, DestinationOverride, Enabled, EscalationDelay, EscalationDelaySeconds, Order, QuietHours

### Community 100 - "ScheduleKind"
Cohesion: 0.25
Nodes (6): ScheduleKind, Calendar, Cron, Interval, InlineData, Theory

### Community 101 - "Wardkitten.Worker"
Cohesion: 0.25
Nodes (7): DOTNET_ENVIRONMENT, profiles, Wardkitten.Worker, $schema, commandName, dotnetRunMessages, environmentVariables

### Community 102 - "🐾 Wardkitten"
Cohesion: 0.29
Nodes (5): Arquitectura, Cómo funciona, Desarrollo, Planes y créditos, 🐾 Wardkitten

### Community 103 - "F14 — Endpoints MCP (Model Context Protocol)"
Cohesion: 0.29
Nodes (7): Cómo conectarse, Descripción, Endpoint, F14 — Endpoints MCP (Model Context Protocol), Herramientas expuestas (F14.01), Metadata, Reglas de negocio

### Community 104 - "Directivas de documentación — Wardkitten"
Cohesion: 0.29
Nodes (6): Directivas de documentación — Wardkitten, Estructura, Ficha de feature (`docs/features/FXX-nombre/overview.md`), Principios, Reglas para agentes, Sistema de numeración de features

### Community 105 - "SignalRWatchEventPublisher"
Cohesion: 0.48
Nodes (4): IHubContext, CancellationToken, Task, SignalRWatchEventPublisher

### Community 106 - "StatusView.razor"
Cohesion: 0.29
Nodes (6): route:/status/{Slug}, CssFor, OnInitializedAsync, PageTitle, WardkittenApiClient, TextFor

### Community 107 - "IPaymentGateway"
Cohesion: 0.48
Nodes (4): CancellationToken, Plan, Task, IPaymentGateway

### Community 108 - "MaintenanceWindow"
Cohesion: 0.33
Nodes (5): DateTime, MaintenanceWindow, EndUtc, Reason, StartUtc

### Community 109 - "Wardkitten.Api.RealTime"
Cohesion: 0.33
Nodes (4): Wardkitten.Api.RealTime, Hub, Task, WatchHub

### Community 110 - "ADR · Apps móviles nativas en lugar de MAUI"
Cohesion: 0.33
Nodes (6): ADR · Apps móviles nativas en lugar de MAUI, Alternativa descartada, Consecuencias, Contexto, Decisión, El problema

### Community 111 - "Futura mejora — NSwag: operationIds estables"
Cohesion: 0.33
Nodes (5): Cuándo, Cómo ejecutarlo, El problema, Futura mejora — NSwag: operationIds estables, La idea

### Community 112 - "1. WEB (Blazor WASM) — la más sencilla"
Cohesion: 0.33
Nodes (6): 1.1 Probar en local, 1.2 Publicar la imagen a GHCR (automático con CI), 1.3 Desplegar en Kubernetes, 1.4 Dominios y orígenes, 1.5 Verificación, 1. WEB (Blazor WASM) — la más sencilla

### Community 113 - "4. iOS y watchOS"
Cohesion: 0.33
Nodes (6): 4.1 Requisito previo, 4.2 Certificados, 4.3 Archivar y subir, 4.4 TestFlight, 4.5 Requisitos de ficha, 4. iOS y watchOS

### Community 114 - ".WithConfiguration_UsesConfiguredValues"
Cohesion: 0.53
Nodes (4): IOptions, MongoSettings, Fact, MongoSettingsRegistrationTests

### Community 115 - "Wardkitten.Application"
Cohesion: 0.33
Nodes (6): Es.Nimita.Domain.Primitives (26.7.5), Microsoft.Extensions.DependencyInjection.Abstractions (10.0.10), Microsoft.Extensions.Logging.Abstractions (10.0.10), Microsoft.Extensions.Options (10.0.10), Wardkitten.Application, Microsoft.NET.Sdk

### Community 116 - "QuietHours"
Cohesion: 0.33
Nodes (4): DateTime, QuietHours, EndMinute, StartMinute

### Community 117 - "UserRepository"
Cohesion: 0.53
Nodes (3): CancellationToken, Task, UserRepository

### Community 119 - "ToDo — Wardkitten"
Cohesion: 0.33
Nodes (5): Decisiones de proyecto, Fases, Pendiente - acceso al feed NuGet de NimitaCo, Pendientes funcionales (post-v1), ToDo — Wardkitten

### Community 120 - "C4 — Diagrama de contexto"
Cohesion: 0.40
Nodes (4): C4 — Diagrama de contexto, Descripción, Diagrama (Mermaid), Notas

### Community 121 - "Tech debt — Wardkitten"
Cohesion: 0.40
Nodes (4): Advisories de dependencias aceptados, Deprecaciones, Pendientes bloqueados por terceros / herramientas (no realizables en código), Tech debt — Wardkitten

### Community 123 - "Wardkitten"
Cohesion: 0.50
Nodes (3): Iconos (front), Publicar nueva versión (K8S deploy), Wardkitten

### Community 124 - "ChargeOutcome"
Cohesion: 0.50
Nodes (4): ChargeOutcome, Charged, Free, InsufficientFunds

### Community 127 - "Wardkitten.Domain"
Cohesion: 0.67
Nodes (3): NCrontab (3.4.0), Wardkitten.Domain, Microsoft.NET.Sdk

## Knowledge Gaps
- **678 isolated node(s):** `Ok`, `Late`, `Failing`, `Paused`, `PackageDescription` (+673 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 906 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **7 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Watch` connect `Watch` to `MongoRepository`, `CheckIn`, `MongoContext`, `PingProbe`, `StatusPage`, `ChannelType`, `.GetByIdAsync`, `.CreateAsync`, `INotificationChannel`, `WardkittenMcpTools`, `.ToDto`, `Subscription`, `Severity`, `Wardkitten.Domain.Common`, `.EvaluateWatchAsync`, `Schedule`, `Tolerance`, `IWatchEventPublisher`, `IncidentService`, `HttpWatchEventPublisher`, `WatchStatus`, `ChannelBinding`, `SignalRWatchEventPublisher`, `MaintenanceWindow`?**
  _High betweenness centrality (0.086) - this node is a cross-community bridge._
- **Why does `Wardkitten.Domain.Watches` connect `Wardkitten.Domain.Watches` to `MongoRepository`, `.AddWardkittenInfrastructure`, `Wardkitten.Application.Abstractions.Persistence`, `WatchdogEndToEndTests.cs`, `Wardkitten.Domain.Identity`, `.ToDto`, `CreditTransaction`, `Severity`, `Wardkitten.Application.Services`, `Wardkitten.Domain.Common`, `NotificationMessage`, `Wardkitten.Web/_Imports.razor`, `Schedule`, `Tolerance`, `WatchTemplateCatalog`, `Wardkitten.Shared.UI.Auth`, `IChannelRateRepository`, `WatchStatus`, `MaintenanceWindow`, `QuietHours`, `ScheduleTests`, `Wardkitten.Tests.Domain`?**
  _High betweenness centrality (0.068) - this node is a cross-community bridge._
- **Why does `User` connect `User` to `Wardkitten.Domain.Identity`, `MongoContext`, `.ToDto`, `.ReplaceAsync`, `Subscription`, `ChannelType`, `IPaymentGateway`, `.AddWardkittenInfrastructure`, `StripePaymentGateway`, `UserRepository`?**
  _High betweenness centrality (0.052) - this node is a cross-community bridge._
- **Are the 4 inferred relationships involving `Watch` (e.g. with `.FullWatchdogLoop_OnRealMongo()` and `.BreachedWatch_OpensIncidentAndAlertsOnce()`) actually correct?**
  _`Watch` has 4 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Ok`, `Late`, `Failing` to the rest of the system?**
  _678 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `MongoRepository` be split into smaller, more focused modules?**
  _Cohesion score 0.05396825396825397 - nodes in this community are weakly interconnected._
- **Should `CheckIn` be split into smaller, more focused modules?**
  _Cohesion score 0.06009783368273934 - nodes in this community are weakly interconnected._