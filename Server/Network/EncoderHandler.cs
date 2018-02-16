using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NORSU.EncodeMe.Models;
using NORSU.EncodeMe.Properties;

namespace NORSU.EncodeMe.Network
{
    static partial class Server
    {
        public static async void LoginHandler(PacketHeader packetheader, Connection connection, Login login)
        {
            var ip = ((IPEndPoint)connection.ConnectionInfo.RemoteEndPoint).Address.ToString();
            var client = Client.Cache.FirstOrDefault(x => x.IP == ip);
            if (!(client?.IsEnabled ?? false))
            {
                Activity.Log(
                    Activity.Categories.Network,
                    Activity.Types.Warning,
                    $"Login attempted at an unauthorized terminal ({ip}).");
                return;
            }

            client.IsOnline = true;
            if ((DateTime.Now - client.LastHeartBeat).TotalSeconds > Settings.Default.LoginAttemptTimeout)
                client.LoginAttempts = 0;
            
            client.Update("LastHeartBeat", DateTime.Now);
            client.LoginAttempts++;

            if (client.LoginAttempts > Settings.Default.MaxLoginAttempts)
            {
                TerminalLog.Add(client.Id, "Login is disabled. Maximum attempts reached.", TerminalLog.Types.Warning);
                await new LoginResult(ResultCodes.Error, "Too many failed attempts").Send((IPEndPoint)connection.ConnectionInfo.RemoteEndPoint);
                return;
            }
            
            var encoder = Models.Encoder.Cache.FirstOrDefault(
                x => string.Equals(x.Username.Trim(), login.Username, StringComparison.CurrentCultureIgnoreCase) &&
                     !string.IsNullOrWhiteSpace(x.Username));

            if (encoder != null && !string.IsNullOrEmpty(login.Password) &&
                (encoder.Password == login.Password || string.IsNullOrEmpty(encoder.Password)))
            {
                encoder.Update(nameof(encoder.Password), login.Password);
                TerminalLog.Add(client.Id, $"{encoder.Username} has logged in.");
                //Logout previous session if any.
                var cl = Client.Cache.FirstOrDefault(x => x.Encoder?.Id == encoder.Id);
                if (cl != null)
                {
                    await new Logout() {Reason = $"You were logged in at another terminal ({cl.Hostname})."}
                        .Send(new IPEndPoint(IPAddress.Parse(cl.IP), cl.Port));
                    cl.Encoder = null;
                }
                

                client.Encoder = encoder;
                await new LoginResult(new Encoder()
                {
                    Username = encoder.Username,
                    FullName = encoder.FullName,
                    Picture = encoder.Thumbnail,
                    WorkCount = Request.Cache.Count(x=>x.Status > Request.Statuses.Proccessing && x.EncoderId == encoder.Id),
                    Rate = encoder.WorkRate,
                }).Send((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint);
                client.LoginAttempts = 0;
            }
            else
            {
                TerminalLog.Add(client.Id, $"Login attempt failed. Username: {login.Username}");
                await new LoginResult(ResultCodes.Error, "Invalid username/password").Send((IPEndPoint)connection.ConnectionInfo.RemoteEndPoint);
            }
         //   List<Client> clients;
         //   lock (_clients)
         //       clients = _clients.ToList();
            await SendEncoderUpdates(Client.Cache.ToList());
        }

        private static object _clientsLock = new object();
        private static Queue<Task> _handshakeTasks = new Queue<Task>();
        private static bool _handshakeTasksRunning;
       // private static List<Client> _clients = new List<Client>();
        private static Task Pinger;

        private static async void StartPinger(Client client)
        {
            while (client.IsOnline)
            {
                await TaskEx.Delay(5555);
                client.IsOnline = false;
                await new Ping().Send(new IPEndPoint(IPAddress.Parse(client.IP), client.Port));
                await TaskEx.Delay(1111);
                if (!client.IsOnline)
                    client.Encoder = null;
            }
        }

        public static async void HandShakeHandler(PacketHeader packetheader, Connection connection, EndPointInfo ep)
        {
            
            lock(_clientsLock)
            _handshakeTasks.Enqueue(new Task(async () =>
            {
                //Get known client or create new one.
              //  Client client = null;
              //  lock (_clients)
              //  {
                 var   client = Client.Cache.FirstOrDefault(x => x.IP == ep.IP);
                    if (client == null)
                    {
                        client = new Client()
                        {
                            IP = ep.IP
                        };
                    }
               // }
                client.Hostname = ep.Hostname;
                client.Port = ep.Port;
                client.LastHeartBeat = DateTime.Now;
                var isNew = client.Id == 0;
                await client.SaveAsync();
                client.IsOnline = true;
                
                StartPinger(client);
                
                if (isNew)
                    TerminalLog.Add(client.Id, "Encoder terminal added.");

                TerminalLog.Add(client.Id, "Terminal has connected.");
            
                var localEPs = Connection.AllExistingLocalListenEndPoints();
                var serverInfo = new ServerInfo(Environment.MachineName);
                var ip = new IPEndPoint(IPAddress.Parse(ep.IP), ep.Port);

                foreach (var localEP in localEPs[ConnectionType.UDP])
                {
                    var lEp = localEP as IPEndPoint;
                
                    if (lEp == null) continue;
                    if(lEp.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (!ip.Address.IsInSameSubnet(lEp.Address)) continue;

                    serverInfo.IP = lEp.Address.ToString();
                    serverInfo.Port = lEp.Port;
                    await serverInfo.Send(ip);
                    break;
                }
                
                await SendEncoderUpdates(Client.Cache.ToList());

                await TaskEx.Delay(100);
            }));
            if(_handshakeTasksRunning)
                return;
            _handshakeTasksRunning = true;

            await Task.Factory.StartNew(() =>
            {
                while(true)
                {
                    Task task = null;
                    lock(_clientsLock)
                    {
                        if(_handshakeTasks.Count == 0)
                            break;
                        task = _handshakeTasks.Dequeue();

                        if(task == null)
                            continue;
                        task.Start();
                        task.Wait();
                    }
                }
                _handshakeTasksRunning = false;
            });
        }
        
        public static async void GetWorkHandler(PacketHeader packetheader, Connection connection, GetWork req)
        {
            var ip = ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString();
            var client = Client.Cache.FirstOrDefault(x => x.IP == ip);
            if (!(client?.IsEnabled ?? false))
            {
                Activity.Log(
                    Activity.Categories.Network,
                    Activity.Types.Warning,
                    $"Work item requested at an unauthorized terminal ({ip}).");
                return;
            }

            client.IsOnline = true;
            client.LastHeartBeat = DateTime.Now;
            TerminalLog.Add(client.Id, "Work item requested.");
            
            var work = Request.GetNextRequest();
            if (work == null)
            {
                await new GetWorkResult(ResultCodes.NotFound).Send(new IPEndPoint(IPAddress.Parse(client.IP), client.Port));
                return;
            }
            
            //work.Update(nameof(work.Status),Request.Statuses.Proccessing);
            work.Status = Request.Statuses.Proccessing;

            var student = Models.Student.Cache.FirstOrDefault(x => x.StudentId == work.StudentId);
            
            var result = new GetWorkResult(ResultCodes.Success)
            {
                RequestId = work.Id,
                StudentId = work.StudentId?.ToUpper(),
                StudentName = $"{student?.FirstName} {student?.LastName}"
            };

            var items = RequestDetail.Cache.Where(x => x.RequestId == work.Id).ToList();
            foreach (var item in items)
            {
                var sched = Models.ClassSchedule.Cache.FirstOrDefault(x => x.Id == item.ScheduleId);
                result.ClassSchedules.Add(new ClassSchedule()
                {
                    ClassId = item.ScheduleId,
                    SubjectCode = item.Schedule.Subject.Code,
                    Instructor = sched?.Instructor,
                    Room = sched?.Room,
                    Schedule = sched?.Description,
                });
            }

            await result.Send(new IPEndPoint(IPAddress.Parse(client.IP), client.Port));
            
            await SendEncoderUpdates(Client.Cache.ToList());
        }

        private static void SaveWorkHandler(PacketHeader packetheader, Connection connection, SaveWork i)
        {
            var ip = ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString();
            var client = Client.Cache.FirstOrDefault(x => x.IP == ip);
            if (!(client?.IsEnabled ?? false))
                return;

            client.IsOnline = true;
            client.LastHeartBeat = DateTime.Now;

            if (client.Encoder == null)
            {
                var res = new SaveWorkResult();
                res.Result = ResultCodes.Denied;
                res.Send(new IPEndPoint(IPAddress.Parse(client.IP), client.Port));
                return;
            }
            
            TerminalLog.Add(client.Id, "Enrollment item completed.");

            var req = Request.Cache.FirstOrDefault(x => string.Equals(x.StudentId, i.StudentId, StringComparison.CurrentCultureIgnoreCase));
            if (req == null) return;
            
            foreach (var sched in i.ClassSchedules)
            {
                var s = RequestDetail.Cache.FirstOrDefault(x => x.RequestId == req.Id && x.ScheduleId == sched.ClassId);
                if(s==null) continue;
                var stat = Request.Statuses.Pending;
                switch (sched.EnrollmentStatus)
                {
                    case ScheduleStatuses.Accepted:
                        stat = Request.Statuses.Accepted;
                        break;
                    case ScheduleStatuses.Conflict:
                        stat = Request.Statuses.Conflict;
                        break;
                    case ScheduleStatuses.Closed:
                        stat = Request.Statuses.Closed;
                        break;
                }
                if (stat > req.Status) req.Status = stat;
                s.Update(nameof(s.Status), stat);
            }
            req.EncoderId = client.Encoder.Id;
            req.Save();
            
            var result = new SaveWorkResult();
            result.Result = ResultCodes.Success;
            result.Send(new IPEndPoint(IPAddress.Parse(client.IP), client.Port));
        }

        private static void LogoutHandler(PacketHeader packetheader, Connection connection, Logout incomingobject)
        {
            var ip = ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString();
            var client = Client.Cache.FirstOrDefault(x => x.IP == ip);
            if (client?.Encoder == null) return;
            
            TerminalLog.Add(client.Id, $"{client.Encoder.Username} has logged out.");
            client.Encoder = null;
        }

        private static void PongHandler(PacketHeader packetheader, Connection connection, Pong incomingobject)
        {
            var ip = ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString();
            var client = Client.Cache.FirstOrDefault(x => x.IP == ip);
            if (client == null) return;
            client.IsOnline = true;
            
        }

        private static void GetCoursesHandler(PacketHeader packetheader, Connection connection, GetCoursesDesktop incomingobject)
        {
            var ip = ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString();
            var client = Client.Cache.FirstOrDefault(x => x.IP == ip);

            //Maybe do not ignore this on production
            if (client == null)
                return;

            var result = new GetCoursesResult();
            foreach (var course in Models.Course.Cache.ToList())
            {
                result.Courses.Add(new Course()
                {
                    Id = course.Id,
                    Name = course.Acronym,
                    Fullname = course.FullName
                });
            }

            result.Send(new IPEndPoint(IPAddress.Parse(client.IP), client.Port));
        }

        private static async void EnrollStudentHandler(PacketHeader packetheader, Connection connection, EnrollStudent req)
        {
            var ip = ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString();
            var client = Client.Cache.FirstOrDefault(x => x.IP == ip);

            //Maybe do not ignore this on production
            if (client == null)
                return;

            if (Models.Student.Cache.Any(x => x.StudentId.ToLower() == req.Student.StudentId.ToLower()))
            {
                await new EnrollStudentResult()
                {
                    Success = false,
                    ErrorMessage = "Student ID Taken"
                }.Send(new IPEndPoint(IPAddress.Parse(client.IP), client.Port));
                return;
            }
            var stud = new Models.Student()
            {
                StudentId = req.Student.StudentId,
                Address = req.Student.Address,
                BirthDate = req.Student.BirthDate,
                CourseId = req.Student.CourseId,
                FirstName = req.Student.FirstName,
                LastName = req.Student.LastName,
                Major = req.Student.Major,
                Minor = req.Student.Minor,
                Scholarship = req.Student.Scholarship,
            };
            stud.Save();

            await new EnrollStudentResult()
            {
                Success = true,
                Id = stud.Id
            }.Send(new IPEndPoint(IPAddress.Parse(client.IP), client.Port));
        }
    }
}
