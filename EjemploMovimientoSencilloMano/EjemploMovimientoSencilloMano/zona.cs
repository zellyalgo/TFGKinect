using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EjemploMovimientoSencilloMano
{
    public class Zona
    {
        private float inicioX, finX, inicioY, finY, inicioZ, finZ;
        //es un entero para facilitar las cosas, la simbologia va segun las agujas del reloj:
        //1->arriba, 2->derecha, 3->abajo, 4->izquierda
        private int zona;

        public Zona(float inicioX, float finX, float inicioY, float finY, float inicioZ, float finZ, int zona)
        {
            setearZona(inicioX, finX, inicioY, finY, inicioZ, finZ);
            this.zona = zona;
        }

        public int getZona()
        {
            return zona;
        }

        public Boolean isUnder(float posicionX, float posicionY)
        {
            return (inicioX < posicionX && posicionX < finX && inicioY < posicionY && posicionY < finY);
        }

        public void setearZona(float inicioX, float finX, float inicioY, float finY, float inicioZ, float finZ)
        {
            this.inicioX = inicioX;
            this.inicioY = inicioY;
            this.inicioZ = inicioZ;
            this.finX = finX;
            this.finY = finY;
            this.finZ = finZ;
        }

        public float getInicioX()
        {
            return inicioX;
        }
        public float getInicioY()
        {
            return inicioY;
        }
        public float getInicioZ()
        {
            return inicioZ;
        }
        public float getFinX()
        {
            return finX;
        }
        public float getFinY()
        {
            return finY;
        }
        public float getFinZ()
        {
            return finZ;
        }
    }
}
