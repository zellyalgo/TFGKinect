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

    public class Jugador
    {
        public event EventHandler<ZonaPulsadaArgs> ZonaPulsada;

        Skeleton skel;
        //Lista que contiene todas las zonas, se van a situar como las agujas del reloj para mas sencillez
        List<Zona> zonas = new List<Zona>();

        VirtualKeyCode pulsaArriba, pulsaAbajo, pulsaDerecha, pulsaIzquierda, pulsaAlante;
        //El modo consiste en que se le va a buscar el usuario (siendo 0 las manos, y uno los pies)
        int id, modo;
        //Boleanos para controlar cual es el elemento que ha sido pulsado
        Boolean pulsadoArriba = false, pulsadoIzquierda = false, pulsadoDerecha = false, pulsadoAbajo = false, pulsadoAlante = false;

        //distancia calculada entre el hombro izquierdo y el "hombro central"(base del cuello), para determinar la distancia de accion
        float distanciaX;
        //distancia del eje Z utilizada para poder tener un margen en el cual recalcular la distanciaX.
        float ejeZDistacia;

        float factorDistanciaZonaAD = Convert.ToSingle(0.75);
        float factorDistanciaZona = Convert.ToSingle(0.25);
        float dsitaciaRecalcular = Convert.ToSingle(0.5);
        float desviacionY = Convert.ToSingle(0.03);

        public Jugador(Skeleton skel, int nJugador, int modo)
        {
            this.skel = skel;
            id = skel.TrackingId;
            this.modo = modo;
            if (nJugador == 1)
            {
                pulsaArriba = VirtualKeyCode.NUMPAD1;
                pulsaAbajo = VirtualKeyCode.NUMPAD4;
                pulsaDerecha = VirtualKeyCode.NUMPAD2;
                pulsaIzquierda = VirtualKeyCode.NUMPAD3;
                pulsaAlante = VirtualKeyCode.SPACE;
            }
            else if (nJugador == 2)
            {
                pulsaArriba = VirtualKeyCode.VK_X;
                pulsaAbajo = VirtualKeyCode.VK_N;
                pulsaDerecha = VirtualKeyCode.VK_M;
                pulsaIzquierda = VirtualKeyCode.VK_Z;
                pulsaAlante = VirtualKeyCode.VK_B;
            }
        }

        public void mover(Skeleton skel)
        {
            if (modo == 0)
            {
                moverMano(skel);
            }
            else
            {
                moverPie(skel);
            }
        }

        public Boolean isPlayer(int id)
        {
            return this.id == id;
        }

        void OnZonePulse(Zona z)
        {
            if (ZonaPulsada != null)
            {
                ZonaPulsada(this, new ZonaPulsadaArgs(z, id));
            }
                
        }

        public Zona getZona(int nZona)
        {
            return zonas[nZona];
        }

        public void calcularZonas(Skeleton skel)
        {
            float puntoCentralProfundidad = skel.Joints[JointType.ShoulderCenter].Position.Z;
            //calculo la zona de accion del eje X para la zona izquierda.
            float inicioAccionX = (distanciaX * 3 - skel.Joints[JointType.ShoulderCenter].Position.X) * -1;
            float finAccionX = (distanciaX * 6 - skel.Joints[JointType.ShoulderCenter].Position.X) * -1;

            //calculo la zona de accion del ejeX para la zona derecha
            float inicioAccionDerechaX = (distanciaX * 3 + skel.Joints[JointType.ShoulderCenter].Position.X);
            float finAccionDerechaX = (distanciaX * 6 + skel.Joints[JointType.ShoulderCenter].Position.X);

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

            if (zonas.Count == 0)
            {
                Zona arriba = new Zona(inicioAccionArribaX, finAccionArribaX, inicioAccionArribaY, finAccionArribaY, puntoCentralProfundidad, puntoCentralProfundidad, 1);
                Zona derecha = new Zona(inicioAccionDerechaX, finAccionDerechaX, inicioAccionY, finAccionY, puntoCentralProfundidad, puntoCentralProfundidad, 2);
                Zona abajo = new Zona(inicioAccionArribaX, finAccionArribaX, finAccionAbajoY, inicioAccionAbajoY, puntoCentralProfundidad, puntoCentralProfundidad, 3);
                Zona izquierda = new Zona(finAccionX, inicioAccionX, inicioAccionY, finAccionY, puntoCentralProfundidad, puntoCentralProfundidad, 4);
                zonas.Add(arriba);
                zonas.Add(derecha);
                zonas.Add(abajo);
                zonas.Add(izquierda);
            }
            else
            {
                zonas[0].setearZona(inicioAccionArribaX, finAccionArribaX, inicioAccionArribaY, finAccionArribaY, puntoCentralProfundidad, puntoCentralProfundidad);
                zonas[1].setearZona(inicioAccionDerechaX, finAccionDerechaX, inicioAccionY, finAccionY, puntoCentralProfundidad, puntoCentralProfundidad);
                zonas[2].setearZona(inicioAccionArribaX, finAccionArribaX, finAccionAbajoY, inicioAccionAbajoY, puntoCentralProfundidad, puntoCentralProfundidad);
                zonas[3].setearZona(finAccionX, inicioAccionX, inicioAccionY, finAccionY, puntoCentralProfundidad, puntoCentralProfundidad);
            }
        }

        public void moverMano(Skeleton skel)
        {
            float puntoCentralProfundidad = skel.Joints[JointType.ShoulderCenter].Position.Z;

            float manoIzquierdaX = skel.Joints[JointType.HandLeft].Position.X;
            float manoIzquierdaY = skel.Joints[JointType.HandLeft].Position.Y;

            float manoDerechaX = skel.Joints[JointType.HandRight].Position.X;
            float manoDerechaY = skel.Joints[JointType.HandRight].Position.Y;

            Double manoDerechaZ = skel.Joints[JointType.HandRight].Position.Z;
            Double manoIzquierdaZ = skel.Joints[JointType.HandLeft].Position.Z;
            reescalar();
            calcularZonas(skel);
            

            //empieza el control para ver si esta dentro de la zona de accion.
            //zona de la izquierda.
            if ((zonas[3].isUnder(manoDerechaX, manoDerechaY) || zonas[3].isUnder(manoIzquierdaX, manoIzquierdaY)))
            {
                if (!pulsadoIzquierda)
                {
                    InputSimulator.SimulateKeyDown(pulsaIzquierda);
                    pulsadoIzquierda = true;
                }
                OnZonePulse(zonas[3]);
            }
            else
            {
                InputSimulator.SimulateKeyUp(pulsaIzquierda);
                pulsadoIzquierda = false;
            }

            //zona de arriba
            if (zonas[0].isUnder(manoDerechaX, manoDerechaY) || zonas[0].isUnder(manoIzquierdaX, manoIzquierdaY))
            {
                if (!pulsadoArriba)
                {
                    InputSimulator.SimulateKeyDown(pulsaArriba);
                    pulsadoArriba = true;

                }
                OnZonePulse(zonas[0]);
            }
            else
            {
                InputSimulator.SimulateKeyUp(pulsaArriba);
                pulsadoArriba = false;
            }

            //zona derecha.
            if (zonas[1].isUnder(manoDerechaX, manoDerechaY) || zonas[1].isUnder(manoIzquierdaX, manoIzquierdaY))
            {
                if (!pulsadoDerecha)
                {
                    InputSimulator.SimulateKeyDown(pulsaDerecha);
                    pulsadoDerecha = true;
                }
                OnZonePulse(zonas[1]);
            }
            else
            {
                InputSimulator.SimulateKeyUp(pulsaDerecha);
                pulsadoDerecha = false;
            }

            //zona de abajo
            if (zonas[2].isUnder(manoDerechaX, manoDerechaY) || zonas[2].isUnder(manoIzquierdaX, manoIzquierdaY))
            {
                if (!pulsadoAbajo)
                {
                    InputSimulator.SimulateKeyDown(pulsaAbajo);
                    pulsadoAbajo = true;
                }
                OnZonePulse(zonas[2]);
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

        private void moverPie(Skeleton skel)
        {
            
            float pieIzquierdoX = skel.Joints[JointType.FootLeft].Position.X;
            float pieDerechoX = skel.Joints[JointType.FootRight].Position.X;

            float pieIzquierdoZ = skel.Joints[JointType.FootLeft].Position.Z;
            float pieDerechoZ = skel.Joints[JointType.FootRight].Position.Z;

            float pieIzquierdoY = skel.Joints[JointType.FootLeft].Position.Y;
            float pieDerechoY = skel.Joints[JointType.FootRight].Position.Y;

            reescalar(skel);

            if (zonas[3].isUnder(pieDerechoX, pieDerechoY, pieDerechoZ) || zonas[3].isUnder(pieIzquierdoX, pieIzquierdoY, pieIzquierdoZ))
            {
                if (!pulsadoIzquierda)
                {

                    InputSimulator.SimulateKeyDown(pulsaIzquierda);
                    pulsadoIzquierda = true;
                }
                OnZonePulse(zonas[3]);
            }
            else
            {

                InputSimulator.SimulateKeyUp(pulsaIzquierda);
                pulsadoIzquierda = false;
            }
            //zona de alante
            if (zonas[0].isUnder(pieDerechoX, pieDerechoY, pieDerechoZ) || zonas[3].isUnder(pieIzquierdoX, pieIzquierdoY, pieIzquierdoZ))
            {
                if (!pulsadoArriba)
                {
                    InputSimulator.SimulateKeyDown(pulsaArriba);
                    pulsadoArriba = true;

                }
                OnZonePulse(zonas[0]);
            }
            else
            {
                InputSimulator.SimulateKeyUp(pulsaArriba);
                pulsadoArriba = false;
            }
            //zona derecha.
            if (zonas[1].isUnder(pieDerechoX, pieDerechoY, pieDerechoZ) || zonas[3].isUnder(pieIzquierdoX, pieIzquierdoY, pieIzquierdoZ))
            {
                if (!pulsadoDerecha)
                {
                    InputSimulator.SimulateKeyDown(pulsaDerecha);
                    pulsadoDerecha = true;
                }
                OnZonePulse(zonas[1]);
            }
            else
            {
                InputSimulator.SimulateKeyUp(pulsaDerecha);
                pulsadoDerecha = false;
            }
            //zona de abajo
            if (zonas[2].isUnder(pieDerechoX, pieDerechoY, pieDerechoZ) || zonas[3].isUnder(pieIzquierdoX, pieIzquierdoY, pieIzquierdoZ))
            {
                if (!pulsadoAbajo)
                {
                    InputSimulator.SimulateKeyDown(pulsaAbajo);
                    pulsadoAbajo = true;
                }
                OnZonePulse(zonas[2]);
            }
            else
            {
                InputSimulator.SimulateKeyUp(pulsaAbajo);                
                pulsadoAbajo = false;
            }
        }

        //calcula la distancia entre el hombre izqueirdo y el centro, para poder luego seleccionar el area de accion.
        public void reescalar(Skeleton skel)
        {
            float ejeZ = skel.Joints[JointType.ShoulderCenter].Position.Z;
            if (ejeZ > ejeZDistacia + dsitaciaRecalcular || ejeZ < ejeZDistacia - dsitaciaRecalcular)
            {
                distanciaX = skel.Joints[JointType.ShoulderCenter].Position.X - skel.Joints[JointType.ShoulderLeft].Position.X;
                ejeZDistacia = ejeZ;

                float finPieY = skel.Joints[JointType.FootLeft].Position.Y + desviacionY;
                float inicioPieY = skel.Joints[JointType.FootLeft].Position.Y - desviacionY;

                //zona izquierda
                float finAccionX = (distanciaX - skel.Joints[JointType.ShoulderCenter].Position.X) * -1;
                float inicioAccionX = (distanciaX * 4 - skel.Joints[JointType.ShoulderCenter].Position.X) * -1;
                //zona derecha
                float inicioAccionDerechaX = (distanciaX + skel.Joints[JointType.ShoulderCenter].Position.X);
                float finAccionDerechaX = (distanciaX * 4 + skel.Joints[JointType.ShoulderCenter].Position.X);
                //ejeZ para izquierda y derecha
                float finAccionIDZ = ejeZ + factorDistanciaZona;
                float inicioAccionIDZ = ejeZ - factorDistanciaZona;
                //alante y atras eje X
                float inicioAccionAlanteX = (distanciaX * 2 - skel.Joints[JointType.ShoulderCenter].Position.X) * -1;
                float finAccionAlanteX = (distanciaX * 2 + skel.Joints[JointType.ShoulderCenter].Position.X);
                //alante Z
                float finAccionAlanteZ = ejeZ - factorDistanciaZona;
                float inicioAccionAlanteZ = ejeZ - factorDistanciaZonaAD;
                //atras Z
                float inicioAccionAtrasZ = ejeZ + factorDistanciaZona;
                float finAccionAtrasZ = ejeZ + factorDistanciaZonaAD;

                if (zonas.Count == 0)
                {
                    Zona arriba = new Zona(inicioAccionAlanteX, finAccionAlanteX, inicioPieY, finPieY, inicioAccionAlanteZ, finAccionAlanteZ, 1);
                    Zona derecha = new Zona(inicioAccionDerechaX, finAccionDerechaX, inicioPieY, finPieY, inicioAccionIDZ, finAccionIDZ, 2);
                    Zona abajo = new Zona(inicioAccionAlanteX, finAccionAlanteX, inicioPieY, finPieY, inicioAccionAtrasZ, finAccionAtrasZ, 3);
                    Zona izquierda = new Zona(finAccionX, inicioAccionX, inicioPieY, finPieY, inicioAccionIDZ, finAccionIDZ, 4);
                    zonas.Add(arriba);
                    zonas.Add(derecha);
                    zonas.Add(abajo);
                    zonas.Add(izquierda);
                }
                else
                {
                    zonas[0].setearZona(inicioAccionAlanteX, finAccionAlanteX, inicioPieY, finPieY, inicioAccionAlanteZ, finAccionAlanteZ);
                    zonas[1].setearZona(inicioAccionDerechaX, finAccionDerechaX, inicioPieY, finPieY, inicioAccionIDZ, finAccionIDZ);
                    zonas[2].setearZona(inicioAccionAlanteX, finAccionAlanteX, inicioPieY, finPieY, inicioAccionAtrasZ, finAccionAtrasZ);
                    zonas[3].setearZona(finAccionX, inicioAccionX, inicioPieY, finPieY, inicioAccionIDZ, finAccionIDZ);
                }
                System.Console.Out.WriteLine("REEESCALANDO: " + ejeZ + " -> " + ejeZDistacia);
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
                //System.Console.Out.WriteLine("REEESCALANDO: " + ejeZ + " -> " + ejeZDistacia);
            }
        }
    }
}
