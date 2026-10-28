import { useState } from 'react'
import { Encabezado } from './componentes/Encabezado'
import { FormularioBusqueda } from './componentes/FormularioBusqueda'
import { PanelResultados } from './componentes/PanelResultados'
import { consultarDisponibilidad, cotizarEstadia } from './servicios/apiHotel'
import {
  CategoriaDisponibilidadDto,
  CotizacionEstadiaDto,
  ErrorApi,
  EstadoConsulta,
  ParametrosBusqueda,
} from './tipos'
import './Aplicacion.css'

export default function Aplicacion() {
  const [parametrosActuales, setParametrosActuales] = useState<ParametrosBusqueda | null>(null)
  const [estado, setEstado] = useState<EstadoConsulta>('inactivo')
  const [resultados, setResultados] = useState<CategoriaDisponibilidadDto[]>([])
  const [error, setError] = useState<ErrorApi | null>(null)
  const [cotizacion, setCotizacion] = useState<CotizacionEstadiaDto | null>(null)
  const [cotizandoTipo, setCotizandoTipo] = useState<string | null>(null)

  const manejarBusqueda = async (parametros: ParametrosBusqueda) => {
    setParametrosActuales(parametros)
    setEstado('cargando')
    setError(null)
    setCotizacion(null)

    try {
      const datos = await consultarDisponibilidad(parametros)
      setResultados(datos)
      if (datos.length === 0) {
        setEstado('vacio')
      } else {
        setEstado('exito')
      }
    } catch (err: unknown) {
      setError(
        (err as ErrorApi) || {
          mensaje: 'Ocurrió un error inesperado al consultar la disponibilidad.',
        },
      )
      setEstado('error')
    }
  }

  const manejarCotizacion = async (tipoHabitacion: string) => {
    if (!parametrosActuales) return

    setCotizandoTipo(tipoHabitacion)
    try {
      const resultadoCotizacion = await cotizarEstadia({
        sedeId: parametrosActuales.sedeId,
        tipoHabitacion,
        fechaEntrada: parametrosActuales.fechaEntrada,
        fechaSalida: parametrosActuales.fechaSalida,
        numeroHuespedes: parametrosActuales.numeroHuespedes,
        cantidadHabitaciones: 1,
      })
      setCotizacion(resultadoCotizacion)
    } catch (err: unknown) {
      setError(
        (err as ErrorApi) || {
          mensaje: 'No fue posible cotizar la categoría seleccionada.',
        },
      )
    } finally {
      setCotizandoTipo(null)
    }
  }

  return (
    <div className="layout-aplicacion">
      <Encabezado />

      <main className="contenedor-principal">
        <FormularioBusqueda
          onBuscar={manejarBusqueda}
          cargando={estado === 'cargando'}
        />

        <PanelResultados
          estado={estado}
          resultados={resultados}
          error={error}
          cotizacionSeleccionada={cotizacion}
          cotizandoTipo={cotizandoTipo}
          onCotizar={manejarCotizacion}
          onCerrarCotizacion={() => setCotizacion(null)}
        />
      </main>

      <footer className="pie-pagina">
        <div className="pie-contenedor">
          <p>Plataforma de Reservas Hoteleras — Prueba Técnica AXD (Senior Full Stack)</p>
          <p className="pie-creditos">
            Stack: React 19 + TypeScript + Vite | Backend: ASP.NET Core .NET 8 + EF Core SQLite
          </p>
        </div>
      </footer>
    </div>
  )
}
