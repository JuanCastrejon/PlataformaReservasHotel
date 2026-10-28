interface PropsEncabezado {
  titulo?: string
  subtitulo?: string
}

export function Encabezado({
  titulo = 'Plataforma de Reservas Hoteleras',
  subtitulo = 'Consulta de disponibilidad en tiempo real y cotización transparente por temporada',
}: PropsEncabezado) {
  return (
    <header className="encabezado-principal">
      <div className="encabezado-contenedor">
        <div className="encabezado-marca">
          <span className="insignia-sistema">Prueba Técnica AXD</span>
          <h1 className="titulo-plataforma">{titulo}</h1>
          <p className="subtitulo-plataforma">{subtitulo}</p>
        </div>
        <div className="encabezado-info">
          <span className="indicador-red">Red de Hoteles Colombia</span>
          <span className="badge-sedes">4 Sedes • 108 Habitaciones</span>
        </div>
      </div>
    </header>
  )
}
