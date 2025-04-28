0# JIM-CBVR

## 一个自由，开放的VR串流解决方案

## 当前功能：

### Windows服务端：
 - [x] WPF配置界面(含JSON序列化)
 - [x] SteamVR连接核心模块
 - [x] 智能端口管理(自动冲突检测)
 - [x] 画面矫正参数配置界面
 - [x] 双模式网络服务(USB/WiFi)

### Android客户端：
 - [x] OpenGL ES 3.0渲染管线
 - [x] WebSocket双通道连接
 - [x] 动态畸变参数更新
 - [x] 心跳检测机制

## 为什么创建它？

### 因为RiftCat VRidge收费，Trinus VR又不好用，而且不美观，所以我决定自己做一个！

## 现在能用吗？

### 很遗憾，不！它正在制作中，可用时，我们将会发布releases。

## 环境要求

### Windows服务端
- Windows 10/11 64位操作系统
- .NET 6.0 SDK或更高版本
- SteamVR
- Visual Studio 2022或更高版本（用于开发）

### Android客户端
- Android 8.0 (API 26)或更高版本
- 支持OpenGL ES 3.0的设备
- Android Studio（用于开发）

## 编译与构建

### Windows服务端

1. **克隆仓库**
   ```
   git clone https://github.com/your-username/JIM-CBVR.git
   cd JIM-CBVR
   ```

2. **使用Visual Studio打开项目**
   - 打开Visual Studio 2022
   - 选择「打开项目或解决方案」
   - 导航到`Windows-Server`文件夹并选择`JIMCBVR.Server.csproj`

3. **构建项目**
   - 在Visual Studio中选择「生成」>「生成解决方案」(F6)
   - 或使用命令行：
     ```
     dotnet build Windows-Server/JIMCBVR.Server.csproj -c Release
     ```

4. **运行应用**
   - 在Visual Studio中按F5运行
   - 或从`bin/Release/net6.0-windows`文件夹中运行生成的可执行文件

### Android客户端

1. **使用Android Studio打开项目**
   - 打开Android Studio
   - 选择「打开已有项目」
   - 导航到`Android-Client`文件夹并打开

2. **构建项目**
   - 等待Gradle同步完成
   - 选择「Build」>「Build Bundle(s) / APK(s)」>「Build APK(s)」
   - 或使用命令行：
     ```
     cd Android-Client
     ./gradlew assembleDebug
     ```

3. **安装到设备**
   - 通过USB连接Android设备（确保已启用开发者选项和USB调试）
   - 在Android Studio中选择「Run」>「Run 'app'」
   - 或手动安装APK：
     ```
     adb install app/build/outputs/apk/debug/app-debug.apk
     ```

## 使用指南

### Windows服务端配置

1. **启动SteamVR**
   - 确保SteamVR已安装并正常运行

2. **启动JIM-CBVR服务端**
   - 运行JIMCBVR.Server.exe

3. **配置连接设置**
   - 在「网络设置」选项卡中设置连接模式（USB/WiFi）
   - 如有需要，调整端口设置（程序会自动检测端口冲突）

4. **配置画面矫正参数**
   - 在「画面矫正」选项卡中调整畸变参数
   - 可根据不同设备调整FOV、IPD等参数

5. **启动服务**
   - 点击「启动服务」按钮开始串流

### Android客户端使用

1. **安装APK**
   - 从构建步骤中获取APK并安装到Android设备

2. **连接设置**
   - 启动应用后，输入Windows服务端的IP地址（WiFi模式）
   - 或选择USB连接模式（需要ADB支持）

3. **连接服务器**
   - 点击「连接」按钮
   - 等待连接成功提示

4. **进入VR模式**
   - 连接成功后，将手机放入VR眼镜
   - 开始体验VR内容