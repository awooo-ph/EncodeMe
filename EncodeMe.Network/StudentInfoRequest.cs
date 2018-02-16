using ProtoBuf;

namespace NORSU.EncodeMe.Network
{
    [ProtoContract]
    class StudentInfoRequest : Message<StudentInfoRequest>
    {   
        [ProtoMember(1)]
        public string StudentId { get; set; }
        [ProtoMember(2)]
        public string Password { get; set; }
    }
}
