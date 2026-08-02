import Foundation

/// Contrato mínimo con la API de Wardkitten.
///
/// Esqueleto: los DTO reales viven en `Wardkitten.Shared.Contracts` (.NET) y hay
/// que mantenerlos alineados a mano. Ver `docs/architecture/ADR-mobile-nativo.md`.
public struct Watch: Identifiable, Codable, Sendable {
    public let id: String
    public let name: String
    public let status: WatchStatus
    public let criticality: String

    public init(id: String, name: String, status: WatchStatus, criticality: String) {
        self.id = id
        self.name = name
        self.status = status
        self.criticality = criticality
    }
}

public enum WatchStatus: String, Codable, Sendable {
    case ok = "Ok"
    case late = "Late"
    case failing = "Failing"
    case paused = "Paused"
}

public protocol WardkittenApi: Sendable {
    func listWatches() async throws -> [Watch]
    func checkIn(watchID: String) async throws
}
