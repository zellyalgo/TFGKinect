using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EjemploMovimientoSencilloMano
{
    class zona
    {
        float inicioX, finX, inicioY, finY;
        //es un entero para facilitar las cosas, la simbologia va segun las agujas del reloj:
        //1->arriba, 2->derecha, 3->abajo, 4->izquierda
        int zona;

        public zona(float inicioX, float finX, float inicioY, float finY, int zona)
        {
            this.inicioX = inicioX;
            this.inicioY = inicioY;
            this.finX = finX;
            this.finY = finY;
        }

        public Boolean isUnder(float posicionX, float posicionY)
        {
            return (inicioX < posicionX && posicionX < finX && inicioY < posicionY && posicionY < finY);
        }
    }
}
