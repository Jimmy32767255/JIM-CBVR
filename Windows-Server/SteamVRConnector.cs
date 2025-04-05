using OpenVR;
using System;
using System.Threading;

namespace JIMCBVR.Server
{
    /// <summary>
    /// SteamVR连接管理器，负责初始化VR系统和帧数据捕获
    /// </summary>
    public class SteamVRConnector : IDisposable
    {
        private CVRSystem _vrSystem;
        private Thread _renderThread;
        private bool _isRunning;

        /// <summary>
        /// 初始化SteamVR连接
        /// </summary>
        public void Initialize()
        {
            var error = EVRInitError.None;
            _vrSystem = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Background);
            
            if (error != EVRInitError.None)
                throw new Exception($"SteamVR初始化失败：{error}");

            StartRenderThread();
        }

        private void StartRenderThread()
        {
            _isRunning = true;
            _renderThread = new Thread(RenderLoop)
            {
                Priority = ThreadPriority.Highest
            };
            _renderThread.Start();
        }

        private void RenderLoop()
        {
            while (_isRunning)
            {
                // 此处添加帧捕获和编码逻辑
                Thread.Sleep(10);
            }
        }

        public void Dispose()
        {
            _isRunning = false;
            _renderThread?.Join();
            OpenVR.Shutdown();
        }
    }
}