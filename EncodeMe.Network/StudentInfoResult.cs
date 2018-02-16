using System;
using ProtoBuf;

namespace NORSU.EncodeMe.Network
{
    
    [ProtoContract]
    class StudentInfoResult : Message<StudentInfoResult>
    {
        
        
        public StudentInfoResult() 
        {
            
        }

        public StudentInfoResult(ResultCodes result) : this()
        {
            switch (result)
            {
                case ResultCodes.Offline:
                    Message = "Not connected to server";
                    break;
                case ResultCodes.Timeout:
                    Message = "Request timeout";
                    break;
                case ResultCodes.NotFound:
                    Message = "Invalid Student Id";
                    break;                    
            }
            Result = result;
        }
        
        public StudentInfoResult(Student student) : this()
        {
            Student = student;
            Result = ResultCodes.Success;
        }

        [ProtoMember(1, IsRequired = true)]
        public ResultCodes Result { get; set; } = ResultCodes.Success;
        [ProtoMember(2)]
        public Student Student { get; set; }
        [ProtoMember(3)]
        public string Message { get; set; }
    }
}
