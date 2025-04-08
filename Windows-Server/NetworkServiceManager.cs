using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace JIMCBVR.Server
{
    /// <summary>
    /// 网络服务管理器，处理端口冲突检测和双模式连接
    /// </summary>
    public class NetworkServiceManager
    {
        private TcpListener _tcpListener;
        private Thread _listenerThread;
        private AppConfig _config;
        private SteamVRConnector _vrConnector;
        private List<ClientConnection> _clients = new List<ClientConnection>();
        private readonly object _clientsLock = new object();
        
        // 帧数据队列
        private ConcurrentQueue<FramePacket> _framePackets = new ConcurrentQueue<FramePacket>();

        public NetworkServiceManager(AppConfig config)
        {
            _config = config;
        }
        
        /// <summary>
        /// 设置VR连接器
        /// </summary>
        /// <param name="connector">SteamVR连接器实例</param>
        public void SetVRConnector(SteamVRConnector connector)
        {
            _vrConnector = connector;
            _vrConnector.EncodedFrameReady += OnEncodedFrameReady;
        }

        public void StartService()
        {
            var port = FindAvailablePort(_config.MainPort);
            _tcpListener = new TcpListener(IPAddress.Any, port);
            
            _listenerThread = new Thread(() =>
            {
                try
                {
                    _tcpListener.Start();
                    Console.WriteLine($"服务已启动，监听端口: {port}");
                    
                    while (true)
                    {
                        var client = _tcpListener.AcceptTcpClient();
                        HandleNewClient(client);
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"网络错误: {ex.Message}");
                }
            })
            {
                IsBackground = true
            };
            _listenerThread.Start();
        }
        
        private void HandleNewClient(TcpClient client)
        {
            try
            {
                var clientConnection = new ClientConnection(client, _config);
                clientConnection.Disconnected += OnClientDisconnected;
                
                lock (_clientsLock)
                {
                    _clients.Add(clientConnection);
                }
                
                Console.WriteLine($"新客户端连接: {clientConnection.ClientId}");
                clientConnection.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理新客户端连接时出错: {ex.Message}");
                client.Close();
            }
        }
        
        private void OnClientDisconnected(object sender, EventArgs e)
        {
            var clientConnection = sender as ClientConnection;
            if (clientConnection != null)
            {
                lock (_clientsLock)
                {
                    _clients.Remove(clientConnection);
                }
                Console.WriteLine($"客户端断开连接: {clientConnection.ClientId}");
            }
        }
        
        private void OnEncodedFrameReady(object sender, SteamVRConnector.EncodedFrameEventArgs e)
        {
            // 将编码帧添加到队列
            _framePackets.Enqueue(new FramePacket
            {
                Data = e.EncodedData,
                Timestamp = e.Timestamp,
                IsLeftEye = e.IsLeftEye
            });
            
            // 处理队列中的帧数据
            ProcessFrameQueue();
        }
        
        private void ProcessFrameQueue()
        {
            while (_framePackets.TryDequeue(out FramePacket packet))
            {
                // 向所有连接的客户端发送帧数据
                lock (_clientsLock)
                {
                    foreach (var client in _clients)
                    {
                        try
                        {
                            client.SendFrame(packet.Data, packet.Timestamp, packet.IsLeftEye);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"向客户端 {client.ClientId} 发送帧时出错: {ex.Message}");
                        }
                    }
                }
            }
        }

        private int FindAvailablePort(int startPort)
        {
            for (int port = startPort; port < startPort + 100; port++)
            {
                try
                {
                    using (var tester = new TcpListener(IPAddress.Loopback, port))
                    {
                        tester.Start();
                        tester.Stop();
                        return port;
                    }
                }
                catch { }
            }
            throw new Exception("未找到可用端口");
        }

        public void StopService()
        {
            // 断开VR连接器事件
            if (_vrConnector != null)
            {
                _vrConnector.EncodedFrameReady -= OnEncodedFrameReady;
            }
            
            // 关闭所有客户端连接
            lock (_clientsLock)
            {
                foreach (var client in _clients)
                {
                    client.Stop();
                }
                _clients.Clear();
            }
            
            // 停止监听
            _tcpListener?.Stop();
            _listenerThread?.Join(1000);
            
            Console.WriteLine("网络服务已停止");
        }
        
        // 帧数据包
        private class FramePacket
        {
            public byte[] Data { get; set; }
            public long Timestamp { get; set; }
            public bool IsLeftEye { get; set; }
        }
        
        // 客户端连接类
        private class ClientConnection
        {
            private TcpClient _client;
            private NetworkStream _stream;
            private Thread _receiveThread;
            private bool _isRunning;
            private AppConfig _config;
            
            public string ClientId { get; }
            
            public event EventHandler Disconnected;
            
            public ClientConnection(TcpClient client, AppConfig config)
            {
                _client = client;
                _stream = client.GetStream();
                _config = config;
                ClientId = ((IPEndPoint)client.Client.RemoteEndPoint).ToString();
            }
            
            public void Start()
            {
                _isRunning = true;
                _receiveThread = new Thread(ReceiveLoop)
                {
                    IsBackground = true
                };
                _receiveThread.Start();
            }
            
            private void ReceiveLoop()
            {
                byte[] buffer = new byte[1024];
                
                try
                {
                    while (_isRunning)
                    {
                        if (_stream.DataAvailable)
                        {
                            int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                            if (bytesRead > 0)
                            {
                                // 处理客户端消息
                                ProcessClientMessage(buffer, bytesRead);
                            }
                        }
                        else
                        {
                            Thread.Sleep(1);
                        }
                    }
    }
}