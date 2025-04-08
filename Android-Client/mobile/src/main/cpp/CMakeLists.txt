cmake_minimum_required(VERSION 3.22.1)

project(jimcbvr-native)

# 添加日志库依赖
find_library(log-lib log)

# 添加OpenGL ES库依赖
find_library(glesv3-lib GLESv3)

# 创建native库
add_library(
        jimcbvr-native
        SHARED
        jimcbvr-native.cpp
)

# 链接库
target_link_libraries(
        jimcbvr-native
        ${log-lib}
        ${glesv3-lib}
)