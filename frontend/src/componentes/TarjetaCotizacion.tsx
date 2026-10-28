import { CotizacionEstadiaDto } from '../tipos'

interface PropsTarjetaCotizacion {
  cotizacion: CotizacionEstadiaDto
  onCerrar: () => void
}

export function TarjetaCotizacion({ cotizacion, onCerrar }: PropsTarjetaCotizacion) {
  const formatearDinero = (monto: number) => {
    return new Intl.NumberFormat('es-CO', {
      style: 'currency',
      currency: cotizacion.moneda || 'COP',
      maximumFractionDigits: 0,
    }).format(monto)
  }

  return (
    <article className="tarjeta-cotizacion-detalle" aria-labelledby="titulo-cotizacion">
      <div className="cotizacion-encabezado">
        <div>
          <span className="badge-tipo-grande">{cotizacion.tipoHabitacion}</span>
          <h3 id="titulo-cotizacion" className="cotizacion-titulo">
            Resumen de Cotización — {cotizacion.sede}
          </h3>
          <p className="cotizacion-subtitulo">
            {cotizacion.sedeNombre} • {cotizacion.numeroNoches}{' '}
            {cotizacion.numeroNoches === 1 ? 'noche' : 'noches'} ({cotizacion.fechaEntrada} al{' '}
            {cotizacion.fechaSalida})
          </p>
        </div>
        <button
          type="button"
          onClick={onCerrar}
          className="boton-cerrar"
          aria-label="Cerrar desglose de cotización"
        >
          ✕
        </button>
      </div>

      <div className="cotizacion-metricas">
        <div className="metrica-item">
          <span className="metrica-etiqueta">Huéspedes</span>
          <span className="metrica-valor">{cotizacion.numeroHuespedes} pers.</span>
        </div>
        <div className="metrica-item">
          <span className="metrica-etiqueta">Habitaciones</span>
          <span className="metrica-valor">{cotizacion.cantidadHabitaciones}</span>
        </div>
        <div className="metrica-item">
          <span className="metrica-etiqueta">Capacidad por hab.</span>
          <span className="metrica-valor">Máx. {cotizacion.capacidadMaximaPorHabitacion}</span>
        </div>
        <div className="metrica-item metrica-total">
          <span className="metrica-etiqueta">Total Estancia</span>
          <span className="metrica-valor-destacado">
            {formatearDinero(cotizacion.totalEstadia)}
          </span>
        </div>
      </div>

      {cotizacion.desgloseNoches && cotizacion.desgloseNoches.length > 0 && (
        <div className="cotizacion-desglose">
          <h4 className="subtitulo-desglose">Desglose Noche a Noche</h4>
          <div className="tabla-desglose-contenedor">
            <table className="tabla-desglose">
              <thead>
                <tr>
                  <th scope="col">Fecha</th>
                  <th scope="col">Temporada</th>
                  <th scope="col">Tarifa Noche</th>
                </tr>
              </thead>
              <tbody>
                {cotizacion.desgloseNoches.map((noche) => (
                  <tr key={noche.fecha}>
                    <td>{noche.fecha}</td>
                    <td>
                      <span
                        className={`badge-temporada ${
                          noche.temporada.includes('Alta')
                            ? 'temporada-alta'
                            : 'temporada-baja'
                        }`}
                      >
                        {noche.temporada}
                      </span>
                    </td>
                    <td className="monto-tabla">{formatearDinero(noche.tarifaTotalNoche)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {cotizacion.habitaciones && cotizacion.habitaciones.length > 1 && (
        <div className="cotizacion-distribucion">
          <h4 className="subtitulo-desglose">Distribución por Habitación</h4>
          <ul className="lista-distribucion">
            {cotizacion.habitaciones.map((h) => (
              <li key={h.numeroHabitacion} className="item-distribucion">
                <span>Habitación #{h.numeroHabitacion}</span>
                <span>{h.huespedesAsignados} huéspedes</span>
                <span className="monto-subtotal">{formatearDinero(h.subtotalHabitacion)}</span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </article>
  )
}
