using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EjemploMovimientoSencilloMano
{
    public class ZonaPulsadaArgs
    {
        Zona zona;
        int idJugador;

        public ZonaPulsadaArgs(Zona zona, int idJugador)
        {
            this.zona = zona;
            this.idJugador = idJugador;
        }

        public Zona getZonaPulsada()
        {
            return zona;
        }

        public int getIdJugador()
        {
            return idJugador;
        }
    }
}
