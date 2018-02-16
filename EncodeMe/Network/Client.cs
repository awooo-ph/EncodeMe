using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Telephony;
using Android.Views;
using Android.Widget;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NetworkCommsDotNet.DPSBase;
using NetworkCommsDotNet.Tools;
using Environment = System.Environment;

namespace NORSU.EncodeMe.Network
{
    sealed class Client
    {
        private Client()
        {
        }

        ~Client()
        {
            Stop();
        }
        
        private static Client _instance;
        public static Client Instance => _instance ?? (_instance = new Client());
        private static bool _started;
      //  private static bool Emulator;
        
        
        
        public static void Start()
        {
            if (_started) return;
            _started = true;

            NetworkComms.EnableLogging(new LiteLogger(LiteLogger.LogMode.ConsoleOnly));

            NetworkComms.IgnoreUnknownPacketTypes = true;
            var serializer = DPSManager.GetDataSerializer<NetworkCommsDotNet.DPSBase.ProtobufSerializer>();

            NetworkComms.DefaultSendReceiveOptions = new SendReceiveOptions(serializer,
                NetworkComms.DefaultSendReceiveOptions.DataProcessors, NetworkComms.DefaultSendReceiveOptions.Options);

            NetworkComms.AppendGlobalIncomingPacketHandler<ServerInfo>(ServerInfo.GetHeader(), ServerInfoReceived);

            PeerDiscovery.EnableDiscoverable(PeerDiscovery.DiscoveryMethod.UDPBroadcast);

            PeerDiscovery.OnPeerDiscovered += OnPeerDiscovered;
            Connection.StartListening(ConnectionType.UDP, new IPEndPoint(IPAddress.Any,0));

            PeerDiscovery.DiscoverPeersAsync(PeerDiscovery.DiscoveryMethod.UDPBroadcast);
        }

        public static void Stop()
        {
            Connection.StopListening();
            NetworkComms.Shutdown();
        }

        private static ServerInfo _server;

        private static ServerInfo Server
        {
            get
            {
                return _server;
            }
            set
            {
                _server = value;
            }
        }

        private static void ServerInfoReceived(PacketHeader packetheader, Connection connection, ServerInfo incomingobject)
        {
            Server = incomingobject;
        }

        private static void OnPeerDiscovered(ShortGuid peeridentifier, Dictionary<ConnectionType, List<EndPoint>> endPoints)
        {
            var info = GetDeviceInfo();

            var eps = endPoints[ConnectionType.UDP];
            var localEPs = Connection.AllExistingLocalListenEndPoints();

            foreach (var value in eps)
            {
                var ip = value as IPEndPoint;
                if (ip?.AddressFamily != AddressFamily.InterNetwork) continue;

                foreach (var localEP in localEPs[ConnectionType.UDP])
                {
                    var lEp = (IPEndPoint)localEP;
                    if(lEp.AddressFamily!=AddressFamily.InterNetwork) continue;
                    if (!ip.Address.IsInSameSubnet(lEp.Address)) continue;
                    info.IP = lEp.Address.ToString();
                    info.Port =lEp.Port;
                    info.Send(ip);
                }
            }
        }

        private static AndroidInfo GetDeviceInfo()
        {
            var telephonyManager = (TelephonyManager) Application.Context.GetSystemService(Context.TelephonyService);
            var wifi = (WifiManager) Application.Context.GetSystemService(Context.WifiService);
            var info = new AndroidInfo();
            try
            {
                info.DeviceId = Build.Serial;
                info.Sim = telephonyManager.Line1Number;
                info.Model = Build.Brand;
            } catch (Exception ){}
            try
            {                
                info.MAC = wifi.ConnectionInfo.MacAddress;
                info.Hostname = Build.Model;
            } catch (Exception ){}
            
            return info;
        }

        private static async Task FindServer()
        {
            var start = DateTime.Now;
            PeerDiscovery.DiscoverPeersAsync(PeerDiscovery.DiscoveryMethod.UDPBroadcast);
            while ((DateTime.Now - start).TotalSeconds < 20)
            {
                if (Server != null) break;
                await Task.Delay(TimeSpan.FromSeconds(7));
            }
        }

