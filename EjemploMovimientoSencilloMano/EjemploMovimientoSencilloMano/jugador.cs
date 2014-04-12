using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using WindowsInput;
using System.Windows;

namespace EjemploMovimientoSencilloMano
{
    public class jugador
    {
        Skeleton skel;
        VirtualKeyCode pulsaArriba, pulsaAbajo, pulsaDerecha, pulsaIzquierda, pulsaAlante;
        int id;
        //Boleanos para controlar cual es el elemento que ha sido pulsado
        Boolean pulsadoArriba = false, pulsadoIzquierda = false, pulsadoDerecha = false, pulsadoAbajo = false, pulsadoAlante = false;

        //distancia calculada entre el hombro izquierdo y el "hombro central"(base del cuello), para determinar la distancia de accion
        float distanciaX;
        //distancia del eje Z utilizada para poder tener un margen en el cual recalcular la distanciaX.
        float ejeZDistacia;

        public jugador(Skeleton skel, int nJugador)
        {
            this.skel = skel;
            id = skel.TrackingId;
            if (nJugador == 1)
            {
                pulsaArriba = VirtualKeyCode.NUMPAD1;
                pulsaAbajo = VirtualKeyCode.NUMPAD4;
                pulsaDerecha = VirtualKeyCode.NUMPAD2;
                pulsaIzquierda = VirtualKeyCode.NUMPAD3;
            }
            else if (nJugador == 2)
            {
                pulsaArriba = VirtualKeyCode.VK_X;
                pulsaAbajo = VirtualKeyCode.VK_N;
                pulsaDerecha = VirtualKeyCode.VK_M;
                pulsaIzquierda = VirtualKeyCode.VK_Z;
            }
        }

        public Boolean isPlayer(int id)
        {
            return this.id == id;
        }

