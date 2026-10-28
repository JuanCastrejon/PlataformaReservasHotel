export interface SedeHotel {
  id: number
  nombre: string
  ciudad: string
  tiposHabitacion: string[]
}

export type TipoHabitacion = 'Estándar' | 'Premium' | 'VIP'

export interface ParametrosBusqueda {
  sedeId: number
  fechaEntrada: string
  fechaSalida: string
  numeroHuespedes: number
  tipoHabitacion?: string
}

export interface CategoriaDisponibilidadDto {
  sedeId: number
  sede: string
  tipoHabitacion: string
  capacidadMaxima: number
  habitacionesDisponibles: number
}

export interface ParametrosCotizacion {
  sedeId: number
  categoriaId?: number
  tipoHabitacion?: string
  fechaEntrada: string
  fechaSalida: string
  numeroHuespedes: number
  cantidadHabitaciones?: number
}

export interface DesgloseHabitacionNocheDto {
  numeroHabitacion: number
  huespedes: number
  tarifaAplicada: number
}

export interface DesgloseNocheCotizacionDto {
  fecha: string
  temporada: string
  tarifaTotalNoche: number
  detalleHabitaciones: DesgloseHabitacionNocheDto[]
}

export interface DistribucionHabitacionCotizacionDto {
  numeroHabitacion: number
  huespedesAsignados: number
  subtotalHabitacion: number
}

export interface CotizacionEstadiaDto {
  sedeId: number
  sede: string
  sedeNombre: string
  categoriaId: number
  tipoHabitacion: string
  capacidadMaximaPorHabitacion: number
  fechaEntrada: string
  fechaSalida: string
  numeroNoches: number
  numeroHuespedes: number
  cantidadHabitaciones: number
  moneda: string
  totalEstadia: number
  habitaciones: DistribucionHabitacionCotizacionDto[]
  desgloseNoches: DesgloseNocheCotizacionDto[]
}

export type EstadoConsulta = 'inactivo' | 'cargando' | 'exito' | 'error' | 'vacio'

export interface ErrorApi {
  codigo?: string
  mensaje: string
}
