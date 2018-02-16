using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace NORSU.EncodeMe.Network
{
    [ProtoContract]
    class CommitEnrollment : Message<CommitEnrollment>
    {
        [ProtoMember(1)]
        public List<long> ClassIds { get; set; }

        [ProtoMember(2)]
        public long TransactionId { get; set; }

        [ProtoMember(3)]
        public string StudentId { get; set; }
    }

    [ProtoContract]
    class CommitEnrollmentResult : Message<CommitEnrollmentResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
        [ProtoMember(2)]
        public string ErrorMessage { get; set; }
        [ProtoMember(3)]
        public long QueueNumber { get; set; }
    }
}
