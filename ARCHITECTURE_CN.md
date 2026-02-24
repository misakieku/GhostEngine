# GhostEngine 架构详解

本文档深入探讨 GhostEngine 核心模块的设计实现，为协同开发提供详细的技术参考。

## 1. 实体组件系统 (Ghost.Entities)

Ghost.Entities 采用 Archetype (原型) 模式，旨在优化大规模实体的遍历效率和内存布局。

### 1.1 核心组件
- **World**: 包含实体生命周期管理的顶层容器，包含 `EntityManager`。
- **Archetype**: 定义了一组特定组件的组合。具有相同组件集的所有实体都存储在同一 Archetype 中。
- **Chunk**: Archetype 内部的数据块，按列存储 (Columnar Storage) 组件数据，以确保对单个组件的线性访问具备极佳的 CPU 缓存亲和性。
- **EntityQuery**: 通过位掩码 (Bitmask) 快速匹配 Archetype，支持 `WithAll`, `WithAny`, `WithNone` 过滤规则。

### 1.2 并行处理
- 实体查询支持多线程作业系统 (Job System) 迭代。
- **EntityCommandBuffer (ECB)**: 允许在并行作业中排队待处理的实体修改请求 (如创建、删除、添加组件)，并在单线程同步点进行统一应用，以避免竞态条件。

## 2. 渲染架构 (Ghost.Graphics)

渲染系统设计目标是支持高效、现代的图形渲染流水线，同时降低 D3D12 的开发复杂度。

### 2.1 RHI (渲染硬件接口)
RHI 位于 `Ghost.Graphics.RHI` 命名空间下，抽象了底层的渲染资源：
- **IRenderDevice**: 逻辑渲染设备。
- **ICommandBuffer**: 用于录制 GPU 指令。
- **IPipelineLibrary**: 缓存并管理渲染管线状态对象 (PSO)。
- **IResourceDatabase**: 管理显存资源 (Buffer, Texture) 及其生命周期。

### 2.2 Render Graph (渲染图)
Render Graph 是图形模块的核心组件，负责帧内资源的依赖分析和自动调度：
- **Pass Builder**: 在帧开始阶段，每个 Pass 声明其读取 (`Read`) 和写入 (`Write`) 的资源。
- **Resource Aliasing**: 自动识别不重叠的资源生命周期，实现显存的物理地址复用。
- **Automatic Barrier**: 自动根据资源的读写关系插入 `ResourceBarrier` (例如从 `RenderTarget` 状态转换到 `PixelShaderResource` 状态)。

### 2.3 自定义着色器语言 (Ghost.DSL)
引擎支持 `.gshdr` 文件，其语法借鉴了 HLSL 但增强了元数据支持：
- **自动属性映射**: DSL 编译器会解析着色器定义的属性，并自动生成与之匹配的 C# 结构体 (`ShaderStructGenerator`)，简化 CPU 到 GPU 的数据传递。

## 3. 编辑器架构 (Ghost.Editor)

编辑器框架采用模块化设计，重点在于扩展性和资源工作流。

### 3.1 资源数据库 (Asset Database)
- **AssetRegistry**: 通过资源文件的 GUID 进行追踪，支持跨文件引用的完整性校验。
- **AssetProcessor**: 插件化系统，针对不同文件扩展名 (如 `.png`, `.fbx`) 提供特定的导入和转换逻辑 (如调用 Nvtt 压缩纹理)。

### 3.2 检查器 (Inspector)
- **Service-driven**: `InspectorService` 动态检测当前选中实体的组件列表，并根据组件类型匹配对应的 `ComponentEditor` 进行 UI 渲染。
- **Data Binding**: 利用 `ReflectionBinding` 实现编辑器 UI 与运行时组件数据的双向同步。

## 4. 核心设计原则 (Core Design Principles)

项目在底层代码中遵循以下核心设计原则，以确保高性能和系统稳定性：

### 4.1 Result over Exception (结果对象胜于异常)
在引擎的核心运行时（尤其是 `Ghost.Graphics` 和 `Ghost.Entities`）中，我们避免使用异常来处理预期的错误。
- **Result 结构体**: 使用 `Ghost.Core.Result` 或 `Result<T>` 结构体返回操作结果。
- **性能**: 避免了异常产生的堆栈跟踪开销。
- **显性处理**: 强制调用者检查 `IsSuccess` 或使用 `Deconstruct` 模式处理错误，使错误流更加清晰。

### 4.2 Handle over Ptr/Reference (句柄胜于指针/引用)
为了内存安全和支持序列化，引擎广泛使用句柄而非直接的内存指针。
- **Handle<T>**: 资源（如 `Texture`, `Buffer`, `Entity`）通过强类型句柄进行引用。
- **安全性**: 防止野指针问题，支持资源的延迟加载和卸载，而不破坏引用。
- **解耦**: 句柄作为后端资源的索引，使得底层的资源管理器（如 `D3D12ResourceDatabase`）可以自由地重新分配或移动物理资源。

## 5. 协同开发建议


- **跨项目引用**: 尽量避免 `Runtime` 项目引用 `Editor` 项目。`Editor` 应依赖 `Runtime` 以提供实时预览。
- **性能关键点**: 在 `Ghost.Entities` 和 `Ghost.Graphics` 模块中，应尽量避免堆内存分配 (GC Allocation)，优先使用 `Span<T>`, `Memory<T>` 及非托管内存。

---
*注：本架构基于当前代码实现，如有变更将及时更新文档。*
