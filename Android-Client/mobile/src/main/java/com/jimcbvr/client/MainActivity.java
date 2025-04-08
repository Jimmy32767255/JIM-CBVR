package com.jimcbvr.client;

import android.opengl.GLSurfaceView;
import android.os.Bundle;
import androidx.appcompat.app.AppCompatActivity;
import javax.microedition.khronos.egl.EGLConfig;
import javax.microedition.khronos.opengles.GL10;

public class MainActivity extends AppCompatActivity {
    private GLSurfaceView glSurfaceView;
    private NetworkManager networkManager;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        initGLSurfaceView();
        setupNetworkConnection();
    }

    private void initGLSurfaceView() {
        glSurfaceView = new GLSurfaceView(this);
        glSurfaceView.setEGLContextClientVersion(3);
        GLRenderer renderer = new GLRenderer();
        glSurfaceView.setRenderer(renderer);
        // 保存渲染器实例以便后续访问
        glSurfaceView.setTag(renderer);
        setContentView(glSurfaceView);
    }

    private void setupNetworkConnection() {
        // 双通道连接初始化逻辑
        networkManager = new NetworkManager(this, networkCallback);
        networkManager.startConnection(6144); // 默认端口，可根据实际需求调整
    }

    private static class GLRenderer implements GLSurfaceView.Renderer {
        private float distortionK1 = 0.25f;
        private float distortionK2 = 0.05f;

        @Override
        public void onSurfaceCreated(GL10 gl, EGLConfig config) {
            // 初始化着色器并传入初始矫正参数
            initShader(distortionK1, distortionK2);
        }

        public void updateDistortionParams(float k1, float k2) {
            distortionK1 = k1;
            distortionK2 = k2;
            // 更新着色器参数
            updateShaderParams(k1, k2);
        }

        private native void initShader(float k1, float k2);
        private native void updateShaderParams(float k1, float k2);
        
        @Override
        public void onSurfaceChanged(GL10 gl, int width, int height) {
            // 画面尺寸变化处理
        }

        @Override
        public void onDrawFrame(GL10 gl) {
            // 每帧渲染逻辑
        }
    }

    // 在MainActivity中添加参数更新回调
    private final NetworkManager.NetworkCallback networkCallback = new NetworkManager.NetworkCallback() {
        @Override
        public void onConnected() {
            // 连接成功处理
        }

        @Override
        public void onDisconnected(String reason) {
            // 连接断开处理
        }

        @Override
        public void onError(String message) {
            // 错误处理
        }
        
        @Override
        public void onFrameReceived(byte[] frameData) {
            // 解析视频帧中的矫正参数（需要与服务端协议保持一致）
            float[] params = parseDistortionParams(frameData);
            glSurfaceView.queueEvent(() -> {
                // 修复：不能直接通过getRenderer获取渲染器
                // 使用在initGLSurfaceView中设置的渲染器实例
                GLRenderer renderer = (GLRenderer) glSurfaceView.getTag();
                if (renderer != null) {
                    renderer.updateDistortionParams(params[0], params[1]);
                }
            });
        }
    };
    
    // 解析视频帧中的畸变校正参数
    private float[] parseDistortionParams(byte[] frameData) {
        // 实际项目中需要根据协议解析帧数据中的参数
        // 这里简单返回默认值作为示例
        return new float[]{0.25f, 0.05f};
    }
}