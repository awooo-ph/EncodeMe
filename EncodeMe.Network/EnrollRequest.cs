#if !ENCODER
using System.Collections.Generic;
using ProtoBuf;

namespace NORSU.EncodeMe.Network
{
    [ProtoContract]
    class EnrollRequest : Message<EnrollRequest>
    {
        [ProtoMember(1)]
        public string StudentId { get; set; }

        [ProtoMember(2)]
        public List<ClassSchedule> ClassSchedules { get; set; }
        
        [ProtoMember(3)]
        public string ReceiptNumber { get; set; }
    }
}
#endif