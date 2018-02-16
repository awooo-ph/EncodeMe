using System;
using System.Collections.Generic;
using System.Text;

namespace NORSU.EncodeMe.Network
{
    class Subject : Message<Subject>
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }
}
