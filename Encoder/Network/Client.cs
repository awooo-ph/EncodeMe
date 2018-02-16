using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NetworkCommsDotNet.Connections.UDP;
using NetworkCommsDotNet.DPSBase;
using NetworkCommsDotNet.Tools;
using NORSU.EncodeMe.ViewModels;

namespace NORSU.EncodeMe.Network
{
    static class Client
    {
        private static bool _started;
        
        public static void Start()
        {
            if (_started) return;
            _started = true;
            
            //NetworkComms.EnableLogging(new LiteLogger(LiteLogger.LogMode.ConsoleOnly));
            NetworkComms.DisableLogging();
            
            NetworkComms.IgnoreUnknownPacketTypes = true;
            var serializer = DPSManager.GetDataSerializer<ProtobufSerializer>();
            
            NetworkComms.DefaultSendReceiveOptions = new SendReceiveOptions(serializer,
                NetworkComms.DefaultSendReceiveOptions.DataProcessors, NetworkComms.DefaultSendReceiveOptions.Options);
            
            NetworkComms.AppendGlobalIncomingPacketHandler<ServerInfo>(ServerInfo.GetHeader(), ServerInfoReceived);
            NetworkComms.AppendGlobalIncomingPacketHandler<Logout>(Network.Logout.GetHeader(), LogoutHandlger);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>("PING", PingHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<Ping>(Ping.GetHeader(),PingPongHandler);
            PeerDiscovery.EnableDiscoverable(PeerDiscovery.DiscoveryMethod.UDPBroadcast);
            PeerDiscovery.OnPeerDiscovered += OnPeerDiscovered;
            
            Connection.StartListening(ConnectionType.UDP, new IPEndPoint(IPAddress.Any, 0));
            PeerDiscovery.DiscoverPeersAsync(PeerDiscovery.DiscoveryMethod.UDPBroadcast);
            
        }

        private static async void PingPongHandler(PacketHeader packetheader, Connection connection, Ping incomingobject)
        {
            if(Server!=null)
            await new Pong().Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));
        }

        private static async void PingHandler(PacketHeader packetHeader, Connection connection, string incomingObject)
        {
            var ip =(IPEndPoint) connection.ConnectionInfo.RemoteEndPoint;
            var localEPs = Connection.AllExistingLocalListenEndPoints();
            var lep = "";
            foreach (var localEP in localEPs[ConnectionType.UDP])
            {
                var lEp = (IPEndPoint) localEP;
                if (!ip.Address.IsInSameSubnet(lEp.Address))
                    continue;
                lep = lEp.Address.ToString();
                break;
            }
            
            var sent = false;
            while (!sent)
            {
                try
                {
                    UDPConnection.SendObject($"PONG{lep}", "https://github.com/awooo-ph", ip, NetworkComms.DefaultSendReceiveOptions,
                        ApplicationLayerProtocolStatus.Enabled);
                    sent = true;
                    break;
                }
                catch (Exception)
                {
                    await TaskEx.Delay(100);
                }
            }
        }

        private static void LogoutHandlger(PacketHeader packetHeader, Connection connection, Logout incomingObject)
        {
            Encoder = null;
            MainViewModel.Instance.Encoder = null;
        }

