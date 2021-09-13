using Entity;
using System.ServiceModel;

namespace BusinessLibrary.Fin
{
    [ServiceContract]
    public interface IBCInvoice
    {
        [OperationContract]
        double CalculateInvoiceIncome(Invoice invoice);
    }
}