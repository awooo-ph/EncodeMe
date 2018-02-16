using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace NORSU.EncodeMe.Network
{
    [ProtoContract]
    class GetCourses : Message<GetCourses> 
    {
    }

    [ProtoContract]
    class Courses : Message<Courses>
    {
        [ProtoMember(1)]
        public List<Course> Items { get; set; } = new List<Course>();
    }
}
