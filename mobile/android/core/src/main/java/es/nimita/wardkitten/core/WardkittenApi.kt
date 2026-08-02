package es.nimita.wardkitten.core

import kotlinx.serialization.Serializable

/**
 * Contrato mínimo con la API de Wardkitten.
 *
 * Esqueleto: los DTO reales viven en Wardkitten.Shared.Contracts (.NET) y hay que
 * mantenerlos alineados a mano. Ver docs/architecture/ADR-mobile-nativo.md.
 */
@Serializable
data class Watch(
    val id: String,
    val name: String,
    val status: WatchStatus,
    val criticality: String,
)

@Serializable
enum class WatchStatus { Ok, Late, Failing, Paused }

interface WardkittenApi {
    suspend fun listWatches(): List<Watch>
    suspend fun checkIn(watchId: String)
}
