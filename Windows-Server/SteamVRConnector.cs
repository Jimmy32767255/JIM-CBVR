using OpenVR;
using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Threading.Tasks;

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
        
        // 帧捕获和编码相关字段
        private CVRCompositor _compositor;
        private Texture_t _leftEyeTexture;
        private Texture_t _rightEyeTexture;
        private VRTextureBounds_t _textureBounds;
        private ConcurrentQueue<FrameData> _frameQueue;
        private Task _encodingTask;
        private CancellationTokenSource _encodingCts;
        private VideoEncoder _encoder;
        private FrameProcessor _frameProcessor;
        private AppConfig _config;
        
        // 事件：当新的编码帧可用时触发
        public event EventHandler<EncodedFrameEventArgs> EncodedFrameReady;
        
        public SteamVRConnector(AppConfig config)
        {
            _config = config;
            _frameQueue = new ConcurrentQueue<FrameData>();
        }

        /// <summary>
        /// 初始化SteamVR连接
        /// </summary>
        public void Initialize()
        {
            var error = EVRInitError.None;
            _vrSystem = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Background);
            
            if (error != EVRInitError.None)
                throw new Exception($"SteamVR初始化失败：{error}");
            
            // 初始化Compositor
            _compositor = OpenVR.Compositor;
            if (_compositor == null)
                throw new Exception("无法初始化SteamVR Compositor");
            
            // 初始化纹理和边界
            InitializeTextures();
            
            // 初始化帧处理器
            _frameProcessor = new FrameProcessor(_config);
            
            // 初始化编码器
            _encoder = new VideoEncoder(1080, 1200, _config.MaxFPS, GetEncoderQuality(_config.QualityPreset));
            
            // 启动编码任务
            StartEncodingTask();
            
            // 启动渲染线程
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

        private void InitializeTextures()
        {
            // 设置纹理边界（完整纹理）
            _textureBounds = new VRTextureBounds_t
            {
                uMin = 0,
                uMax = 1,
                vMin = 0,
                vMax = 1
            };
            
            // 创建左右眼纹理
            uint width = 1080; // 根据需要调整分辨率
            uint height = 1200;
            
            _leftEyeTexture = new Texture_t
            {
                handle = IntPtr.Zero, // 将在捕获时设置
                eType = ETextureType.DirectX,
                eColorSpace = EColorSpace.Auto
            };
            
            _rightEyeTexture = new Texture_t
            {
                handle = IntPtr.Zero, // 将在捕获时设置
                eType = ETextureType.DirectX,
                eColorSpace = EColorSpace.Auto
            };
        }
        
        private void StartEncodingTask()
        {
            _encodingCts = new CancellationTokenSource();
            _encodingTask = Task.Run(() => EncodingLoop(_encodingCts.Token));
        }
        
        private void EncodingLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_frameQueue.TryDequeue(out FrameData frameData))
                {
                    try
                    {
                        // 处理帧数据（应用畸变矫正等）
                        byte[] processedData = _frameProcessor.ProcessFrame(
                            frameData.Data, 
                            frameData.Width, 
                            frameData.Height, 
                            frameData.IsLeftEye);
                        
                        // 编码处理后的帧数据
                        byte[] encodedData = _encoder.EncodeFrame(processedData, frameData.Timestamp);
                        
                        // 触发事件通知新帧可用
                        EncodedFrameReady?.Invoke(this, new EncodedFrameEventArgs
                        {
                            EncodedData = encodedData,
                            Timestamp = frameData.Timestamp,
                            IsLeftEye = frameData.IsLeftEye
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"帧编码错误: {ex.Message}");
                    }
                    finally
                    {
                        // 释放帧数据资源
                        frameData.Dispose();
                    }
                }
                else
                {
                    // 队列为空，短暂休眠
                    Thread.Sleep(1);
                }
            }
        }
        
        private void RenderLoop()
        {
            while (_isRunning)
            {
                try
                {
                    // 等待VR合成器提交下一帧
                    _compositor.WaitGetPoses(null, 0, null, 0);
                    
                    // 捕获左眼帧
                    CaptureEyeFrame(EVREye.Eye_Left);
                    
                    // 捕获右眼帧
                    CaptureEyeFrame(EVREye.Eye_Right);
                    
                    // 控制帧率
                    int sleepTime = 1000 / _config.MaxFPS;
                    Thread.Sleep(Math.Max(1, sleepTime - 2)); // 减去处理时间的估计值
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"帧捕获错误: {ex.Message}");
                    Thread.Sleep(100); // 错误后短暂暂停
                }
            }
        }
        
        private void CaptureEyeFrame(EVREye eye)
        {
            try
            {
                // 获取眼睛的投影矩阵
                HmdMatrix44_t projectionMatrix = _vrSystem.GetProjectionMatrix(eye, 0.1f, 100.0f);
                
                // 获取眼睛的姿态矩阵
                HmdMatrix34_t eyeToHeadTransform = _vrSystem.GetEyeToHeadTransform(eye);
                
                // 创建帧缓冲区
                var frameData = new FrameData
                {
                    Width = 1080, // 与纹理大小匹配
                    Height = 1200,
                    IsLeftEye = eye == EVREye.Eye_Left,
                    Timestamp = DateTime.Now.Ticks
                };
                
                // 从VR合成器获取帧数据
                Texture_t targetTexture = eye == EVREye.Eye_Left ? _leftEyeTexture : _rightEyeTexture;
                
                // 获取当前帧的纹理
                var error = _compositor.GetMirrorTextureD3D11(eye, targetTexture.eType, ref targetTexture.handle);
                if (error != EVRCompositorError.None)
                {
                    Console.WriteLine($"获取镜像纹理失败: {error}");
                    return;
                }
                
                // 将纹理数据复制到帧缓冲区
                // 注意：这里需要根据实际的DirectX实现进行调整
                frameData.CaptureTextureData(targetTexture.handle);
                
                // 将帧添加到编码队列
                _frameQueue.Enqueue(frameData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"捕获{(eye == EVREye.Eye_Left ? "左" : "右")}眼帧错误: {ex.Message}");
            }
        }
        
        private VideoEncoder.EncoderQuality GetEncoderQuality(string preset)
        {
            switch (preset.ToLower())
            {
                case "low":
                    return VideoEncoder.EncoderQuality.Low;
                case "medium":
                    return VideoEncoder.EncoderQuality.Medium;
                case "high":
                default:
                    return VideoEncoder.EncoderQuality.High;
            }
        }

        public void Dispose()
        {
            _isRunning = false;
            _renderThread?.Join();
            
            // 停止编码任务
            _encodingCts?.Cancel();
            _encodingTask?.Wait(1000);
            
            // 释放编码器资源
            _encoder?.Dispose();
            
            // 清空帧队列并释放资源
            while (_frameQueue.TryDequeue(out FrameData frameData))
            {
                frameData.Dispose();
            }
            
            // 帧处理器不需要显式释放资源
            
            // 关闭OpenVR
            OpenVR.Shutdown();
        }
        
        // 帧数据类，用于存储捕获的帧
        private class FrameData : IDisposable
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public bool IsLeftEye { get; set; }
            public long Timestamp { get; set; }
            public byte[] Data { get; private set; }
            
            public void CaptureTextureData(IntPtr textureHandle)
            {
                // 实现从DirectX纹理捕获数据的逻辑
                // 这里需要使用SharpDX或类似库来访问DirectX资源
                // 简化示例：创建一个测试图像
                Data = new byte[Width * Height * 4]; // RGBA格式
                
                // 在实际实现中，这里应该从textureHandle中提取实际的纹理数据
                // 例如：使用SharpDX.Direct3D11.Texture2D和SharpDX.Direct3D11.DeviceContext
            }
            
            public void Dispose()
            {
                Data = null;
            }
        }
        
        // 编码帧事件参数
        public class EncodedFrameEventArgs : EventArgs
        {
            public byte[] EncodedData { get; set; }
            public long Timestamp { get; set; }
            public bool IsLeftEye { get; set; }
        }
        

    }
}