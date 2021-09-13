using Entity;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace BusinessLibrary.Fin
{
    [ServiceContract]
    public interface IBCPurchase
    {
        [OperationContract]
        int Find(Purchase purchase);
        [OperationContract]
        Purchase Create(Purchase purchase);
        [OperationContract]
        bool IsAvailable(Purchase purchase);
        [OperationContract]
        double Calculate(Purchase purchase, double rate);
        [OperationContract]
        void CreateAndSendReport(DateTime start, DateTime end, List<Person> peoples);
    }
}