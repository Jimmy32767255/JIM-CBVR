using System;

namespace JIMCBVR.Server
{
    /// <summary>
    /// 应用程序配置模型（自动序列化为JSON）
    /// </summary>
    public class AppConfig
    {
        // 视频设置
        public int MaxFPS { get; set; } = 90;
        public string QualityPreset { get; set; } = "High";
        
        // 网络设置
        public int MainPort { get; set; } = 5588;
        public bool EnableUSBDebug { get; set; } = true;
        
        // 矫正参数
        public float DistortionK1 { get; set; } = 0.25f;
        public float DistortionK2 { get; set; } = 0.05f;
        
        // 运行时状态（不保存）
        [NonSerialized]
        public bool IsStreaming;
    }
}