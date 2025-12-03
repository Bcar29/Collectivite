using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Utils
{
    public class Convertir
    {
        public static string ConvertirNombreEnLettres(long nombre, bool ajouterDevise = true)
        {
            if (nombre == 0) return "zéro" + (ajouterDevise ? " franc guinéen" : "");

            string[] unites = { "", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf" };
            string[] dizaines = { "", "dix", "vingt", "trente", "quarante", "cinquante", "soixante", "soixante-dix", "quatre-vingt", "quatre-vingt-dix" };
            string[] speciales = { "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize", "dix-sept", "dix-huit", "dix-neuf" };

            string resultat = "";

            if (nombre >= 1000000000)
            {
                long milliards = nombre / 1000000000;
                resultat += ConvertirNombreEnLettres(milliards, false) + " milliard" + (milliards > 1 ? "s" : "") + " ";
                nombre %= 1000000000;
            }

            if (nombre >= 1000000)
            {
                long millions = nombre / 1000000;
                resultat += ConvertirNombreEnLettres(millions, false) + " million" + (millions > 1 ? "s" : "") + " ";
                nombre %= 1000000;
            }

            if (nombre >= 1000)
            {
                long milliers = nombre / 1000;
                if (milliers == 1)
                    resultat += "mille ";
                else
                    resultat += ConvertirNombreEnLettres(milliers, false) + " mille ";
                nombre %= 1000;
            }

            if (nombre >= 100)
            {
                long centaines = nombre / 100;
                if (centaines == 1)
                    resultat += "cent ";
                else
                    resultat += ConvertirNombreEnLettres(centaines, false) + " cent" + (nombre % 100 == 0 ? "s" : "") + " ";
                nombre %= 100;
            }

            if (nombre >= 20)
            {
                long diz = nombre / 10;
                long unit = nombre % 10;

                if (diz == 7 || diz == 9)
                {
                    resultat += dizaines[diz - 1];
                    resultat += "-" + ConvertirNombreEnLettres(10 + unit, false);
                }
                else if (diz == 8)
                {
                    resultat += "quatre-vingt";
                    if (unit == 0)
                        resultat += "s";
                    else
                        resultat += "-" + unites[unit];
                }
                else
                {
                    resultat += dizaines[diz];
                    if (unit == 1 && diz != 8)
                        resultat += "-et-un";
                    else if (unit > 0)
                        resultat += "-" + unites[unit];
                }
            }
            else if (nombre >= 10)
            {
                resultat += speciales[nombre - 10];
            }
            else if (nombre > 0)
            {
                resultat += unites[nombre];
            }

            return resultat.Trim() + (ajouterDevise ? " francs guinéens" : "");
        }

    }
}