        public void moverMano()
        {
            float puntoCentralProfundidad = skel.Joints[JointType.ShoulderCenter].Position.Z;
            float manoIzquierdaX = skel.Joints[JointType.HandLeft].Position.X;
            //calculo la zona de accion del eje X para la zona izquierda.
            float inicioAccionX = (distanciaX * 3 - skel.Joints[JointType.ShoulderCenter].Position.X) * -1;
            float finAccionX = (distanciaX * 6 - skel.Joints[JointType.ShoulderCenter].Position.X) * -1;

            float manoIzquierdaY = skel.Joints[JointType.HandLeft].Position.Y;

            float manoDerechaX = skel.Joints[JointType.HandRight].Position.X;
            //calculo la zona de accion del ejeX para la zona derecha
            float inicioAccionDerechaX = (distanciaX * 3 + skel.Joints[JointType.ShoulderCenter].Position.X);
            float finAccionDerechaX = (distanciaX * 6 + skel.Joints[JointType.ShoulderCenter].Position.X);

            float manoDerechaY = skel.Joints[JointType.HandRight].Position.Y;

            Double manoDerechaZ = skel.Joints[JointType.HandRight].Position.Z;
            Double manoIzquierdaZ = skel.Joints[JointType.HandLeft].Position.Z;

            //calculo la zona de accion del ejeY para las zonas izquierda y derecha (como son simetricas vale para ambas.
            float inicioAccionY = skel.Joints[JointType.ShoulderCenter].Position.Y - distanciaX * 2;
            float finAccionY = skel.Joints[JointType.ShoulderCenter].Position.Y + distanciaX * 2;

            // fin de coger los datos para los lados derecha e izquierda

            //calculo la zona de accion del ejeX para la zona de arriba y la de abajo (me vale para ambas).
            float inicioAccionArribaX = (distanciaX * 2 - skel.Joints[JointType.ShoulderCenter].Position.X) * -1;
            float finAccionArribaX = (distanciaX * 2 + skel.Joints[JointType.ShoulderCenter].Position.X);
            //calculo la zona de accion del ejeY para la zona de arriba.
            float inicioAccionArribaY = (distanciaX + skel.Joints[JointType.Head].Position.Y);
            float finAccionArribaY = (distanciaX * 3 + skel.Joints[JointType.Head].Position.Y);
            //calculo la zona de accion del ejeY para la zona de abajo.
            float inicioAccionAbajoY = (distanciaX * 2 - skel.Joints[JointType.ShoulderCenter].Position.Y) * -1;
            float finAccionAbajoY = (distanciaX * 4 - skel.Joints[JointType.ShoulderCenter].Position.Y) * -1;

            //empieza el control para ver si esta dentro de la zona de accion.
            //zona de la izquierda.
            if ((manoIzquierdaX <= inicioAccionX && manoIzquierdaY >= inicioAccionY && manoIzquierdaX >= finAccionX && manoIzquierdaY <= finAccionY))
            {
                if (!pulsadoIzquierda)
                {
                    InputSimulator.SimulateKeyDown(pulsaIzquierda);
                    pulsadoIzquierda = true;
                }
                dc.DrawRectangle(brush, null, new Rect(puntoFinIzquierda.X, puntoFinIzquierda.Y, puntoInicioIzquierda.X - puntoFinIzquierda.X, puntoInicioIzquierda.Y - puntoFinIzquierda.Y));
            }
            else
            {
                InputSimulator.SimulateKeyUp(pulsaIzquierda);
                pulsadoIzquierda = false;
            }
            //zona de arriba
            if ((manoIzquierdaX >= inicioAccionArribaX && manoIzquierdaY >= inicioAccionArribaY && manoIzquierdaX <= finAccionArribaX && manoIzquierdaY <= finAccionArribaY) ||
                (manoDerechaX >= inicioAccionArribaX && manoDerechaY >= inicioAccionArribaY && manoDerechaX <= finAccionArribaX && manoDerechaY <= finAccionArribaY))
            {
                if (!pulsadoArriba)
                {
                    InputSimulator.SimulateKeyDown(pulsaArriba);
                    pulsadoArriba = true;

                }
                //PETA, HAY UE ARREGALRLO
                dc.DrawRectangle(brush, null, new Rect(puntoInicioArriba.X, puntoFinArriba.Y, puntoFinArriba.X - puntoInicioArriba.X, puntoInicioArriba.Y - puntoFinArriba.Y));
            }
            else
            {
                InputSimulator.SimulateKeyUp(pulsaArriba);
                pulsadoArriba = false;
            }
            //zona derecha.
            if (manoDerechaX >= inicioAccionDerechaX && manoDerechaY >= inicioAccionY && manoDerechaX <= finAccionDerechaX && manoDerechaY <= finAccionY)
            {
                if (!pulsadoDerecha)
                {
                    InputSimulator.SimulateKeyDown(pulsaDerecha);
                    pulsadoDerecha = true;
                }
                dc.DrawRectangle(brush, null, new Rect(puntoInicioDerecha.X, puntoFinIzquierda.Y, puntoFinDerecha.X - puntoInicioDerecha.X, puntoInicioIzquierda.Y - puntoFinIzquierda.Y));
            }
            else
            {
                InputSimulator.SimulateKeyUp(pulsaDerecha);
                pulsadoDerecha = false;
            }
            //zona de abajo
            if ((manoIzquierdaX >= inicioAccionArribaX && manoIzquierdaY <= inicioAccionAbajoY && manoIzquierdaX <= finAccionArribaX && manoIzquierdaY >= finAccionAbajoY) ||
                (manoDerechaX >= inicioAccionArribaX && manoDerechaY <= inicioAccionAbajoY && manoDerechaX <= finAccionArribaX && manoDerechaY >= finAccionAbajoY))
            {
                if (!pulsadoAbajo)
                {
                    InputSimulator.SimulateKeyDown(pulsaAbajo);
                    pulsadoAbajo = true;
                }
                if (puntoFinArriba.X - puntoInicioArriba.X > 0 && puntoFinAbajo.Y - puntoInicioAbajo.Y > 0)
                {
                    dc.DrawRectangle(brush, null, new Rect(puntoInicioArriba.X, puntoInicioAbajo.Y, puntoFinArriba.X - puntoInicioArriba.X, puntoFinAbajo.Y - puntoInicioAbajo.Y));
                }
            }
            else
            {
                InputSimulator.SimulateKeyUp(pulsaAbajo);
                pulsadoAbajo = false;
            }

            if (manoIzquierdaZ <= puntoCentralProfundidad - 0.5 || manoDerechaZ <= puntoCentralProfundidad - 0.5)
            {
                if (!pulsadoAlante)
                {
                    InputSimulator.SimulateKeyDown(pulsaAlante);
                    pulsadoAlante = true;
                }
            }
            else
            {
                InputSimulator.SimulateKeyUp(pulsaAlante);
                pulsadoAlante = false;
            }
        }

        //calcula la distancia entre el hombre izqueirdo y el centro, para poder luego seleccionar el area de accion.
        private void reescalar()
        {
            float ejeZ = skel.Joints[JointType.ShoulderCenter].Position.Z;
            if (ejeZ > ejeZDistacia + 0.5 || ejeZ < ejeZDistacia - 0.5)
            {
                distanciaX = skel.Joints[JointType.ShoulderCenter].Position.X - skel.Joints[JointType.ShoulderLeft].Position.X;
                ejeZDistacia = ejeZ;
                System.Console.Out.WriteLine("REEESCALANDO: " + ejeZ + " -> " + ejeZDistacia);
            }
        }
    }
}