        public static async void Stop()
        {
            if(!_started) return;

            if(Server!=null)
                await new Disconnected().Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));
            
            Connection.StopListening();
            NetworkComms.Shutdown();
        }

        private static ServerInfo Server;
        private static void ServerInfoReceived(PacketHeader packetheader, Connection connection, ServerInfo incomingobject)
        {
            Server = incomingobject;
        }

        private static async void OnPeerDiscovered(ShortGuid peeridentifier, Dictionary<ConnectionType, List<EndPoint>> endPoints)
        {
            var info = new EndPointInfo(Environment.MachineName);
            
            var eps = endPoints[ConnectionType.UDP].Where(x=>((IPEndPoint)x).Port==7777);
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
                    info.Port = lEp.Port;                   
                    await info.Send(ip);
                }
            }
        }

        public static async Task FindServer()
        {
            if (Server != null) return;
            Start();
            var start = DateTime.Now;
            PeerDiscovery.DiscoverPeersAsync(PeerDiscovery.DiscoveryMethod.UDPBroadcast);
            while ((DateTime.Now-start).TotalSeconds<20)
            {
                if(Server!=null) break;
                await TaskEx.Delay(TimeSpan.FromSeconds(7));
            }
        }
        
        public static Encoder Encoder { get; private set; }
        
        public static async Task<LoginResult> Login(string username, string password)
        {
                await FindServer();
                if (Server == null) return new LoginResult(ResultCodes.Offline);
                
                var login = new Login(){Username = username,Password = password};

                LoginResult result = null;
                
                NetworkComms.AppendGlobalIncomingPacketHandler<LoginResult>(LoginResult.GetHeader(),
                    (h, c, res) =>
                    {
                        NetworkComms.RemoveGlobalIncomingPacketHandler(LoginResult.GetHeader());
                        result = res;
                        Encoder = res?.Encoder;
                    });
            
                await login.Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));

            var start = DateTime.Now;
            while ((DateTime.Now-start).TotalSeconds<17)
                {
                    if (result != null) return result;
                    await TaskEx.Delay(TimeSpan.FromSeconds(1));
                }

            Server = null;

            NetworkComms.RemoveGlobalIncomingPacketHandler(LoginResult.GetHeader());
            
            return new LoginResult(ResultCodes.Timeout);
        }

        public static async Task<SaveWorkResult> SaveWork(SaveWork work)
        {
            await FindServer();
            if (Server == null) return new SaveWorkResult() { Result = ResultCodes.Offline};

            SaveWorkResult result = null;
            
            NetworkComms.AppendGlobalIncomingPacketHandler<SaveWorkResult>(SaveWorkResult.GetHeader(),
                (h, c, i) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(SaveWorkResult.GetHeader());
                    result = i;
                    if (i?.Result == ResultCodes.Denied)
                        Encoder = null;
                });

            await work.Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null) return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }

            Server = null;
            NetworkComms.RemoveGlobalIncomingPacketHandler(SaveWorkResult.GetHeader());
            return new SaveWorkResult(){Result = ResultCodes.Timeout};
        }

        public static async Task<EnrollStudentResult> EnrollStudent(Student student)
        {
            await FindServer();
            if (Server == null) return null;

            EnrollStudentResult result = null;

            NetworkComms.AppendGlobalIncomingPacketHandler<EnrollStudentResult>(EnrollStudentResult.GetHeader(),
                (h, c, i) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(EnrollStudentResult.GetHeader());
                    result = i;
                });

            await new EnrollStudent(){Student = student}.Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null) return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }

            Server = null;
            NetworkComms.RemoveGlobalIncomingPacketHandler(EnrollStudentResult.GetHeader());
            return null;
        }

        public static async Task<GetCoursesResult> GetCoursesDesktop()
        {
            await FindServer();
            if (Server == null) return null;

            GetCoursesResult result = null;

            NetworkComms.AppendGlobalIncomingPacketHandler<GetCoursesResult>(GetCoursesResult.GetHeader(),
                (h, c, i) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(GetCoursesResult.GetHeader());
                    result = i;
                });

            await new GetCoursesDesktop().Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }

            Server = null;
            NetworkComms.RemoveGlobalIncomingPacketHandler(GetCoursesResult.GetHeader());
            return null;
        }

        public static async Task<GetWorkResult> GetNextWork(string username="")
        {
            await FindServer();
            if (Server == null) return new GetWorkResult(ResultCodes.Offline);
            
            var request = new GetWork(username);
            GetWorkResult result = null;
            
            NetworkComms.AppendGlobalIncomingPacketHandler<GetWorkResult>(GetWorkResult.GetHeader(),
                (h, c, i) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(GetWorkResult.GetHeader());
                    result = i;
                });

            await request.Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null) return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }

            Server = null;
            NetworkComms.RemoveGlobalIncomingPacketHandler(GetWorkResult.GetHeader());
            return new GetWorkResult(ResultCodes.Timeout);
        }

        public static async void Logout()
        {
            await FindServer();
            if (Server == null) return;

            await new Logout().Send(new IPEndPoint(IPAddress.Parse(Server.IP), Server.Port));
        }
    }
}
