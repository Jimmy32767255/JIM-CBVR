package com.jimcbvr.client;

import android.opengl.GLSurfaceView;
import android.os.Bundle;
import androidx.appcompat.app.AppCompatActivity;

public class MainActivity extends AppCompatActivity {
    private GLSurfaceView glSurfaceView;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        initGLSurfaceView();
        setupNetworkConnection();
    }

    private void initGLSurfaceView() {
        glSurfaceView = new GLSurfaceView(this);
        glSurfaceView.setEGLContextClientVersion(3);
        glSurfaceView.setRenderer(new GLRenderer());
        setContentView(glSurfaceView);
    }

    private void setupNetworkConnection() {
        // 双通道连接初始化逻辑
        new Thread(() -> {
            // 网络连接实现
        }).start();
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
        public void onSurfaceChanged(javax.microedition.khronos.opengles.GL10 gl, int width, int height) {
            // 画面尺寸变化处理
        }

        @Override
        public void onDrawFrame(javax.microedition.khronos.opengles.GL10 gl) {
            // 每帧渲染逻辑
        }
    }

    // 在MainActivity中添加参数更新回调
    private NetworkManager.NetworkCallback networkCallback = new NetworkManager.NetworkCallback() {
        @Override
        public void onFrameReceived(byte[] frameData) {
            // 解析视频帧中的矫正参数（需要与服务端协议保持一致）
            float[] params = parseDistortionParams(frameData);
            glSurfaceView.queueEvent(() -> {
                ((GLRenderer)glSurfaceView.getRenderer()).updateDistortionParams(params[0], params[1]);
            });
        }
    };
}