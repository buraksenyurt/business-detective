#region Namespaces
using Entity;
using System.Collections.Generic;
#endregion

namespace BusinessLibrary.Cus.Global
{
    public class BCGlobalCustomer : BCCommon,IBCGlobalCustomer
    {
        public List<Customer> CustomersByLocation(string city)
        {
            return null;
        }

        public List<Promotion> FindPromotions(Customer customer)
        {
            return null;
        }
    }
}
