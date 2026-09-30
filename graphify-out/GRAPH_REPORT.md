# Graph Report - wardkitten  (2026-09-30)

## Corpus Check
- 225 files · ~70,584 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2599 nodes · 5883 edges · 168 communities (153 shown, 6 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 469 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `9f8722aa`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- CancellationToken
- .HandlePingAsync
- Watch
- MongoContext
- PingProbe
- .ReplaceAsync
- StatusPage
- OnCallSchedule
- HmacMagicLinkService
- Instrucciones para agentes — Wardkitten
- WardkittenApiClient
- ChannelType
- .Build
- PingProbeService
- Wardkitten.Domain.Watches
- NotificationLog
- Wardkitten.Application.Abstractions.Persistence
- NotificationResult
- WatchEdit.razor
- Result
- TeamService
- User
- Incident
- NotificationMessage
- TwilioChannelBase
- WatchStatus
- Home.razor
- SampleDocument
- Wardkitten.Application.RealTime
- Severity
- .AddWardkittenIntegrations
- IWalletRepository
- UserDto
- http
- Wardkitten.Domain.Identity
- .ToDtoAsync
- PingTestStateDto
- CreditTransaction
- Subscription
- Welcome
- Wardkitten.Application.Services
- Wardkitten.Infrastructure
- Wardkitten.Shared.UI
- http
- PlanLimits
- .GetByIdAsync
- Wallet
- EmailOptions
- SubscriptionStatus
- WelcomeTests
- CancellationToken
- EvaluationWorker
- F04 — Motor de evaluación e incidentes
- Wardkitten.Domain.CheckIns
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
- ChannelBinding
- .Build
- App.razor
- Wardkitten.IntegrationTests.csproj
- Wallet.razor
- StripeWebhookProcessor
- .ToDto
- .Build
- .AddWardkittenClient
- WatchStatus
- Wardkitten.Tests
- StripePaymentGateway
- IWatchRepository
- PingTestBenchTests
- PingTestBench
- StripeOptions
- HttpWatchEventPublisher
- .MapStatusPageEndpoints
- MainLayout.razor
- Política de seguridad — Wardkitten
- .AddWardkittenInfrastructure
- IAckLinkBuilder
- .Wizard_Profile_Channels_Test_FirstPingMonitor_AndComplete
- CancellationToken
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
- Task
- FakeApi
- Wardkitten.Worker
- .Build
- F14 — Endpoints MCP (Model Context Protocol)
- Directivas de documentación — Wardkitten
- SignalRWatchEventPublisher
- StatusView.razor
- IPaymentGateway
- MaintenanceWindow
- OnboardingHelpersTests
- ADR · Apps móviles nativas en lugar de MAUI
- Futura mejora — NSwag: operationIds estables
- OnboardingPagesTests
- 4. iOS y watchOS
- TelegramLinkCodeDto
- Wardkitten.Application
- EscalationPolicy
- UserRepository
- IUserRepository
- ToDo — Wardkitten
- C4 — Diagrama de contexto
- Tech debt — Wardkitten
- ClaimsPrincipal
- Wardkitten
- ChargeOutcome
- TelegramLinkService
- Hardcodeos activos en Wardkitten
- .CreateAsync
- Wardkitten.Domain.Billing
- RedirectToLogin.razor
- Package.swift
- OnboardingService
- AlertDelivery
- OnboardingProgress
- RefreshToken
- CheckIn
- MongoRepository
- Task
- .Hace_IsHumanReadable
- PingTestModeTests
- .ProbeDuringCreation_DoesNotCount_AndItsUrlBecomesTheRealOne
- OnboardingLayout.razor
- F05.05 — Vinculación de Telegram por deep link
- Wardkitten.Web.Tests
- F01.04 — Asistente de bienvenida (onboarding)
- IClock
- .Load
- F05.06 — Envío de prueba por un canal
- CheckInSource
- .Current
- Welcome.razor
- _registry.md
- CreditTransactionType
- F02.05 — Canales por defecto del usuario
- TelegramUpdateOutcome
- WizardStep
- .Main
- PingResolution
- ChannelTestButton.razor

## God Nodes (most connected - your core abstractions)
1. `Welcome` - 91 edges
2. `PingTestBench` - 86 edges
3. `Watch` - 85 edges
4. `Wardkitten.Domain.Watches` - 69 edges
5. `WardkittenApiClient` - 65 edges
6. `ApiResult` - 60 edges
7. `User` - 59 edges
8. `Incident` - 56 edges
9. `WelcomeTests` - 51 edges
10. `Result` - 46 edges

## Surprising Connections (you probably didn't know these)
- `ApiFactory` --references--> `Program`  [EXTRACTED]
  test/Wardkitten.IntegrationTests/OnboardingApiTests.cs → src/Wardkitten.Api/Program.cs
- `TestClock` --implements--> `IClock`  [EXTRACTED]
  test/Wardkitten.Tests/TestClock.cs → src/Wardkitten.Application/Abstractions/IClock.cs
- `Harness` --references--> `ISubscriptionRepository`  [EXTRACTED]
  test/Wardkitten.Tests/Application/AuthServiceTests.cs → src/Wardkitten.Application/Abstractions/Persistence/IBillingRepositories.cs
- `Harness` --references--> `IWalletRepository`  [EXTRACTED]
  test/Wardkitten.Tests/Application/AuthServiceTests.cs → src/Wardkitten.Application/Abstractions/Persistence/IBillingRepositories.cs
- `Harness` --references--> `IUserRepository`  [EXTRACTED]
  test/Wardkitten.Tests/Application/AuthServiceTests.cs → src/Wardkitten.Application/Abstractions/Persistence/IIdentityRepositories.cs

## Import Cycles
- None detected.

## Communities (168 total, 6 thin omitted)

### Community 0 - "CancellationToken"
Cohesion: 0.18
Nodes (7): CancellationToken, DateTime, IAsyncEnumerable, IReadOnlyList, Task, ICheckInRepository, IPingProbeRepository

### Community 1 - ".HandlePingAsync"
Cohesion: 0.22
Nodes (10): HttpRequest, IResult, CancellationToken, HttpContext, Task, PublicEndpoints, CheckInKind, Fail (+2 more)

### Community 2 - "Watch"
Cohesion: 0.07
Nodes (30): DateTime, List, Watch, BestStreak, ChannelBindings, ConsecutiveMisses, CurrentIncidentId, CurrentStreak (+22 more)

### Community 3 - "MongoContext"
Cohesion: 0.09
Nodes (23): IMongoDatabase, Lease, CancellationToken, IMongoCollection, Task, Team, MongoContext, ChannelRates (+15 more)

### Community 4 - "PingProbe"
Cohesion: 0.10
Nodes (19): DateTime, List, TimeSpan, PingProbe, ExpiresAtUtc, HitCount, Hits, LastHitAtUtc (+11 more)

### Community 5 - ".ReplaceAsync"
Cohesion: 0.16
Nodes (13): HttpContext, IEndpointRouteBuilder, AuthEndpoints, DateTime, AccessToken, ITokenService, CancellationToken, Task (+5 more)

### Community 6 - "StatusPage"
Cohesion: 0.17
Nodes (15): CancellationToken, IReadOnlyList, Task, IStatusPageRepository, List, StatusPage, IsPublic, Slug (+7 more)

### Community 7 - "OnCallSchedule"
Cohesion: 0.07
Nodes (29): CancellationToken, IReadOnlyList, Task, ITeamRepository, DateTime, List, OnCallOverride, EndUtc (+21 more)

### Community 8 - "HmacMagicLinkService"
Cohesion: 0.18
Nodes (8): IMagicLinkValidator, MagicLinkData, DateTime, DateTimeExtensions, HmacMagicLinkService, MagicLinkOptions, Secret, TtlMinutes

### Community 9 - "Instrucciones para agentes — Wardkitten"
Cohesion: 0.05
Nodes (37): Angular / TypeScript — StrykerJS (no aplica hoy), Autorización, Blazor — Stryker.NET sobre el code-behind, tests con bUnit, Bloqueos de MongoDB (CPU alta / COLLSCAN), C# — Stryker.NET, Concurrencia del worker, Despliegue Linux vs. desarrollo Windows, Documentación (+29 more)

### Community 10 - "WardkittenApiClient"
Cohesion: 0.14
Nodes (9): VerifyCodeRequest, WatchDto, ApiResult, HttpClient, HttpResponseMessage, JsonElement, Task, WardkittenApiClient (+1 more)

### Community 11 - "ChannelType"
Cohesion: 0.17
Nodes (13): CancellationToken, IReadOnlyList, Task, INotificationLogRepository, CancellationToken, ChannelBinding, DateTime, ILogger (+5 more)

### Community 12 - ".Build"
Cohesion: 0.35
Nodes (9): CancellationToken, DateTime, Fact, InlineData, Task, Theory, User, AuthServiceTests (+1 more)

### Community 13 - "PingProbeService"
Cohesion: 0.24
Nodes (9): IEndpointRouteBuilder, CancellationToken, DateTime, IReadOnlyList, Task, TimeSpan, PingActivity, PingProbeService (+1 more)

### Community 14 - "Wardkitten.Domain.Watches"
Cohesion: 0.13
Nodes (7): Wardkitten.Domain.Incidents, Wardkitten.Application.Notifications, Wardkitten.Domain.Notifications, Wardkitten.Domain.Watches, Wardkitten.Infrastructure.Notifications, Wardkitten.Tests.Domain, System.Net.Http.Json

### Community 15 - "NotificationLog"
Cohesion: 0.12
Nodes (18): DateTime, NotificationLog, Channel, CreditsCharged, Destination, Error, IncidentId, Kind (+10 more)

### Community 16 - "Wardkitten.Application.Abstractions.Persistence"
Cohesion: 0.19
Nodes (6): Wardkitten.Application.Abstractions, Wardkitten.Application.Common, Wardkitten.Infrastructure.Mongo.Repositories, Wardkitten.Application.Abstractions.Persistence, DateTime, TelegramLinkCode

### Community 17 - "NotificationResult"
Cohesion: 0.12
Nodes (20): NotificationResult, Error, ProviderMessageId, Success, CancellationToken, Task, CancellationToken, IHttpClientFactory (+12 more)

### Community 18 - "WatchEdit.razor"
Cohesion: 0.15
Nodes (12): route:/watches/{Id}/edit, route:/watches/new, ApplyBindings, BuildBindings, OnInitializedAsync, ChannelBinding, NavigationManager, PageTitle (+4 more)

### Community 19 - "Result"
Cohesion: 0.13
Nodes (18): IEndpointRouteBuilder, WatchEndpoints, Result, CancellationToken, Task, CancellationToken, IReadOnlyList, Task (+10 more)

### Community 20 - "TeamService"
Cohesion: 0.31
Nodes (8): IEndpointRouteBuilder, CancellationToken, DateTime, IReadOnlyList, List, Task, Team, TeamService

### Community 21 - "User"
Cohesion: 0.07
Nodes (28): DateTime, List, Plan, Roles, User, DefaultChannelBindings, DisplayName, Email (+20 more)

### Community 22 - "Incident"
Cohesion: 0.08
Nodes (25): CancellationToken, Task, NoopWatchEventPublisher, DateTime, List, Severity, Incident, AcknowledgedAtUtc (+17 more)

### Community 23 - "NotificationMessage"
Cohesion: 0.07
Nodes (31): ConcurrentQueue, IWebHostBuilder, CancellationToken, IEnumerable, Task, CancellationToken, IReadOnlyList, Severity (+23 more)

### Community 24 - "TwilioChannelBase"
Cohesion: 0.11
Nodes (17): CancellationToken, IHttpClientFactory, Task, TwilioChannelBase, Channel, From, TwilioOptions, AccountSid (+9 more)

### Community 25 - "WatchStatus"
Cohesion: 0.13
Nodes (16): Codable, Foundation, Identifiable, WatchStatus, WardkittenApi, Watch, WatchStatus, failing (+8 more)

### Community 26 - "Home.razor"
Cohesion: 0.10
Nodes (20): CriticalityBadge, route:/, CheckInAsync, DeleteAsync, DismissOnboardingAsync, Dispose, FormatDue, HandleChanged (+12 more)

### Community 27 - "SampleDocument"
Cohesion: 0.09
Nodes (21): Wardkitten.Tests.Infrastructure, IOptions, MongoSettings, SampleDocument, SampleState, MongoDbConfigurator, DateTime, Fact (+13 more)

### Community 28 - "Wardkitten.Application.RealTime"
Cohesion: 0.19
Nodes (8): Wardkitten.Application.DependencyInjection, Wardkitten.Worker, Wardkitten.IntegrationTests, Wardkitten.Api.Mcp, Wardkitten.Application.Evaluation, Wardkitten.Application.RealTime, Wardkitten.Infrastructure.DependencyInjection, Program

### Community 29 - "Severity"
Cohesion: 0.08
Nodes (39): IHttpContextAccessor, McpServerTool, CancellationToken, JsonSerializerOptions, Task, WardkittenMcpTools, Dictionary, CriticalityCatalog (+31 more)

### Community 30 - ".AddWardkittenIntegrations"
Cohesion: 0.11
Nodes (15): FirebaseApp, IPasswordHasher, IConfiguration, IServiceCollection, IntegrationsRegistration, PushChannel, Channel, PushOptions (+7 more)

### Community 31 - "IWalletRepository"
Cohesion: 0.15
Nodes (12): CancellationToken, IReadOnlyList, Task, ICreditTransactionRepository, IWalletRepository, CancellationToken, IReadOnlyList, Task (+4 more)

### Community 32 - "UserDto"
Cohesion: 0.18
Nodes (11): DateTime, IReadOnlyList, AuthResponse, LoginRequest, PhoneOtpRequest, PushTokenRequest, RefreshRequest, RegisterRequest (+3 more)

### Community 33 - "http"
Cohesion: 0.12
Nodes (17): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, inspectUri, launchBrowser, applicationUrl (+9 more)

### Community 34 - "Wardkitten.Domain.Identity"
Cohesion: 0.13
Nodes (6): Wardkitten.Domain.Identity, Wardkitten.Application.Billing, Wardkitten.Tests.Application, Wardkitten.Infrastructure.Security, Wardkitten.Infrastructure.Billing, Wardkitten.Application.Security

### Community 35 - ".ToDtoAsync"
Cohesion: 0.19
Nodes (14): CancellationToken, Task, TeamDto, TeamEndpoints, DateTime, List, AddMemberRequest, AddOnCallOverrideRequest (+6 more)

### Community 36 - "PingTestStateDto"
Cohesion: 0.28
Nodes (8): PingProbeMode, Draft, DryRun, DateTime, List, PingTestHitDto, PingTestStateDto, WatchTemplateDto

### Community 37 - "CreditTransaction"
Cohesion: 0.16
Nodes (14): CreditTransaction, AmountCredits, BalanceAfter, Channel, IdempotencyKey, ProviderReference, Reason, Type (+6 more)

### Community 38 - "Subscription"
Cohesion: 0.17
Nodes (13): ISubscriptionRepository, DateTime, Subscription, CancelAtPeriodEnd, CurrentPeriodEndUtc, GrantsPaidFeatures, Plan, Status (+5 more)

### Community 39 - "Welcome"
Cohesion: 0.04
Nodes (48): IDisposable, Dictionary, IReadOnlyList, NavigationManager, TimeSpan, WatchType, Welcome, Api (+40 more)

### Community 40 - "Wardkitten.Application.Services"
Cohesion: 0.22
Nodes (8): Wardkitten.Application.Services, Wardkitten.Api.Security, Wardkitten.Api.Mapping, Wardkitten.Api.Endpoints, Wardkitten.Shared.Contracts, Wardkitten.Api.RealTime, IEndpointRouteBuilder, InternalEndpoints

### Community 41 - "Wardkitten.Infrastructure"
Cohesion: 0.12
Nodes (16): Anthropic (12.39.0), BCrypt.Net-Next (4.2.0), Es.Nimita.Infra.Mongo (26.7.5), FirebaseAdmin (3.6.0), MailKit (4.17.0), Microsoft.Extensions.Configuration.Binder (10.0.10), Microsoft.Extensions.Hosting.Abstractions (10.0.10), Microsoft.Extensions.Options.ConfigurationExtensions (10.0.10) (+8 more)

### Community 42 - "Wardkitten.Shared.UI"
Cohesion: 0.20
Nodes (10): Microsoft.AspNetCore.Components.Authorization (10.0.10), Microsoft.AspNetCore.Components.Web (10.0.10), Microsoft.AspNetCore.Components.WebAssembly (10.0.10), Microsoft.AspNetCore.Components.WebAssembly.DevServer (10.0.10), Microsoft.AspNetCore.SignalR.Client (9.0.18), Microsoft.NET.Sdk.BlazorWebAssembly, Wardkitten.Shared.UI, Microsoft.Extensions.Http (10.0.10) (+2 more)

### Community 43 - "http"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 44 - "PlanLimits"
Cohesion: 0.13
Nodes (16): OnboardingState, Completed, Dictionary, Plan, Free, Pro, Team, PlanCatalog (+8 more)

### Community 45 - ".GetByIdAsync"
Cohesion: 0.19
Nodes (11): IConfiguration, IEndpointRouteBuilder, MoneyEndpoints, CancellationToken, IReadOnlyList, Task, IRepository, CancellationToken (+3 more)

### Community 46 - "Wallet"
Cohesion: 0.13
Nodes (13): Wallet, AutoTopUpAmountCredits, AutoTopUpEnabled, BalanceCredits, Currency, IsBelowThreshold, MinThresholdCredits, UserId (+5 more)

### Community 47 - "EmailOptions"
Cohesion: 0.15
Nodes (12): CancellationToken, Task, EmailChannel, Channel, EmailOptions, FromAddress, FromName, Host (+4 more)

### Community 48 - "SubscriptionStatus"
Cohesion: 0.33
Nodes (6): SubscriptionStatus, Active, Canceled, Incomplete, PastDue, Trialing

### Community 49 - "WelcomeTests"
Cohesion: 0.17
Nodes (10): WatchRequest, Fact, Func, IRenderedComponent, List, NavigationManager, Task, TimeSpan (+2 more)

### Community 50 - "CancellationToken"
Cohesion: 0.33
Nodes (5): CancellationToken, IReadOnlyList, Task, CreditTransactionRepository, WalletRepository

### Community 51 - "EvaluationWorker"
Cohesion: 0.24
Nodes (10): BackgroundService, PeriodicTimer, CancellationToken, IConfiguration, IHttpClientFactory, ILeaseStore, ILogger, Task (+2 more)

### Community 52 - "F04 — Motor de evaluación e incidentes"
Cohesion: 0.25
Nodes (7): Componentes, Descripción, F04 — Motor de evaluación e incidentes, Metadata, Modelo de datos, Reglas de negocio, Verificación

### Community 53 - "Wardkitten.Domain.CheckIns"
Cohesion: 0.08
Nodes (22): Wardkitten.Shared.UI.Services, Wardkitten.Domain.CheckIns, Wardkitten.Web.Tests, Wardkitten.Shared.UI.DependencyInjection, Wardkitten.Shared.UI.Components, Wardkitten.Shared.UI.Auth, Wardkitten.Web.Onboarding, Wardkitten.Web.Pages (+14 more)

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
Cohesion: 0.11
Nodes (17): ScheduleKind, Calendar, Cron, Interval, DateTime, List, TimeZoneInfo, Schedule (+9 more)

### Community 59 - "Tolerance"
Cohesion: 0.18
Nodes (8): TimeSpan, Tolerance, Grace, GraceSeconds, None, SkipTolerance, InlineData, Theory

### Community 60 - "TelegramChannel"
Cohesion: 0.17
Nodes (11): CancellationToken, IHttpClientFactory, Task, TelegramChannel, Channel, TelegramMessage, TelegramOptions, BotToken (+3 more)

### Community 61 - "ITokenStore"
Cohesion: 0.28
Nodes (5): Microsoft.JSInterop, IJSRuntime, ValueTask, ITokenStore, LocalStorageTokenStore

### Community 62 - "JwtAuthStateProvider"
Cohesion: 0.21
Nodes (8): AuthenticationState, AuthenticationStateProvider, Claim, Dictionary, IEnumerable, JsonElement, Task, JwtAuthStateProvider

### Community 63 - "Guía de publicación — Wardkitten (web + apps móviles)"
Cohesion: 0.11
Nodes (18): 0. Estado actual del proyecto, 1.1 Probar en local, 1.2 Publicar la imagen a GHCR (automático con CI), 1.3 Desplegar en Kubernetes, 1.4 Dominios y orígenes, 1.5 Verificación, 1. WEB (Blazor WASM) — la más sencilla, 2.1 Herramientas (+10 more)

### Community 64 - "ChannelBinding"
Cohesion: 0.06
Nodes (31): IPAddress, IEndpointRouteBuilder, TemplateEndpoints, ChannelBindingRules, ChannelDestinations, CancellationToken, Task, ChannelBinding (+23 more)

### Community 65 - ".Build"
Cohesion: 0.13
Nodes (19): Wardkitten.Infrastructure.Time, Wardkitten.Tests, DateTimeOffset, ClockTimeProvider, CancellationToken, DateTime, Fact, InlineData (+11 more)

### Community 66 - "App.razor"
Cohesion: 0.20
Nodes (9): AuthorizeRouteView, Authorizing, CascadingAuthenticationState, FocusOnNavigate, Found, RedirectToLogin, Router, Microsoft.AspNetCore.Components.Authorization (+1 more)

### Community 67 - "Wardkitten.IntegrationTests.csproj"
Cohesion: 0.17
Nodes (11): net10.0, EphemeralMongo (3.2.0), Microsoft.AspNetCore.Mvc.Testing (10.0.10), coverlet.collector (10.0.1), Microsoft.Extensions.Configuration (10.0.10), Microsoft.Extensions.DependencyInjection (10.0.10), Microsoft.NET.Test.Sdk (18.8.1), Shouldly (4.3.0) (+3 more)

### Community 68 - "Wallet.razor"
Cohesion: 0.18
Nodes (10): route:/wallet, LoadAsync, OnInitializedAsync, PortalAsync, CreditTransactionDto, NavigationManager, PageTitle, WardkittenApiClient (+2 more)

### Community 69 - "StripeWebhookProcessor"
Cohesion: 0.33
Nodes (6): Session, CancellationToken, ILogger, Subscription, Task, StripeWebhookProcessor

### Community 70 - ".ToDto"
Cohesion: 0.12
Nodes (9): DtoMappings, DateTime, CheckoutResponse, CreditTransactionDto, IncidentDto, SubscribeRequest, TopUpRequest, WalletDto (+1 more)

### Community 71 - ".Build"
Cohesion: 0.14
Nodes (20): Harness, TelegramLinkOptions, BotUsername, WebhookSecret, CancellationToken, DateTime, Fact, InlineData (+12 more)

### Community 72 - ".AddWardkittenClient"
Cohesion: 0.40
Nodes (4): AuthenticationStateProvider, IServiceCollection, Type, Uri

### Community 73 - "WatchStatus"
Cohesion: 0.22
Nodes (7): WardkittenApi, Watch, WatchStatus, Failing, Late, Ok, Paused

### Community 74 - "Wardkitten.Tests"
Cohesion: 0.20
Nodes (10): Wardkitten.Tests, coverlet.collector (10.0.1), Microsoft.Extensions.Configuration (10.0.10), Microsoft.Extensions.DependencyInjection (10.0.10), Microsoft.NET.Test.Sdk (18.8.1), NSubstitute (5.3.0), Shouldly (4.3.0), xunit (2.9.3) (+2 more)

### Community 75 - "StripePaymentGateway"
Cohesion: 0.42
Nodes (5): SessionCreateOptions, SessionLineItemOptions, CancellationToken, Task, StripePaymentGateway

### Community 76 - "IWatchRepository"
Cohesion: 0.21
Nodes (15): IIncidentRepository, IWatchRepository, IServiceCollection, ApplicationRegistration, INotificationDispatcher, IWatchEventPublisher, CheckInService, IncidentService (+7 more)

### Community 77 - "PingTestBenchTests"
Cohesion: 0.17
Nodes (5): CheckInDto, StartPingTestRequest, Fact, Task, PingTestBenchTests

### Community 78 - "PingTestBench"
Cohesion: 0.06
Nodes (31): ComponentBase, IJSRuntime, List, TimeSpan, ValueTask, PingTestBench, Active, Api (+23 more)

### Community 79 - "StripeOptions"
Cohesion: 0.18
Nodes (10): StripeOptions, AutomaticTaxEnabled, CreditCurrency, CreditTaxBehavior, CreditUnitAmountCents, PriceCredit, PriceProMonthly, PriceTeamMonthly (+2 more)

### Community 80 - "HttpWatchEventPublisher"
Cohesion: 0.47
Nodes (6): InternalEventRequest, CancellationToken, IHttpClientFactory, ILogger, Task, HttpWatchEventPublisher

### Community 81 - ".MapStatusPageEndpoints"
Cohesion: 0.22
Nodes (8): IEndpointRouteBuilder, StatusPageEndpoints, DateTime, List, PublicStatusPageDto, StatusItemDto, StatusPageDto, StatusPageRequest

### Community 82 - "MainLayout.razor"
Cohesion: 0.22
Nodes (8): Authorized, AuthorizeView, NavLink, LogoutAsync, ClientAuthService, LayoutComponentBase, NavigationManager, NotAuthorized

### Community 83 - "Política de seguridad — Wardkitten"
Cohesion: 0.22
Nodes (8): 1. Gestión de secretos, 2. Autenticación y autorización, 3. Endpoints públicos (sin sesión), 4. Wallet / canales metered (anti-abuso), 5. Datos y privacidad (GDPR), 6. Dependencias y supply chain, 7. Reporte de vulnerabilidades, Política de seguridad — Wardkitten

### Community 84 - ".AddWardkittenInfrastructure"
Cohesion: 0.13
Nodes (15): IMongoClient, IServiceProvider, IChannelRateRepository, ChannelRate, Channel, CountryPrefix, CreditsPerMessage, CancellationToken (+7 more)

### Community 85 - "IAckLinkBuilder"
Cohesion: 0.29
Nodes (4): DefaultAckLinkBuilder, IAckLinkBuilder, NotificationOptions, PublicBaseUrl

### Community 86 - ".Wizard_Profile_Channels_Test_FirstPingMonitor_AndComplete"
Cohesion: 0.12
Nodes (21): ApiFactory, Client, RecordingChannel, Program, List, ChannelTestRequest, ChannelTestResultDto, OnboardingStateDto (+13 more)

### Community 87 - "CancellationToken"
Cohesion: 0.21
Nodes (8): CancellationToken, DateTime, IAsyncEnumerable, IReadOnlyList, Task, IncidentRepository, PingProbeRepository, WatchRepository

### Community 88 - ".SendAsync"
Cohesion: 0.25
Nodes (6): DelegatingHandler, CancellationToken, HttpRequestMessage, HttpResponseMessage, Task, BearerHandler

### Community 89 - "Apps móviles nativas"
Cohesion: 0.13
Nodes (10): Android, Apps móviles nativas, Contratos, Estado, iOS + watchOS, Arquitectura, Cómo funciona, Desarrollo (+2 more)

### Community 90 - "F02.01 — Watch (tarea vigilada)"
Cohesion: 0.25
Nodes (8): Dependencias, Descripción, Elementos UI, Endpoints, F02.01 — Watch (tarea vigilada), Metadata, Modelo de datos (MongoDB, `watches`), Reglas de negocio

### Community 91 - "F03.03 — Banco de pruebas de la URL de ping (dry-run)"
Cohesion: 0.25
Nodes (8): Dependencias / Sub-features, Descripción, Elementos UI, Endpoints, F03.03 — Banco de pruebas de la URL de ping (dry-run), Metadata, Modelo de datos (MongoDB, `pingProbes`), Reglas de negocio

### Community 92 - "F06 — Wallet de créditos (canales metered)"
Cohesion: 0.22
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

### Community 99 - "Task"
Cohesion: 0.11
Nodes (6): CancellationToken, CancellationTokenSource, ChannelBinding, List, Task, WizardStep

### Community 100 - "FakeApi"
Cohesion: 0.13
Nodes (15): Body, HttpMessageHandler, HttpStatusCode, Key, CancellationToken, Dictionary, Func, HttpRequestMessage (+7 more)

### Community 101 - "Wardkitten.Worker"
Cohesion: 0.25
Nodes (7): DOTNET_ENVIRONMENT, profiles, Wardkitten.Worker, $schema, commandName, dotnetRunMessages, environmentVariables

### Community 102 - ".Build"
Cohesion: 0.24
Nodes (11): CancellationToken, DateTime, Dictionary, Fact, InlineData, List, Task, Theory (+3 more)

### Community 103 - "F14 — Endpoints MCP (Model Context Protocol)"
Cohesion: 0.29
Nodes (7): Cómo conectarse, Descripción, Endpoint, F14 — Endpoints MCP (Model Context Protocol), Herramientas expuestas (F14.01), Metadata, Reglas de negocio

### Community 104 - "Directivas de documentación — Wardkitten"
Cohesion: 0.29
Nodes (6): Directivas de documentación — Wardkitten, Estructura, Ficha de feature (`docs/features/FXX-nombre/overview.md`), Principios, Reglas para agentes, Sistema de numeración de features

### Community 105 - "SignalRWatchEventPublisher"
Cohesion: 0.25
Nodes (7): Hub, IHubContext, CancellationToken, Task, SignalRWatchEventPublisher, Task, WatchHub

### Community 106 - "StatusView.razor"
Cohesion: 0.29
Nodes (6): route:/status/{Slug}, CssFor, OnInitializedAsync, PageTitle, WardkittenApiClient, TextFor

### Community 107 - "IPaymentGateway"
Cohesion: 0.48
Nodes (4): CancellationToken, Plan, Task, IPaymentGateway

### Community 108 - "MaintenanceWindow"
Cohesion: 0.33
Nodes (5): DateTime, MaintenanceWindow, EndUtc, Reason, StartUtc

### Community 109 - "OnboardingHelpersTests"
Cohesion: 0.18
Nodes (8): IEnumerable, List, TimeSpan, TimeZoneInfo, TimeZoneOption, TimeZoneOptions, Fact, OnboardingHelpersTests

### Community 110 - "ADR · Apps móviles nativas en lugar de MAUI"
Cohesion: 0.33
Nodes (6): ADR · Apps móviles nativas en lugar de MAUI, Alternativa descartada, Consecuencias, Contexto, Decisión, El problema

### Community 111 - "Futura mejora — NSwag: operationIds estables"
Cohesion: 0.33
Nodes (5): Cuándo, Cómo ejecutarlo, El problema, Futura mejora — NSwag: operationIds estables, La idea

### Community 112 - "OnboardingPagesTests"
Cohesion: 0.23
Nodes (7): Home, OnboardingLayout, RenderFragment, Fact, IRenderedComponent, NavigationManager, OnboardingPagesTests

### Community 113 - "4. iOS y watchOS"
Cohesion: 0.33
Nodes (6): 4.1 Requisito previo, 4.2 Certificados, 4.3 Archivar y subir, 4.4 TestFlight, 4.5 Requisitos de ficha, 4. iOS y watchOS

### Community 114 - "TelegramLinkCodeDto"
Cohesion: 0.16
Nodes (10): BunitContext, DateTime, TelegramLinkCodeDto, TelegramStatusDto, Func, Task, WebTestBase, Api (+2 more)

### Community 115 - "Wardkitten.Application"
Cohesion: 0.18
Nodes (14): Es.Nimita.Domain.Primitives (26.7.5), Microsoft.Extensions.DependencyInjection.Abstractions (10.0.10), Microsoft.Extensions.Hosting (10.0.10), Microsoft.Extensions.Logging.Abstractions (10.0.10), Microsoft.Extensions.Options (10.0.10), NCrontab (3.4.0), Microsoft.NET.Sdk.Worker, Wardkitten.Application (+6 more)

### Community 116 - "EscalationPolicy"
Cohesion: 0.17
Nodes (14): IEscalationPolicyRepository, List, TimeSpan, EscalationPolicy, Name, Steps, UserId, EscalationStep (+6 more)

### Community 117 - "UserRepository"
Cohesion: 0.33
Nodes (5): CancellationToken, Task, CancellationToken, Task, UserRepository

### Community 118 - "IUserRepository"
Cohesion: 0.37
Nodes (9): IUserRepository, CancellationToken, DateTime, Fact, List, Task, User, Harness (+1 more)

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

### Community 125 - "TelegramLinkService"
Cohesion: 0.22
Nodes (7): SecureTokenGenerator, CancellationToken, IEnumerable, Task, TimeSpan, TelegramLinkService, IsConfigured

### Community 127 - ".CreateAsync"
Cohesion: 0.25
Nodes (8): GeneratedRegex, Regex, CancellationToken, IReadOnlyList, List, Task, StatusPageService, StatusPageView

### Community 128 - "Wardkitten.Domain.Billing"
Cohesion: 0.11
Nodes (7): Wardkitten.Domain.Teams, Wardkitten.Domain.StatusPages, Wardkitten.Domain.Common, Wardkitten.Domain.Billing, Wardkitten.Infrastructure.Mongo, CreditLots, CollectionNames

### Community 138 - "OnboardingService"
Cohesion: 0.33
Nodes (7): IEndpointRouteBuilder, OnboardingEndpoints, CancellationToken, ChannelBinding, IReadOnlyList, Task, OnboardingService

### Community 139 - "AlertDelivery"
Cohesion: 0.14
Nodes (14): AlertDelivery, Channel, CreditsCharged, Destination, Error, EscalationStep, ProviderMessageId, SentAtUtc (+6 more)

### Community 140 - "OnboardingProgress"
Cohesion: 0.19
Nodes (6): IReadOnlyList, Task, OnboardingProgress, Current, Furthest, Task

### Community 141 - "RefreshToken"
Cohesion: 0.22
Nodes (10): IRefreshTokenRepository, DateTime, RefreshToken, CreatedByIp, ExpiresAtUtc, ReplacedByTokenHash, RevokedAtUtc, TokenHash (+2 more)

### Community 142 - "CheckIn"
Cohesion: 0.17
Nodes (12): DateTime, CheckIn, DurationMs, Kind, Payload, ReceivedAtUtc, RemoteIp, Source (+4 more)

### Community 143 - "MongoRepository"
Cohesion: 0.33
Nodes (6): FilterDefinition, CancellationToken, IMongoCollection, IReadOnlyList, Task, MongoRepository

### Community 144 - "Task"
Cohesion: 0.29
Nodes (3): CancellationToken, CancellationTokenSource, Task

### Community 145 - ".Hace_IsHumanReadable"
Cohesion: 0.22
Nodes (4): DateTime, DateTime, InlineData, Theory

### Community 146 - "PingTestModeTests"
Cohesion: 0.38
Nodes (3): DateTime, Fact, PingTestModeTests

### Community 147 - ".ProbeDuringCreation_DoesNotCount_AndItsUrlBecomesTheRealOne"
Cohesion: 0.33
Nodes (6): IMongoRunner, ServiceProvider, PingRequest, Fact, Task, PingTestBenchTests

### Community 148 - "OnboardingLayout.razor"
Cohesion: 0.22
Nodes (8): CascadingValue, Dispose, OnInitialized, OnProgressChanged, LayoutComponentBase, NavigationManager, WardkittenApiClient, SkipAllAsync

### Community 149 - "F05.05 — Vinculación de Telegram por deep link"
Cohesion: 0.22
Nodes (9): Configuración (variables de entorno), Dependencias / Sub-features, Descripción, Elementos UI, Endpoints, F05.05 — Vinculación de Telegram por deep link, Metadata, Modelo de datos (MongoDB, `users`) (+1 more)

### Community 150 - "Wardkitten.Web.Tests"
Cohesion: 0.22
Nodes (9): bunit (2.11.3), Wardkitten.Web.Tests, coverlet.collector (10.0.1), Microsoft.NET.Test.Sdk (18.8.1), NSubstitute (5.3.0), Shouldly (4.3.0), xunit (2.9.3), xunit.runner.visualstudio (3.1.5) (+1 more)

### Community 151 - "F01.04 — Asistente de bienvenida (onboarding)"
Cohesion: 0.25
Nodes (8): Dependencias / Sub-features, Descripción, Elementos UI, Endpoints, F01.04 — Asistente de bienvenida (onboarding), Metadata, Modelo de datos (MongoDB, `users`), Reglas de negocio

### Community 152 - "IClock"
Cohesion: 0.32
Nodes (7): DateTime, IClock, UtcNow, SystemClock, UtcNow, IReadOnlyDictionary, ChannelTestService

### Community 153 - ".Load"
Cohesion: 0.54
Nodes (3): IReadOnlyList, IntervalPreset, IntervalPresets

### Community 154 - "F05.06 — Envío de prueba por un canal"
Cohesion: 0.29
Nodes (7): Dependencias, Descripción, Elementos UI, Endpoints, F05.06 — Envío de prueba por un canal, Metadata, Reglas de negocio

### Community 155 - "CheckInSource"
Cohesion: 0.29
Nodes (7): CheckInSource, App, Email, Http, Sms, System, Telegram

### Community 156 - ".Current"
Cohesion: 0.33
Nodes (3): BrowserLocale, InlineData, Theory

### Community 157 - "Welcome.razor"
Cohesion: 0.33
Nodes (5): ChannelTestButton, route:/welcome, PageTitle, PingTestBench, StepActions

### Community 159 - "CreditTransactionType"
Cohesion: 0.33
Nodes (6): CreditTransactionType, Adjustment, AutoTopUp, Consumption, Refund, TopUp

### Community 160 - "F02.05 — Canales por defecto del usuario"
Cohesion: 0.40
Nodes (5): Descripción, Endpoints, F02.05 — Canales por defecto del usuario, Metadata, Reglas de negocio (`ChannelBindingRules`)

### Community 161 - "TelegramUpdateOutcome"
Cohesion: 0.40
Nodes (5): TelegramUpdateOutcome, Ignored, InvalidCode, Linked, MissingCode

### Community 162 - "WizardStep"
Cohesion: 0.40
Nodes (5): WizardStep, Channels, Done, FirstWatch, Profile

### Community 163 - ".Main"
Cohesion: 0.50
Nodes (3): App, HeadOutlet, Task

### Community 164 - "PingResolution"
Cohesion: 0.50
Nodes (4): PingResolution, NotFound, Recorded, Test

## Knowledge Gaps
- **810 isolated node(s):** `Ok`, `Late`, `Failing`, `Paused`, `PackageDescription` (+805 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1115 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **6 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Welcome` connect `Welcome` to `ChannelBinding`, `WizardStep`, `Task`, `FakeApi`, `WardkittenApiClient`, `ChannelType`, `OnboardingProgress`, `OnboardingHelpersTests`, `PingTestBench`, `OnboardingPagesTests`, `WelcomeTests`, `TelegramLinkCodeDto`, `Wardkitten.Domain.CheckIns`, `.Wizard_Profile_Channels_Test_FirstPingMonitor_AndComplete`, `.Load`?**
  _High betweenness centrality (0.095) - this node is a cross-community bridge._
- **Why does `Wardkitten.Domain.Watches` connect `Wardkitten.Domain.Watches` to `Wardkitten.Domain.Billing`, `ChannelBinding`, `Wardkitten.Domain.Identity`, `Watch`, `PingTestStateDto`, `Wardkitten.Application.Services`, `MaintenanceWindow`, `Wardkitten.Application.Abstractions.Persistence`, `Wardkitten.Domain.CheckIns`, `.Wizard_Profile_Channels_Test_FirstPingMonitor_AndComplete`, `NotificationMessage`, `Schedule`, `Tolerance`, `Wardkitten.Application.RealTime`, `Severity`?**
  _High betweenness centrality (0.085) - this node is a cross-community bridge._
- **Why does `Watch` connect `Watch` to `CancellationToken`, `Wardkitten.Domain.Billing`, `MongoContext`, `ChannelType`, `PingProbeService`, `PingTestModeTests`, `Result`, `Incident`, `Severity`, `Wallet`, `.EvaluateWatchAsync`, `Schedule`, `Tolerance`, `ChannelBinding`, `.ToDto`, `IWatchRepository`, `HttpWatchEventPublisher`, `CancellationToken`, `SignalRWatchEventPublisher`, `MaintenanceWindow`, `IUserRepository`, `.CreateAsync`?**
  _High betweenness centrality (0.079) - this node is a cross-community bridge._
- **Are the 4 inferred relationships involving `Watch` (e.g. with `.FullWatchdogLoop_OnRealMongo()` and `.BreachedWatch_OpensIncidentAndAlertsOnce()`) actually correct?**
  _`Watch` has 4 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Ok`, `Late`, `Failing` to the rest of the system?**
  _810 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Watch` be split into smaller, more focused modules?**
  _Cohesion score 0.06765327695560254 - nodes in this community are weakly interconnected._
- **Should `MongoContext` be split into smaller, more focused modules?**
  _Cohesion score 0.09 - nodes in this community are weakly interconnected._