        public static async Task<Courses> GetCourses()
        {
            return await Instance._GetCourses();
        }

        private Courses _courses;
        
        private async Task<Courses> _GetCourses()
        {
            if (_courses != null) return _courses;
            if (Server == null) await FindServer();
            if (Server == null) return null;
            
            var request = new GetCourses();

            Courses result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<Courses>(Courses.GetHeader(),
                (h, c, r) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(Courses.GetHeader());
                    result = r;
                    _courses = r;
                });
            
            await request.Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            NetworkComms.RemoveGlobalIncomingPacketHandler(Courses.GetHeader());
            return null;
        }

        public static async Task<StudentInfoResult> GetStudentInfo(string studentId,string password)
        {
            return await Instance._GetStudentInfo(studentId,password);
        }
        
        public static Student CurrentStudent { get; set; }
        public static string Receipt { get; set; }
        
        private async Task<StudentInfoResult> _GetStudentInfo(string studentId,string password)
        {
            if (Server == null) await FindServer();
            
            if (Server == null) return new StudentInfoResult(ResultCodes.Offline);
            
            var request = new StudentInfoRequest()
            {
                StudentId = studentId,
                Password = password
            };
            
            StudentInfoResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<StudentInfoResult>(StudentInfoResult.GetHeader(),
                (h, c, res) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(StudentInfoResult.GetHeader());
                    result = res;
                    if(result.Result == ResultCodes.Success)
                        CurrentStudent = res.Student;
                });
            
