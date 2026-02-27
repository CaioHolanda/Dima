using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Requests.Categories
{
    public class GetCategoryByIdRequest:Request
    {
        public long Id { get; set; }
    }
}
