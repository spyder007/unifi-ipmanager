using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace unifi.ipmanager.Models
{
    public class UniResponse<TReturnType>
    {
        public UniMeta Meta { get; set; }

        public TReturnType Data { get; set; }
    }
}
