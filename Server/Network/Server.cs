using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NetworkCommsDotNet.Connections.UDP;
using NetworkCommsDotNet.DPSBase;
using NetworkCommsDotNet.Tools;
using NORSU.EncodeMe.Models;

namespace NORSU.EncodeMe.Network
{
    static partial class Server
    {
        private static bool _started;
        
        public static void Start()
        {
            if (_started) return;
            _started = true;
            
            var serializer = DPSManager.GetDataSerializer<ProtobufSerializer>();
            
            NetworkComms.DefaultSendReceiveOptions = new SendReceiveOptions(serializer,
                NetworkComms.DefaultSendReceiveOptions.DataProcessors, NetworkComms.DefaultSendReceiveOptions.Options);
            
            PeerDiscovery.EnableDiscoverable(PeerDiscovery.DiscoveryMethod.UDPBroadcast);
            
            NetworkComms.AppendGlobalIncomingPacketHandler<AndroidInfo>(AndroidInfo.GetHeader(), AndroidHandler.HandShakeHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<StudentInfoRequest>(StudentInfoRequest.GetHeader(), AndroidHandler.StudentInfoRequested);
            NetworkComms.AppendGlobalIncomingPacketHandler<EndPointInfo>(EndPointInfo.GetHeader(), HandShakeHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<GetWork>(GetWork.GetHeader(), GetWorkHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<Login>(Login.GetHeader(), LoginHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<Logout>(Logout.GetHeader(),LogoutHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<SchedulesRequest>(SchedulesRequest.GetHeader(), AndroidHandler.ScheduleRequestHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<EnrollRequest>(EnrollRequest.GetHeader(),AndroidHandler.EnrollRequestHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<RegisterStudent>(RegisterStudent.GetHeader(), AndroidHandler.RegisterStudentHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<SaveWork>(SaveWork.GetHeader(),SaveWorkHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<GetCourses>(GetCourses.GetHeader(), AndroidHandler.GetCoursesHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<GetCoursesDesktop>(GetCoursesDesktop.GetHeader(),GetCoursesHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<Pong>(Pong.GetHeader(),PongHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<EnrollStudent>(EnrollStudent.GetHeader(),EnrollStudentHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<StartEnrollment>(StartEnrollment.GetHeader(),AndroidHandler.StartEnrollmentHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<AddSchedule>(AddSchedule.GetHeader(),AndroidHandler.AddScheduleHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<CommitEnrollment>(CommitEnrollment.GetHeader(),AndroidHandler.CommitEnrollmentHandler);
            Connection.StartListening(ConnectionType.UDP, new IPEndPoint(IPAddress.Any, 7777), true);
        }
        
        public static void Stop()
        {
            _started = false;
            
            var list = Client.Cache.Where(x => x.IsOnline)?.ToList();
            foreach (var client in list)
            {
                new Disconnected().Send(new IPEndPoint(IPAddress.Parse(client.IP), client.Port));
                TerminalLog.Add(client.Id, "Disconnected");
            }
            
            Connection.StopListening();
            NetworkComms.Shutdown();
        }
        
        public static async Task PushUpdate(string studentId)
        {
            
        }

        public static void CheckTerminalConnections()
        {
            var clients = Client.Cache.OrderByDescending(x => x.IsOnline).ToList();
            Parallel.ForEach(clients,async client =>
            {
                var ep = new IPEndPoint(IPAddress.Parse(client.IP), client.Port);
                client.IsOnline = await Ping(ep);
            });
            
        }

        public static async Task<bool> Ping(IPEndPoint ep)
        {
            var head = $"PONG{ep.Address}";
            var pong = false;
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(head, 
                (h, c, i) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(head);
                    pong = true;
                });

            var sent = false;
            while (!sent)
            {
                try
                {
                    UDPConnection.SendObject("PING",
                        "https://github.com/awooo-ph", ep,
                        NetworkComms.DefaultSendReceiveOptions,
                        ApplicationLayerProtocolStatus.Enabled);
                    sent = true;
                    break;
                }
                catch (Exception)
                {
                    await TaskEx.Delay(100);
                }
            }

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < 4710)
            {
                if (pong)
                    return pong;
                await TaskEx.Delay(TimeSpan.FromMilliseconds(100));
            }

            return pong;
        }

        public static Task SendEncoderUpdates(List<Client> clients)
        {
            return Task.Factory.StartNew(() =>
            {
                var update = new ServerUpdate();
                update.Requests = Request.Cache.Count(x => x.Status == Request.Statuses.Pending);
                update.Encoders = clients.Count(x => x.IsOnline);
                //Parallel.ForEach(Client.Cache, c => update.Send(new IPEndPoint(IPAddress.Parse(c.IP), c.Port)));
                
                foreach (var c in clients) update.Send(new IPEndPoint(IPAddress.Parse(c.IP), c.Port));
            });
        }
    }
}
