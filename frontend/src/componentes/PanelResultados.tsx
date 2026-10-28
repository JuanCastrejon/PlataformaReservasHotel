import { CategoriaDisponibilidadDto, CotizacionEstadiaDto, ErrorApi, EstadoConsulta } from '../tipos'
import { TarjetaCotizacion } from './TarjetaCotizacion'

interface PropsPanelResultados {
  estado: EstadoConsulta
  resultados: CategoriaDisponibilidadDto[]
  error: ErrorApi | null
  cotizacionSeleccionada: CotizacionEstadiaDto | null
  cotizandoTipo: string | null
  onCotizar: (tipo: string, capacidadMaxima: number) => void
  onCerrarCotizacion: () => void
}

export function PanelResultados({
  estado,
  resultados,
  error,
  cotizacionSeleccionada,
  cotizandoTipo,
  onCotizar,
  onCerrarCotizacion,
}: PropsPanelResultados) {
  return (
    <section className="seccion-resultados" aria-labelledby="titulo-resultados">
      <div className="encabezado-seccion-resultados">
        <h2 id="titulo-resultados" className="subtitulo-seccion">
          Habitaciones y Tarifas
        </h2>
        {estado === 'exito' && (
          <span className="conteo-resultados">
            {resultados.length} {resultados.length === 1 ? 'categoría encontrada' : 'categorías encontradas'}
          </span>
        )}
      </div>

      {error && (
        <div className="alerta alerta-error-servidor" role="alert">
          <div className="icono-alerta" aria-hidden="true">❌</div>
          <div className="alerta-cuerpo">
            <h3 className="alerta-titulo">No se pudo completar la operación</h3>
            <p className="alerta-mensaje">{error.mensaje}</p>
            {error.codigo && (
              <span className="badge-codigo-error">Código: {error.codigo}</span>
            )}
          </div>
        </div>
      )}

      {estado === 'inactivo' && !error && (
        <div className="estado-caja estado-inactivo">
          <div className="icono-estado" aria-hidden="true">🏨</div>
          <h3>Listo para buscar</h3>
          <p>
            Complete el formulario superior indicando la sede y las fechas deseadas para consultar el inventario disponible y calcular las tarifas en tiempo real.
          </p>
        </div>
      )}

      {estado === 'cargando' && (
        <div className="estado-caja estado-cargando" aria-live="polite" aria-busy="true">
          <div className="indicador-spinner grande" aria-hidden="true" />
          <h3>Consultando disponibilidad en el servidor...</h3>
          <p>Verificando habitaciones libres y aplicando reglas de temporada del backend.</p>
        </div>
      )}

      {estado === 'vacio' && !error && (
        <div className="estado-caja estado-vacio" role="status">
          <div className="icono-estado" aria-hidden="true">📭</div>
          <h3>Sin disponibilidad para los criterios seleccionados</h3>
          <p>
            No hay habitaciones con suficiente capacidad o inventario libre en las fechas indicadas. Intente modificar las fechas de estadía o seleccionar otra sede.
          </p>
        </div>
      )}

      {estado === 'exito' && resultados.length > 0 && (
        <div className="contenedor-exito">
          {cotizacionSeleccionada && (
            <TarjetaCotizacion
              cotizacion={cotizacionSeleccionada}
              onCerrar={onCerrarCotizacion}
            />
          )}

          <div className="cuadricula-habitaciones">
            {resultados.map((item) => {
              const estaCotizando = cotizandoTipo === item.tipoHabitacion
              const estaSeleccionada =
                cotizacionSeleccionada?.tipoHabitacion === item.tipoHabitacion

              return (
                <article
                  key={`${item.sedeId}-${item.tipoHabitacion}`}
                  className={`tarjeta-habitacion ${estaSeleccionada ? 'seleccionada' : ''}`}
                  aria-labelledby={`tipo-${item.tipoHabitacion}`}
                >
                  <div className="tarjeta-habitacion-cuerpo">
                    <div className="tarjeta-habitacion-cabecera">
                      <span className="badge-tipo">{item.tipoHabitacion}</span>
                      <span className="badge-sede">{item.sede}</span>
                    </div>

                    <h3 id={`tipo-${item.tipoHabitacion}`} className="habitacion-titulo">
                      Habitación {item.tipoHabitacion}
                    </h3>

                    <div className="detalles-habitacion">
                      <div className="detalle-linea">
                        <span className="icono-detalle" aria-hidden="true">👥</span>
                        <span>Capacidad máxima: <strong>{item.capacidadMaxima} huéspedes</strong></span>
                      </div>
                      <div className="detalle-linea">
                        <span className="icono-detalle" aria-hidden="true">🔑</span>
                        <span>
                          Disponibles: <strong>{item.habitacionesDisponibles} habitaciones</strong>
                        </span>
                      </div>
                    </div>

                    <div className="habitacion-acciones">
                      <button
                        type="button"
                        onClick={() => onCotizar(item.tipoHabitacion, item.capacidadMaxima)}
                        className={`boton-cotizar ${estaSeleccionada ? 'activo' : ''}`}
                        disabled={estaCotizando}
                        aria-busy={estaCotizando}
                      >
                        {estaCotizando ? (
                          <>
                            <span className="indicador-spinner pequeno" aria-hidden="true" />
                            <span>Calculando tarifa...</span>
                          </>
                        ) : estaSeleccionada ? (
                          '✓ Cotización Mostrada'
                        ) : (
                          'Cotizar Estadía'
                        )}
                      </button>
                    </div>
                  </div>
                </article>
              )
            })}
          </div>
        </div>
      )}
    </section>
  )
}
