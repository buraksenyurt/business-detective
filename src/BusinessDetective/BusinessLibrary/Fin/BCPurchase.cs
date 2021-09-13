#region Usings

using Entity;
using System;
using System.Collections.Generic;

#endregion

namespace BusinessLibrary.Fin
{
    public class BCPurchase
        : BCCommon
    {
        public int Find(Purchase purchase)
        {
            return -1;
        }

        public Purchase Create(Purchase purchase)
        {
            return null;
        }

        public bool IsAvailable(Purchase purchase)
        {
            return false;
        }

        public double Calculate(Purchase purchase, double rate)
        {
            return 0;
        }

        public void CreateAndSendReport(DateTime start, DateTime end, List<Person> peoples)
        {

        }

        public static string Ping()
        {
            return "Pong";
        }
    }
}
