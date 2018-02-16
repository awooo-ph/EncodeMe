using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace NORSU.EncodeMe.Network
{
    [ProtoContract]
    class StartEnrollment : Message<StartEnrollment>
    {
        [ProtoMember(1)]
        public string Receipt { get; set; }
        [ProtoMember(2)]
        public string StudentId { get; set; }
    }

    [ProtoContract]
    class StartEnrollmentResult : Message<StartEnrollmentResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
        [ProtoMember(2)]
        public string ErrorMessage { get; set; }
        [ProtoMember(3)]
        public long TransactionId { get; set; }
        [ProtoMember(4)]
        public List<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
        [ProtoMember(5)]
        public bool Submitted { get; set; }
    }
}
