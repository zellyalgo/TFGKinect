using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EjemploMovimientoSencilloMano
{
    class Zona
    {
        private float inicioX, finX, inicioY, finY;
        //es un entero para facilitar las cosas, la simbologia va segun las agujas del reloj:
        //1->arriba, 2->derecha, 3->abajo, 4->izquierda
        private int zona;

        public Zona(float inicioX, float finX, float inicioY, float finY, int zona)
        {
            setearZona(inicioX, finX, inicioY, finY);
            this.zona = zona;
        }

        public float getZona()
        {
            return zona;
        }

        public Boolean isUnder(float posicionX, float posicionY)
        {
            return (inicioX < posicionX && posicionX < finX && inicioY < posicionY && posicionY < finY);
        }

        public void setearZona(float inicioX, float finX, float inicioY, float finY)
        {
            this.inicioX = inicioX;
            this.inicioY = inicioY;
            this.finX = finX;
            this.finY = finY;
        }
    }
}
