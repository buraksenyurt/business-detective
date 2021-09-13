using Entity;
using System.Collections.Generic;
using System.ServiceModel;

namespace BusinessLibrary.Cus.Global
{
    [ServiceContract]
    public interface IBCGlobalCustomer
    {
        [OperationContract]
        List<Customer> CustomersByLocation(string city);
        [OperationContract]
        List<Promotion> FindPromotions(Customer customer);
    }
}