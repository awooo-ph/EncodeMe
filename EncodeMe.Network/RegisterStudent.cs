using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace NORSU.EncodeMe.Network
{
    [ProtoContract]
    class RegisterStudent : Message<RegisterStudent>
    {
        public RegisterStudent(Student student) 
        {
            Student = student;
        }
        
        private RegisterStudent() { }

        [ProtoMember(1)]
        public Student Student { get; set; }
    }
}