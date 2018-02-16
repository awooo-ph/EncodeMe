using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace NORSU.EncodeMe.Network
{
    [ProtoContract]
    class AddSchedule : Message<AddSchedule>
    {
        [ProtoMember(1)]
        public long ClassId { get; set; }
        [ProtoMember(2)]
        public long TransactionId { get; set; }
        [ProtoMember(3)]
        public string StudentId { get; set; }
    }

    [ProtoContract]
    class AddScheduleResult : Message<AddScheduleResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
        [ProtoMember(2)]
        public string ErrorMessage { get; set; }
    }
}
