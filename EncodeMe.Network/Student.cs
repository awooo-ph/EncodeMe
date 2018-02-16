using System;
using System.ComponentModel;
using ProtoBuf;
#if __ANDROID__
using SQLite;
#endif
namespace NORSU.EncodeMe.Network
{
    [ProtoContract]
    class Student : Message<Student>, IDataErrorInfo
    {
        public Student()
        {
        }

        [ProtoMember(1)]
        public long Id { get; set; }
        
        private string _StudentId;
        [ProtoMember(2)]
#if __ANDROID__
        [PrimaryKey]
#endif
        public string StudentId
        {
            get => _StudentId;
            set
            {
                if (value == _StudentId) return;
                _StudentId = value;
                OnPropertyChanged(nameof(StudentId));
            }
        }

        private string _Name;
        [ProtoMember(3)]
        public string FirstName
        {
            get => _Name;
            set
            {
                if (value == _Name) return;
                _Name = value;
                OnPropertyChanged(nameof(FirstName));
            }
        }

        private string _LastName;
        [ProtoMember(4)]
        public string LastName
        {
            get => _LastName;
            set
            {
                if (value == _LastName) return;
                _LastName = value;
                OnPropertyChanged(nameof(LastName));
            }
        }
        
        private string _Course;
        [ProtoMember(5)]
        public string Course
        {
            get => _Course;
            set
            {
                if (value == _Course) return;
                _Course = value;
                OnPropertyChanged(nameof(Course));
            }
        }

        private byte[] _Picture;
        [ProtoMember(6)]
        public byte[] Picture
        {
            get => _Picture;
            set
            {
                if (value == _Picture) return;
                _Picture = value;
                OnPropertyChanged(nameof(Picture));
            }
        }

        private DateTime? _BirthDAte;
        [ProtoMember(7)]
        public DateTime? BirthDate
        {
            get => _BirthDAte;
            set
            {
                if (value == _BirthDAte)
                    return;
                _BirthDAte = value;
                OnPropertyChanged(nameof(BirthDate));
            }
        }

        private string _College;
        [ProtoMember(8)]
        public string College
        {
            get => _College;
            set
            {
                if (value == _College)
                    return;
                _College = value;
                OnPropertyChanged(nameof(College));
            }
        }

        private string _Major;
        [ProtoMember(9)]
        public string Major
        {
            get => _Major;
            set
            {
                if (value == _Major)
                    return;
                _Major = value;
                OnPropertyChanged(nameof(Major));
            }
        }

        private string _Minor;
        [ProtoMember(10)]
        public string Minor
        {
            get => _Minor;
            set
            {
                if (value == _Minor)
                    return;
                _Minor = value;
                OnPropertyChanged(nameof(Minor));
            }
        }

        private string _Status;
        [ProtoMember(11)]
        public string Status
        {
            get => _Status;
            set
            {
                if (value == _Status)
                    return;
                _Status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        private string _Scholarship;
        [ProtoMember(12)]
        public string Scholarship
        {
            get => _Scholarship;
            set
            {
                if (value == _Scholarship)
                    return;
                _Scholarship = value;
                OnPropertyChanged(nameof(Scholarship));
            }
        }

        private string _Address;
        [ProtoMember(13)]
        public string Address
        {
            get => _Address;
            set
            {
                if(value == _Address)
                    return;
                _Address = value;
                OnPropertyChanged(nameof(Address));
            }
        }

        private long _CourseId;
        [ProtoMember(14)]
        public long CourseId
        {
            get => _CourseId;
            set
            {
                if(value == _CourseId)
                    return;
                _CourseId = value;
                OnPropertyChanged(nameof(CourseId));
            }
        }

        private bool _Male;
        [ProtoMember(15)]
        public bool Male
        {
            get => _Male;
            set
            {
                if(value == _Male)
                    return;
                _Male = value;
                OnPropertyChanged(nameof(Male));
            }
        }


        public string this[string columnName] => GetErrorInfo(columnName);

        private string GetErrorInfo(string prop)
        {
            if (prop == nameof(BirthDate))
            {
                if (BirthDate == DateTime.MinValue) return "Required";
                if (BirthDate < DateTime.Now.AddYears(-174)) return "Too old";
                if (BirthDate > DateTime.Now.AddYears(-7)) return "Too young";
            }

            if (prop == nameof(StudentId) && string.IsNullOrWhiteSpace(StudentId)) return "Required";
            if (prop == nameof(FirstName) && string.IsNullOrWhiteSpace(FirstName))
                return "Required";
            if (prop == nameof(LastName) && string.IsNullOrWhiteSpace(LastName))
                return "Required";
            if (prop == nameof(CourseId) && CourseId==0) return "Required";
            if (prop == nameof(Address) && string.IsNullOrWhiteSpace(Address))
                return "Required";

            return null;
        }

        public string Error { get; } = null;

        public bool HasError
        {
            get
            {
                if (!string.IsNullOrEmpty(GetErrorInfo(nameof(BirthDate)))) return true;
                if (!string.IsNullOrEmpty(GetErrorInfo(nameof(StudentId))))
                    return true;
                if (!string.IsNullOrEmpty(GetErrorInfo(nameof(FirstName))))
                    return true;
                if (!string.IsNullOrEmpty(GetErrorInfo(nameof(LastName))))
                    return true;
                if (!string.IsNullOrEmpty(GetErrorInfo(nameof(Address))))
                    return true;
                return false;
            }
        }
    }

    [ProtoContract]
    class EnrollStudent : Message<EnrollStudent>
    {
        [ProtoMember(1)]
        public Student Student { get; set; }
    }

    [ProtoContract]
    class EnrollStudentResult : Message<EnrollStudentResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
        [ProtoMember(2)]
        public long Id { get; set; }
        [ProtoMember(3)]
        public string ErrorMessage { get; set; }
    }
}
