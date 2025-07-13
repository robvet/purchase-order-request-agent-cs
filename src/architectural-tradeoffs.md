/// <ArchitecturalDiscussion>
/// Singleton vs Scoped/Transient for Dependency Injection:
/// Singleton:
/// � One instance for the entire application lifetime.
/// � Use when the service is stateless or thread-safe.
/// � Tradeoff: Shared state across all users/requests. If you store per-user/session data, it will be shared (not safe).
/// Scoped:
/// � One instance per HTTP request.
/// � Use when you need to maintain state for a single request, but not across requests/users.
/// Transient:
/// � New instance every time it's requested.
/// � Use for lightweight, stateless services.
/// BOTTOM LINE:
/// � Consider using Singleton for stateless services that do not maintain user-specific state and are ThreadSafe.
/// � Use Scoped for services that need to maintain state for a single request or not ThreadSafe.
/// </ArchitecturalDiscussion>
