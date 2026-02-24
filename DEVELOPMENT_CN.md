# GhostEngine 开发指南

欢迎参与 GhostEngine 项目。本文档旨在帮助你快速了解项目的当前状态、架构设计、开发规范及环境配置。

## 1. 开发环境要求 (Prerequisites)

在开始开发之前，请确保你的开发环境满足以下要求：

- **操作系统**: Windows 10 版本 1809 (17763) 或更高版本。
- **IDE**: Visual Studio 2022 (建议使用最新预览版以支持 .NET 10)。
- **.NET SDK**: .NET 10.0 SDK。
- **Windows App SDK**: 项目目前使用 Windows App SDK (WinUI 3) 版本 1.8.260101001。
- **显卡**: 支持 DirectX 12 (Feature Level 11.0+) 的显卡。
- **Visual Studio 工作负载**:
  - .NET 桌面开发
  - 使用 C++ 的桌面开发 (部分第三方库包装需要)
  - 通用 Windows 平台开发 (用于 Windows App SDK 支持)

## 2. 项目结构 (Project Structure)

项目代码主要位于 `src` 目录下，按功能分为三个主要部分：

### 2.1 Runtime (引擎运行时)
- **Ghost.Graphics**: 图形渲染核心。包含 RHI (渲染硬件接口) 定义及其 D3D12 实现。集成了 Render Graph (渲染图) 模块用于管理复杂的渲染流水线。
- **Ghost.Entities**: 基于 Archetype (原型) 的高性能 ECS (实体组件系统) 实现。包含 `World`, `EntityManager`, `EntityQuery` 等核心组件。
- **Ghost.Engine**: 引擎基础逻辑。包含场景管理 (`Scene`)、基础组件 (如 `Hierarchy`, `LocalToWorld`) 及数学工具类。

### 2.2 Editor (编辑器)
- **Ghost.Editor**: 基于 WinUI 3 编写的桌面编辑器前端。采用 MVVM 架构。
- **Ghost.Editor.Core**: 编辑器框架层。包含资源管理 (`AssetRegistry`)、检查器服务 (`InspectorService`)、场景树管理 (`SceneGraph`) 等非 UI 逻辑。
- **Ghost.DSL**: 引擎自定义着色器语言 (GSL) 的编译器和解析器，用于处理着色器代码生成。

### 2.3 ThirdParty (第三方库)
- 包含对 FMOD (音频)、Nvtt (纹理压缩)、MeshOptimizer (模型优化) 等 C++ 库的 C# 包装。

## 3. 命名规范 (Naming Conventions)

项目严格遵守以下 C# 编程规范：

- **命名空间**: 以 `Ghost.` 开头，后跟模块名 (例如 `Ghost.Graphics.D3D12`)。
- **类与方法**: 使用 `PascalCase` (大驼峰命名法)。
- **私有字段**: 使用 `_camelCase` (下划线前缀的小驼峰命名法)。
- **接口**: 必须以 `I` 作为前缀 (例如 `IRenderDevice`)。
- **异步方法**: 必须以 `Async` 作为后缀 (例如 `LoadAssetAsync`)。
- **实现类**: 针对接口的具体实现应体现技术细节 (例如 `D3D12GraphicsEngine` 实现了 `IGraphicsEngine`)。

## 4. 架构设计与模式 (Architecture & Patterns)

### 4.1 ECS (Entity Component System)
运行时逻辑优先使用 ECS 模式以获得最佳的缓存命中率和并行性能：
- **Entity**: 纯 ID。
- **Component**: 纯数据结构 (Struct)。
- **System**: 处理特定组件组合的逻辑类。

### 4.2 RHI & Render Graph
- **RHI (Render Hardware Interface)**: 抽象了底层的图形 API。目前主要实现为 D3D12，通过 Vortice.Windows 进行绑定。
- **Render Graph**: 采用有向无环图 (DAG) 管理每一帧的渲染顺序、资源屏障 (Resource Barriers) 和资源复用，简化了 D3D12 的资源管理负担。

### 4.3 MVVM & Service Pattern (Editor)
编辑器部分遵循：
- **MVVM**: 分离 UI (`View`) 与业务逻辑 (`ViewModel`)。
- **Service/Contract**: 通过接口定义服务 (如 `IAssetRegistry`)，并利用依赖注入 (Dependency Injection) 进行解耦。

### 4.4 核心设计原则 (Core Design Principles)
- **Result over Exception (结果对象胜于异常)**: 在核心运行时中，避免使用 `throw` 处理预期错误，优先返回 `Ghost.Core.Result` 结构体，以提高性能和错误流的可控性。
- **Handle over Ptr/Reference (句柄胜于指针/引用)**: 资源引用（如实体、纹理、缓冲区）应优先使用 `Handle<T>`，避免持有直接的内存地址，以确保资源管理的安全性并支持高效序列化。

## 5. 核心逻辑当前状态

- **渲染系统**: 已具备基础的 D3D12 渲染器、Render Graph 编译器及简单的 Mesh 渲染 pass。支持自定义着色器加载。
- **实体系统**: 已实现 Archetype-based ECS，支持高性能的实体查询 (`EntityQuery`) 和作业系统迭代。
- **资源管理**: 编辑器端已实现初步的资产数据库 (`AssetRegistry`)，支持纹理和模型的导入流程。
- **场景管理**: 支持基础的层级结构 (`Hierarchy`) 和变换同步 (`LocalToWorld`)。

---
*注：关于后续的开发计划 (TODO) 及改进方向，我将直接通过会议或即时通讯工具进行沟通。*
