using ProtoBuf;

namespace NORSU.EncodeMe.Network
{
    [ProtoContract]
    class Encoder : Message<Encoder>
    {
        [ProtoMember(1)]
        public string Username { get; set; }

        [ProtoMember(2)]
        public string FullName { get; set; }

        [ProtoMember(3)]
        public byte[] Picture { get; set; }
        
        [ProtoMember(4)]
        public double Rate { get; set; }
        
        [ProtoMember(5)]
        public long WorkCount { get; set; }
    }
}
