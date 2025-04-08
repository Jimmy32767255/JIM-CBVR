package com.jimcbvr.client;

import android.content.Context;
import android.util.Log;
import org.java_websocket.client.WebSocketClient;
import org.java_websocket.handshake.ServerHandshake;
import java.net.URI;
import java.net.URISyntaxException;
import java.nio.ByteBuffer;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.TimeUnit;

/**
 * 网络连接管理器，支持WiFi/USB双通道自动切换
 */
public class NetworkManager {
    private static final String TAG = "NetworkManager";

    private WebSocketClient wsClient;
    private final NetworkCallback callback;

    public NetworkManager(Context context, NetworkCallback callback) {
        this.callback = callback;
    }

    /**
     * 启动自动连接（优先尝试USB调试模式）
     * @param serverPort 服务端主端口
     */
    public void startConnection(int serverPort) {
        new Thread(() -> {
            // 先尝试USB连接
            if(attemptUSBMode(serverPort)) return;

            // 失败后尝试无线连接
            attemptWifiMode(serverPort);
        }).start();
    }

    private boolean attemptUSBMode(int port) {
        try {
            // USB调试模式特殊处理
            URI usbUri = new URI("ws://localhost:" + port);
            setupWebSocket(usbUri);
            return true;
        } catch (Exception e) {
            Log.e(TAG, "USB连接失败", e);
            return false;
        }
    }

    private void attemptWifiMode(int port) {
        // 实现无线网络发现逻辑
        // （此处需添加SSDP/UDP广播发现服务端IP的逻辑）
        Log.d(TAG, "尝试无线连接模式，端口：" + port);
        
        // 简单实现：尝试连接本地网络中可能的服务器IP
        // 在实际应用中，应该使用网络发现协议如SSDP或mDNS
        try {
            // 这里假设服务器在同一网络的192.168.1.x子网中
            // 实际应用中应该动态发现服务器IP
            URI wifiUri = new URI("ws://192.168.1.100:" + port);
            setupWebSocket(wifiUri);
        } catch (URISyntaxException e) {
            Log.e(TAG, "无线连接URI创建失败", e);
            callback.onError("无线连接配置错误");
        }
    }

    private void setupWebSocket(URI uri) {
        wsClient = new WebSocketClient(uri) {
            @Override
            public void onOpen(ServerHandshake handshakedata) {
                callback.onConnected();
                startHeartbeat();
            }

            @Override
            public void onMessage(String message) {
                // 处理文本协议消息
                Log.d(TAG, "收到文本消息: " + message);
            }

            @Override
            public void onMessage(ByteBuffer bytes) {
                // 接收视频流数据帧
                callback.onFrameReceived(bytes.array());
            }

            @Override
            public void onClose(int code, String reason, boolean remote) {
                callback.onDisconnected(reason);
            }

            @Override
            public void onError(Exception ex) {
                callback.onError(ex.getMessage());
            }
        };

        try {
            wsClient.connectBlocking();
        } catch (InterruptedException e) {
            Log.e(TAG, "连接被中断", e);
        }
    }

    private void startHeartbeat() {
        final AtomicBoolean running = new AtomicBoolean(true);
        Thread heartbeatThread = new Thread(() -> {
            while (running.get() && wsClient != null && wsClient.isOpen()) {
                try {
                    TimeUnit.SECONDS.sleep(3); // 使用TimeUnit代替直接调用Thread.sleep
                    if (wsClient != null && wsClient.isOpen()) {
                        wsClient.sendPing();
                    }
                } catch (InterruptedException e) {
                    Log.d(TAG, "心跳线程被中断");
                    running.set(false);
                    break;
                }
            }
            Log.d(TAG, "心跳线程结束");
        });
        heartbeatThread.setDaemon(true); // 设置为守护线程，不阻止JVM退出
        heartbeatThread.start();
    }

    /**
     * 关闭网络连接
     */
    public void closeConnection() {
        if (wsClient != null) {
            try {
                wsClient.close();
            } catch (Exception e) {
                Log.e(TAG, "关闭WebSocket连接失败", e);
            }
        }
    }

    public interface NetworkCallback {
        void onConnected();
        void onDisconnected(String reason);
        void onError(String message);
        void onFrameReceived(byte[] frameData);
    }
}