            await request.Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));
            
            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null) return result;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            NetworkComms.RemoveGlobalIncomingPacketHandler(StudentInfoResult.GetHeader());
            result = new StudentInfoResult(ResultCodes.Timeout);
            return result;
        }

        public static long TransactionId { get; set; }

        public static List<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
        public static bool EnrollmentCommited { get; set; }

        public static async Task<StartEnrollmentResult> StartEnrollment(string receipt)
        {
            return await Instance._StartEnrollment(receipt);
        }
        
        private async Task<StartEnrollmentResult> _StartEnrollment(string receipt)
        {
            if (CurrentStudent == null) return null;
            if (string.IsNullOrWhiteSpace(receipt)) return null;

            if(Server == null)
                await FindServer();

            if (Server == null)
                return null;

            StartEnrollmentResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<StartEnrollmentResult>(StartEnrollmentResult.GetHeader(),
                (h, c, i) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(StartEnrollmentResult.GetHeader());
                    ClassSchedules = i.ClassSchedules;
                    TransactionId = i.TransactionId;
                    EnrollmentCommited = i.Submitted;
                    result = i;
                });

            await new StartEnrollment()
            {
                Receipt = receipt,
                StudentId = CurrentStudent.StudentId
            }.Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null) return result;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            Server = null;
            NetworkComms.RemoveGlobalIncomingPacketHandler(StartEnrollmentResult.GetHeader());
            return null;
        }

        public static async Task<CommitEnrollmentResult> CommitEnrollment()
        {
            return await Instance._CommitEnrollment();
        }

        private async Task<CommitEnrollmentResult> _CommitEnrollment()
        {
            if (CurrentStudent == null)
                return null;

            if (TransactionId == 0) return null;

            if (Server == null)
                await FindServer();

            if (Server == null)
                return null;

            CommitEnrollmentResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<CommitEnrollmentResult>(CommitEnrollmentResult.GetHeader(),
                (h, c, i) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(CommitEnrollmentResult.GetHeader());
                    result = i;
                });

            await new CommitEnrollment()
            {
                ClassIds = ClassSchedules.Select(x=>x.ClassId).ToList(),
                StudentId = CurrentStudent.StudentId,
                TransactionId = TransactionId
            }.Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            Server = null;
            NetworkComms.RemoveGlobalIncomingPacketHandler(CommitEnrollmentResult.GetHeader());
            return null;
        }

        public static async Task<AddScheduleResult> AddSchedule(ClassSchedule schedule)
        {
            return await Instance._AddSchedule(schedule);
        }

        private async Task<AddScheduleResult> _AddSchedule(ClassSchedule schedule)
        {
            if (CurrentStudent == null)
                return null;
            
            if (Server == null)
                await FindServer();

            if (Server == null)
                return null;

            AddScheduleResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<AddScheduleResult>(AddScheduleResult.GetHeader(),
                (h, c, i) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(AddScheduleResult.GetHeader());
                    result = i;
                });

            await new AddSchedule()
            {
                ClassId = schedule.ClassId,
                StudentId = CurrentStudent.StudentId,
                TransactionId = TransactionId
            }.Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            Server = null;
            NetworkComms.RemoveGlobalIncomingPacketHandler(AddScheduleResult.GetHeader());
            return null;
        }

        public static async Task<ResultCodes> Register(Student student)
        {
            return await Instance._Register(student);
        }


        private async Task<ResultCodes> _Register(Student student)
        {
            if (Server == null) await FindServer();

            if (Server == null) return ResultCodes.Offline;

            RegisterStudentResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<RegisterStudentResult>(RegisterStudentResult.GetHeader(),
                async (h, c, i) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(RegisterStudentResult.GetHeader());
                    student.Id = i.StudentId;
                    await Db.Save(student);
                    result = i;
                });
            
            await new RegisterStudent(student).Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null) return result.Result;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            Server = null;
            NetworkComms.RemoveGlobalIncomingPacketHandler(RegisterStudentResult.GetHeader());
            return ResultCodes.Timeout;
        }

        public static async Task<SchedulesResult> GetSchedules(string subject)
        {
            return await Instance._GetSchedules(subject);
        }

        private static string _subjectRequested;
        
        private async Task<SchedulesResult> _GetSchedules(string subject)
        {
            if(string.IsNullOrWhiteSpace(_subjectRequested) && _subjectRequested!=subject)
                NetworkComms.RemoveGlobalIncomingPacketHandler($"{SchedulesResult.GetHeader()}{_subjectRequested}");

            if (string.IsNullOrWhiteSpace(subject)) return null;
            
            _subjectRequested = subject;
            
            if (Server == null) await FindServer();

            if (Server == null) return new SchedulesResult(){Result = ResultCodes.Offline};

            if (_subjectRequested != subject) return null;

            SchedulesResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<SchedulesResult>(SchedulesResult.GetHeader()+subject,
                (h, c, i) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(SchedulesResult.GetHeader()+subject);
                    result = i;
                });

            await new SchedulesRequest() {SubjectCode = subject}
                .Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null) return result;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            Server = null;
            NetworkComms.RemoveGlobalIncomingPacketHandler(SchedulesResult.GetHeader()+subject);
            return new SchedulesResult(){Result = ResultCodes.Timeout};
        }

        public static async Task<EnrollResult> Enroll(string studentId, List<ClassSchedule> schedules)
        {
            if (schedules == null || schedules.Count == 0) return null;
            
            if (Server == null) await FindServer();

            if (Server == null)
                return new EnrollResult(ResultCodes.Offline);
            
            var request = new EnrollRequest()
            {
                StudentId = studentId,
                ClassSchedules = schedules
            };

            EnrollResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<EnrollResult>(EnrollResult.GetHeader(),
                (h, c, i) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(EnrollResult.GetHeader());
                    result = i;
                });

            await request.Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));

         //   foreach (var sched in schedules)
          //  {
         //       sched.Sent = true;
          //      await Db.Save(sched);
         //   }
            

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null) return result;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            Server = null;
            NetworkComms.RemoveGlobalIncomingPacketHandler(EnrollResult.GetHeader());
            return new EnrollResult(ResultCodes.Timeout);
        }

        public async Task<StatusResult> GetStatus()
        {
            if (Server == null) await FindServer();

            if (Server == null) return new StatusResult() {Result = ResultCodes.Offline};
            
            var request = new StatusRequest();
            
            StatusResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<StatusResult>(StatusResult.GetHeader(),
                (h, c, i) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(StatusResult.GetHeader());
                    result = i;
                });

            await request.Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null) return result;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            Server = null;
            NetworkComms.RemoveGlobalIncomingPacketHandler(StatusResult.GetHeader());
            return new StatusResult() {Result = ResultCodes.Timeout};
        }
    }
    
}