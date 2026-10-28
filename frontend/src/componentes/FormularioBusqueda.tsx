import { useState, useId, useMemo, FormEvent } from 'react'
import { SEDES_HOTEL } from '../constantes/hoteles'
import { ParametrosBusqueda } from '../tipos'

interface PropsFormularioBusqueda {
  onBuscar: (parametros: ParametrosBusqueda) => void
  cargando: boolean
}

export function FormularioBusqueda({ onBuscar, cargando }: PropsFormularioBusqueda) {
  const hoy = useMemo(() => {
    const fecha = new Date()
    return fecha.toISOString().split('T')[0]
  }, [])

  const manana = useMemo(() => {
    const fecha = new Date()
    fecha.setDate(fecha.getDate() + 1)
    return fecha.toISOString().split('T')[0]
  }, [])

  const [sedeId, setSedeId] = useState<number>(SEDES_HOTEL[0].id)
  const [fechaEntrada, setFechaEntrada] = useState<string>(hoy)
  const [fechaSalida, setFechaSalida] = useState<string>(manana)
  const [numeroHuespedes, setNumeroHuespedes] = useState<number>(2)
  const [tipoHabitacion, setTipoHabitacion] = useState<string>('')
  const [errorValidacion, setErrorValidacion] = useState<string | null>(null)

  const idSede = useId()
  const idEntrada = useId()
  const idSalida = useId()
  const idHuespedes = useId()
  const idTipo = useId()

  const sedeSeleccionada = useMemo(
    () => SEDES_HOTEL.find((s) => s.id === sedeId) || SEDES_HOTEL[0],
    [sedeId],
  )

  const manejarEnvio = (e: FormEvent) => {
    e.preventDefault()
    setErrorValidacion(null)

    if (!fechaEntrada || !fechaSalida) {
      setErrorValidacion('Debe ingresar tanto la fecha de entrada como la de salida.')
      return
    }

    if (fechaEntrada >= fechaSalida) {
      setErrorValidacion('La fecha de entrada debe ser estrictamente anterior a la fecha de salida.')
      return
    }

    if (numeroHuespedes <= 0) {
      setErrorValidacion('El número de huéspedes debe ser mayor que cero.')
      return
    }

    onBuscar({
      sedeId,
      fechaEntrada,
      fechaSalida,
      numeroHuespedes,
      tipoHabitacion: tipoHabitacion.trim() !== '' ? tipoHabitacion : undefined,
    })
  }

  return (
    <section className="seccion-busqueda" aria-labelledby="titulo-busqueda">
      <div className="tarjeta-formulario">
        <div className="encabezado-formulario">
          <h2 id="titulo-busqueda" className="subtitulo-seccion">
            Buscar Habitaciones Disponibles
          </h2>
          <p className="texto-ayuda">
            Seleccione la sede de su preferencia, fechas de estadía y número de huéspedes.
          </p>
        </div>

        {errorValidacion && (
          <div className="alerta alerta-validacion" role="alert">
            <span className="icono-alerta" aria-hidden="true">⚠️</span>
            <span>{errorValidacion}</span>
          </div>
        )}

        <form onSubmit={manejarEnvio} className="formulario-cuadricula" noValidate>
          <div className="campo-grupo">
            <label htmlFor={idSede} className="etiqueta-campo">
              Sede Hotelera <span className="requerido">*</span>
            </label>
            <select
              id={idSede}
              className="control-entrada"
              value={sedeId}
              onChange={(e) => {
                setSedeId(Number(e.target.value))
                setTipoHabitacion('') // Reiniciar tipo si la nueva sede no lo tiene
              }}
              disabled={cargando}
              required
            >
              {SEDES_HOTEL.map((sede) => (
                <option key={sede.id} value={sede.id}>
                  {sede.ciudad} — {sede.nombre}
                </option>
              ))}
            </select>
          </div>

          <div className="campo-grupo">
            <label htmlFor={idEntrada} className="etiqueta-campo">
              Fecha de Entrada <span className="requerido">*</span>
            </label>
            <input
              type="date"
              id={idEntrada}
              className="control-entrada"
              value={fechaEntrada}
              min={hoy}
              onChange={(e) => {
                const nuevaEntrada = e.target.value
                setFechaEntrada(nuevaEntrada)
                if (nuevaEntrada && fechaSalida <= nuevaEntrada) {
                  const fecha = new Date(`${nuevaEntrada}T00:00:00`)
                  fecha.setDate(fecha.getDate() + 1)
                  setFechaSalida(fecha.toISOString().split('T')[0])
                }
              }}
              disabled={cargando}
              required
            />
          </div>

          <div className="campo-grupo">
            <label htmlFor={idSalida} className="etiqueta-campo">
              Fecha de Salida <span className="requerido">*</span>
            </label>
            <input
              type="date"
              id={idSalida}
              className="control-entrada"
              value={fechaSalida}
              min={fechaEntrada || hoy}
              onChange={(e) => setFechaSalida(e.target.value)}
              disabled={cargando}
              required
            />
          </div>

          <div className="campo-grupo">
            <label htmlFor={idHuespedes} className="etiqueta-campo">
              Huéspedes <span className="requerido">*</span>
            </label>
            <input
              type="number"
              id={idHuespedes}
              className="control-entrada"
              value={numeroHuespedes}
              min={1}
              max={8}
              onChange={(e) => setNumeroHuespedes(Number(e.target.value))}
              disabled={cargando}
              required
            />
          </div>

          <div className="campo-grupo">
            <label htmlFor={idTipo} className="etiqueta-campo">
              Tipo de Habitación (Opcional)
            </label>
            <select
              id={idTipo}
              className="control-entrada"
              value={tipoHabitacion}
              onChange={(e) => setTipoHabitacion(e.target.value)}
              disabled={cargando}
            >
              <option value="">Cualquier tipo disponible</option>
              {sedeSeleccionada.tiposHabitacion.map((tipo) => (
                <option key={tipo} value={tipo}>
                  {tipo}
                </option>
              ))}
            </select>
          </div>

          <div className="campo-acciones">
            <button
              type="submit"
              className="boton-primario"
              disabled={cargando}
              aria-busy={cargando}
            >
              {cargando ? (
                <>
                  <span className="indicador-spinner" aria-hidden="true" />
                  <span>Consultando disponibilidad...</span>
                </>
              ) : (
                '🔍 Buscar Disponibilidad'
              )}
            </button>
          </div>
        </form>
      </div>
    </section>
  )
}
