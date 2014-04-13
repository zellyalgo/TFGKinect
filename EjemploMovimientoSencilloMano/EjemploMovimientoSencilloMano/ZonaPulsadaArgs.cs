using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EjemploMovimientoSencilloMano
{
    class ZonaPulsadaArgs : EventArgs
    {
        Zona zonaPulsada;
        int numeroZona;

        public ZonaPulsadaArgs(Zona zonaPulsada, int numeroZona)
        {
            this.zonaPulsada = zonaPulsada;
            this.numeroZona = numeroZona;
        }

        public Zona getZonaPulsada()
        {
            return zonaPulsada;
        }

        public int getNumeroZona()
        {
            return numeroZona;
        }
    }
}
