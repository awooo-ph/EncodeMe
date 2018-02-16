using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace NORSU.EncodeMe.Network
{
    [ProtoContract]
    class Course : Message<Course>
    {
        [ProtoMember(1)]
        public long Id { get; set; }
        
        [ProtoMember(2)]
        public string Name { get; set; }
        
        [ProtoMember(3)]
        public string Fullname { get; set; }
    }

    [ProtoContract]
    class GetCoursesDesktop : Message<GetCoursesDesktop>
    {
        
    }

    [ProtoContract]
    class GetCoursesResult : Message<GetCoursesResult>
    {
        [ProtoMember(1)]
        public List<Course> Courses { get; set; } = new List<Course>();
    }
}
