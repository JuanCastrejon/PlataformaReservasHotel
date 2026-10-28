import {
  CategoriaDisponibilidadDto,
  CotizacionEstadiaDto,
  ErrorApi,
  ParametrosBusqueda,
  ParametrosCotizacion,
} from '../tipos'

const RUTA_BASE = '/api/habitaciones'

async function procesarRespuesta<T>(respuesta: Response): Promise<T> {
  if (respuesta.ok) {
    return (await respuesta.json()) as T
  }

  let errorData: ErrorApi = { mensaje: `Error HTTP ${respuesta.status}` }
  try {
    const json = await respuesta.json()
    if (json && typeof json === 'object') {
      errorData = {
        codigo: json.codigo,
        mensaje: json.mensaje || errorData.mensaje,
      }
    }
  } catch {
    // Si la respuesta no es JSON válido, se conserva el mensaje genérico
  }

  throw errorData
}

export async function consultarDisponibilidad(
  parametros: ParametrosBusqueda,
  signal?: AbortSignal,
): Promise<CategoriaDisponibilidadDto[]> {
  const query = new URLSearchParams({
    sedeId: parametros.sedeId.toString(),
    fechaEntrada: parametros.fechaEntrada,
    fechaSalida: parametros.fechaSalida,
    numeroHuespedes: parametros.numeroHuespedes.toString(),
  })

  if (parametros.tipoHabitacion && parametros.tipoHabitacion.trim() !== '') {
    query.set('tipoHabitacion', parametros.tipoHabitacion)
  }

  try {
    const respuesta = await fetch(`${RUTA_BASE}/disponibles?${query.toString()}`, {
      method: 'GET',
      headers: {
        Accept: 'application/json',
      },
      signal,
    })

    return await procesarRespuesta<CategoriaDisponibilidadDto[]>(respuesta)
  } catch (error: unknown) {
    if ((error as { name?: string })?.name === 'AbortError') {
      throw error
    }

    if (error && typeof error === 'object' && 'mensaje' in error) {
      throw error as ErrorApi
    }

    throw {
      codigo: 'ERROR_CONEXION',
      mensaje:
        'No fue posible comunicarse con el servicio del backend. Verifique que la API esté en ejecución en http://localhost:5287.',
    } as ErrorApi
  }
}

export async function cotizarEstadia(
  parametros: ParametrosCotizacion,
  signal?: AbortSignal,
): Promise<CotizacionEstadiaDto> {
  const query = new URLSearchParams({
    sedeId: parametros.sedeId.toString(),
    fechaEntrada: parametros.fechaEntrada,
    fechaSalida: parametros.fechaSalida,
    numeroHuespedes: parametros.numeroHuespedes.toString(),
    cantidadHabitaciones: (parametros.cantidadHabitaciones ?? 1).toString(),
  })

  if (parametros.categoriaId) {
    query.set('categoriaId', parametros.categoriaId.toString())
  }

  if (parametros.tipoHabitacion && parametros.tipoHabitacion.trim() !== '') {
    query.set('tipoHabitacion', parametros.tipoHabitacion)
  }

  try {
    const respuesta = await fetch(`${RUTA_BASE}/cotizar?${query.toString()}`, {
      method: 'GET',
      headers: {
        Accept: 'application/json',
      },
      signal,
    })

    return await procesarRespuesta<CotizacionEstadiaDto>(respuesta)
  } catch (error: unknown) {
    if ((error as { name?: string })?.name === 'AbortError') {
      throw error
    }

    if (error && typeof error === 'object' && 'mensaje' in error) {
      throw error as ErrorApi
    }

    throw {
      codigo: 'ERROR_CONEXION',
      mensaje:
        'No fue posible obtener la cotización del servidor. Verifique la conexión con el backend.',
    } as ErrorApi
  }
}
