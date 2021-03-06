using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using GraZaDuzoZaMalo.Model;
using static GraZaDuzoZaMalo.Model.Gra.Odpowiedz;
using TimersTimer = System.Timers.Timer;
using System.Threading;


namespace AppGraZaDuzoZaMaloCLI
{
    public class KontrolerCLIZapisWLocie
    {
        public const char ZNAK_ZAKONCZENIA_GRY = 'X';

        private Gra gra;
        private ZapiszGre zapiszGre = new SerializajcaBitowa();
        private WidokCLI widok;

        public int MinZakres { get; private set; } = 1;
        public int MaxZakres { get; private set; } = 100;

        public IReadOnlyList<Gra.Ruch> ListaRuchow
        {
            get
            { return gra.ListaRuchow; }
        }

        public KontrolerCLIZapisWLocie()
        {
            gra = new Gra();
            widok = new WidokCLI(this);
        }

        public void Uruchom()
        {
            widok.OpisGry();
            while (widok.ChceszKontynuowac("Czy chcesz kontynuować aplikację (t/n)? "))
                UruchomRozgrywke();
        }

        public void UruchomRozgrywke()
        {
            widok.CzyscEkran();
            // ustaw zakres do losowania

            if (zapiszGre.zapisDostepny && widok.ChceszKontynuowac("Czy chcesz wczytać poprzednią rozgrywkę? (t/n)"))
            {
                try
                {
                    gra = zapiszGre.WczytajGre();
                    gra.Wznow();
                    zapiszGre.SerializacjaGry(gra);
                    widok.HistoriaGry();
                }
                catch (SerializationException)
                {
                    Console.WriteLine("Zapis gry uszkodzony!");
                    gra = new Gra(MinZakres, MaxZakres);
                    zapiszGre.UsunZapis();
                }
            }
            else
            {
                gra = new Gra(MinZakres, MaxZakres); //może zgłosić ArgumentException
                zapiszGre.UsunZapis();
            }



            //////////////////////////////////////////////////////////Zapis podczas trwania gry


            var timer = new TimersTimer(1000);
            timer.Elapsed += (s, e) => zapiszGre.SerializacjaGry(gra);
            timer.Start();
            do
            {
                //wczytaj propozycję
                int propozycja = 0;
                try
                {
                    propozycja = widok.WczytajPropozycje();
                }
                catch (KoniecGryException)
                {
                    gra.Zawieszona();
                    try
                    {
                        zapiszGre.SerializacjaGry(gra);
                        Console.WriteLine("Gra Zapisana!");
                    }
                    catch (SerializationException)
                    {
                        Console.WriteLine("Zapis gry uszkodzony!");
                    }
                    Environment.Exit(0);
                }

                Console.WriteLine(propozycja);

                if (gra.StatusGry == Gra.Status.Poddana) break;

                //Console.WriteLine( gra.Ocena(propozycja) );
                //oceń propozycję, break
                switch (gra.Ocena(propozycja))
                {
                    case ZaDuzo:
                        widok.KomunikatZaDuzo();
                        break;
                    case ZaMalo:
                        widok.KomunikatZaMalo();
                        break;
                    case Trafiony:
                        widok.KomunikatTrafiono();
                        break;
                    default:
                        break;
                }
                widok.HistoriaGry();
            }
            while (gra.StatusGry == Gra.Status.W_Trakcie);
            timer.Stop();

            //if StatusGry == Przerwana wypisz poprawną odpowiedź
            //if StatusGry == Zakończona wypisz statystyki gry

            if (gra.StatusGry == Gra.Status.Zakonczona)
            {
                zapiszGre.UsunZapis();
                Console.WriteLine("\nHistoria Gry\n");
                widok.HistoriaGry();
            }
        }

        ///////////////////////

        public void UstawZakresDoLosowania(ref int min, ref int max)
        {

        }

        public int LiczbaProb() => gra.ListaRuchow.Count();

        public void ZakonczGre()
        {
            //np. zapisuje stan gry na dysku w celu późniejszego załadowania
            //albo dopisuje wynik do Top Score
            //sprząta pamięć
            gra = null;
            widok.CzyscEkran(); //komunikat o końcu gry
            widok = null;
            Environment.Exit(0);
        }

        public void ZakonczRozgrywke()
        {
            gra.Przerwij();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <exception cref="KoniecGryException"></exception>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="OverflowException"></exception>
        /// <returns></returns>
        public int WczytajLiczbeLubKoniec(string value, int defaultValue)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            value = value.TrimStart().ToUpper();
            if (value.Length > 0 && value[0].Equals(ZNAK_ZAKONCZENIA_GRY))
                throw new KoniecGryException();

            //UWAGA: ponizej może zostać zgłoszony wyjątek 
            return Int32.Parse(value);
        }
    }

    [Serializable]
    internal class KoniecGryException : Exception
    {
        public KoniecGryException()
        {
        }

        public KoniecGryException(string message) : base(message)
        {
        }

        public KoniecGryException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected KoniecGryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}