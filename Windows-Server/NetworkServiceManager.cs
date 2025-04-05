using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

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

        public NetworkServiceManager(AppConfig config)
        {
            _config = config;
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
                    while (true)
                    {
                        var client = _tcpListener.AcceptTcpClient();
                        // 处理客户端连接
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
            _tcpListener?.Stop();
            _listenerThread?.Join(1000);
        }
    }
}