using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace JIMCBVR.Server
{
    /// <summary>
    /// 帧处理器，负责对捕获的VR帧数据进行处理，如畸变矫正、色彩调整等
    /// </summary>
    public class FrameProcessor
    {   
        private readonly AppConfig _config;
        
        // 畸变矫正参数
        private float _distortionK1;
        private float _distortionK2;
        
        public FrameProcessor(AppConfig config)
        {   
            _config = config;
            UpdateDistortionParameters();
        }
        
        /// <summary>
        /// 更新畸变矫正参数
        /// </summary>
        public void UpdateDistortionParameters()
        {   
            _distortionK1 = _config.DistortionK1;
            _distortionK2 = _config.DistortionK2;
        }
        
        /// <summary>
        /// 处理帧数据，应用畸变矫正和其他图像处理
        /// </summary>
        /// <param name="frameData">原始帧数据</param>
        /// <param name="width">帧宽度</param>
        /// <param name="height">帧高度</param>
        /// <param name="isLeftEye">是否为左眼帧</param>
        /// <returns>处理后的帧数据</returns>
        public byte[] ProcessFrame(byte[] frameData, int width, int height, bool isLeftEye)
        {   
            if (frameData == null || frameData.Length == 0)
                return frameData;
                
            try
            {   
                // 创建位图对象
                using (var bitmap = CreateBitmapFromRawData(frameData, width, height))
                {
                    // 应用畸变矫正
                    ApplyDistortionCorrection(bitmap, isLeftEye);
                    
                    // 转换回字节数组
                    return BitmapToByteArray(bitmap);
                }
            }
            catch (Exception ex)
            {   
                Console.WriteLine($"帧处理错误: {ex.Message}");
                return frameData; // 出错时返回原始数据
            }
        }
        
        /// <summary>
        /// 从原始字节数据创建位图
        /// </summary>
        private Bitmap CreateBitmapFromRawData(byte[] data, int width, int height)
        {   
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
                
            try
            {   
                // 复制数据到位图
                Marshal.Copy(data, 0, bitmapData.Scan0, Math.Min(data.Length, bitmapData.Stride * height));
            }
            finally
            {   
                bitmap.UnlockBits(bitmapData);
            }
            
            return bitmap;
        }
        
        /// <summary>
        /// 应用畸变矫正
        /// </summary>
        private void ApplyDistortionCorrection(Bitmap bitmap, bool isLeftEye)
        {   
            int width = bitmap.Width;
            int height = bitmap.Height;
            
            // 创建临时位图进行处理
            using (var tempBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(tempBitmap))
                {
                    g.DrawImage(bitmap, 0, 0, width, height);
                }
                
                // 锁定位图数据进行像素级操作
                var srcData = tempBitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);
                    
                var destData = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);
                    
                try
                {   
                    // 获取源数据指针
                    IntPtr srcPtr = srcData.Scan0;
                    IntPtr destPtr = destData.Scan0;
                    
                    // 计算中心点
                    float centerX = width / 2.0f;
                    float centerY = height / 2.0f;
                    
                    // 应用畸变矫正算法
                    unsafe
                    {   
                        for (int y = 0; y < height; y++)
                        {   
                            for (int x = 0; x < width; x++)
                            {   
                                // 计算归一化坐标 (-1 到 1)
                                float nx = (x - centerX) / centerX;
                                float ny = (y - centerY) / centerY;
                                
                                // 计算径向距离的平方
                                float r2 = nx * nx + ny * ny;
                                float r4 = r2 * r2;
                                
                                // 应用畸变公式
                                float distortionFactor = 1.0f + _distortionK1 * r2 + _distortionK2 * r4;
                                
                                // 计算源坐标
                                float srcX = centerX + (nx * distortionFactor * centerX);
                                float srcY = centerY + (ny * distortionFactor * centerY);
                                
                                // 检查边界
                                if (srcX >= 0 && srcX < width && srcY >= 0 && srcY < height)
                                {   
                                    // 双线性插值
                                    int x1 = (int)srcX;
                                    int y1 = (int)srcY;
                                    int x2 = Math.Min(x1 + 1, width - 1);
                                    int y2 = Math.Min(y1 + 1, height - 1);
                                    
                                    float xFrac = srcX - x1;
                                    float yFrac = srcY - y1;
                                    
                                    // 获取四个相邻像素
                                    byte* p1 = (byte*)srcPtr + y1 * srcData.Stride + x1 * 4;
                                    byte* p2 = (byte*)srcPtr + y1 * srcData.Stride + x2 * 4;
                                    byte* p3 = (byte*)srcPtr + y2 * srcData.Stride + x1 * 4;
                                    byte* p4 = (byte*)srcPtr + y2 * srcData.Stride + x2 * 4;
                                    
                                    // 目标像素指针
                                    byte* pDest = (byte*)destPtr + y * destData.Stride + x * 4;
                                    
                                    // 对每个颜色通道进行插值
                                    for (int c = 0; c < 4; c++)
                                    {   
                                        float c1 = p1[c];
                                        float c2 = p2[c];
                                        float c3 = p3[c];
                                        float c4 = p4[c];
                                        
                                        // 双线性插值公式
                                        float color = c1 * (1 - xFrac) * (1 - yFrac) +
                                                     c2 * xFrac * (1 - yFrac) +
                                                     c3 * (1 - xFrac) * yFrac +
                                                     c4 * xFrac * yFrac;
                                                     
                                        pDest[c] = (byte)Math.Max(0, Math.Min(255, color));
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {   
                    tempBitmap.UnlockBits(srcData);
                    bitmap.UnlockBits(destData);
                }
            }
        }
        
        /// <summary>
        /// 将位图转换为字节数组
        /// </summary>
        private byte[] BitmapToByteArray(Bitmap bitmap)
        {   
            int width = bitmap.Width;
            int height = bitmap.Height;
            
            var result = new byte[width * height * 4]; // RGBA格式
            
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
                
            try
            {   
                // 复制位图数据到字节数组
                Marshal.Copy(bitmapData.Scan0, result, 0, result.Length);
            }
            finally
            {   
                bitmap.UnlockBits(bitmapData);
            }
            
            return result;
        }
    }
}