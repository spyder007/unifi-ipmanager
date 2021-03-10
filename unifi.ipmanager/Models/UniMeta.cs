using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace unifi.ipmanager.Models
{
    public class UniMeta
    {
        public const string ErrorResponse = "error";
        public const string SuccessResponse = "ok";


        public string Rc { get; set; }

        public string Msg { get; set; }

    }
}
