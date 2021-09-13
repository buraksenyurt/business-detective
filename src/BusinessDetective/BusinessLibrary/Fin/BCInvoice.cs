using Entity;

namespace BusinessLibrary.Fin
{
    public class BCInvoice : BCCommon,IBCInvoice
    {
        public double CalculateInvoiceIncome(Invoice invoice)
        {
            return Calculate(invoice);
        }

        private double Calculate(Invoice invoice)
        {
            return 0;
        }
    }
}
