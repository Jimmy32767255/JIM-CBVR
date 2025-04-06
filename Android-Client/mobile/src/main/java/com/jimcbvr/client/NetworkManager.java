package com.jimcbvr.client;

import android.content.Context;
import android.util.Log;
import org.java_websocket.client.WebSocketClient;
import org.java_websocket.handshake.ServerHandshake;
import java.net.URI;
import java.nio.ByteBuffer;

/**
 * 网络连接管理器，支持WiFi/USB双通道自动切换
 */
public class NetworkManager {
    private static final String TAG = "NetworkManager";

    private WebSocketClient wsClient;
    private final Context context;
    private NetworkCallback callback;

    public NetworkManager(Context context, NetworkCallback callback) {
        this.context = context;
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
        new Thread(() -> {
            while (wsClient.isOpen()) {
                try {
                    Thread.sleep(3000);
                    wsClient.sendPing();
                } catch (InterruptedException e) {
                    break;
                }
            }
        }).start();
    }

    public interface NetworkCallback {
        void onConnected();
        void onDisconnected(String reason);
        void onError(String message);
        void onFrameReceived(byte[] frameData);
    }
}