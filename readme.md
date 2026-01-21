# FallbackDriver - 高性能数据补偿框架

`FallbackDriver` 是一个基于 **.NET 8** 开发的高性能数据补偿框架，专为工业物联网（IIoT）环境下的数据采集链路而设计。它作为数据获取的“第二道防线”，在主驱动程序（Primary Driver）因网络抖动、超时或协议错误导致数据缺失时，自动触发补偿逻辑，从备用源（如 InSQL、Historian、CSV、API 等）检索并补全历史数据。

## 1. 设计思路与核心理念

本框架的设计目标是 **高可靠性**、**高扩展性** 以及 **资源利用最大化**。

### 1.1 生产者-消费者模型 (Channels)
系统核心采用 `System.Threading.Channels` 实现内存中的生产者-消费者模型。
- **生产者 (Producers)**：当主驱动检测到数据读取失败（如断开连接、响应为空等）时，生成 `FallbackTask` 并推送到 Channel。
- **消费者 (Consumers)**：后台服务 `FallbackBackgroundService` 持续监听 Channel，根据配置分发任务给对应的补偿驱动。

### 1.2 补偿源映射与解析 (Source Resolving)
支持灵活的映射逻辑：
- **驱动级补偿**：整个驱动实例失效时，对该实例下所有标签进行全局补偿。
- **标签级补偿**：仅针对特定读取失败的标签进行精确补偿。
- **动态解析**：通过 `IFallbackSourceResolver` 自动寻找驱动或标签对应的补偿源 ID。

### 1.3 任务合并策略 (Task Merging)
为了降低对补偿源（如数据库或外部系统）的 IO 压力，框架实现了高效的任务合并机制：
- 针对相同补偿源、相同时间范围或重叠时间段的多个任务，自动合并为单次批量读取请求。

---

## 2. 关键接口说明

### 2.1 `IFallbackDriver` (补偿驱动实现)
这是扩展框架以支持新补偿源（如新的数据库或文件格式）的核心接口。
```csharp
public interface IFallbackDriver
{
    string InstanceId { get; } // 补偿源唯一标识
    string DriverType { get; } // 驱动类型标识
    Task<IEnumerable<TagValue>> ReadAsync(FallbackReadContext context, CancellationToken ct);
}
```

### 2.2 `IFallbackTaskProducer` (任务生成器)
负责在业务逻辑中检测故障并转化为标准化的补偿任务。
```csharp
public interface IFallbackTaskProducer
{
    // 创建驱动级或多标签级任务
    ValueTask CreateTaskAsync(string driverId, DateTime startTime, DateTime endTime, IReadOnlyList<string>? tagIds = null, CancellationToken ct = default);
}
```

### 2.3 `IFallbackSourceResolver` (源解析器)
定义了如何根据原始 `DriverId` 找到其对应的补偿路径。
```csharp
public interface IFallbackSourceResolver
{
    Task<string?> ResolveSourceIdAsync(string driverId, CancellationToken ct = default);
    Task<IDictionary<string, List<DriverTag>>> GroupTagsBySourceAsync(string driverId, IEnumerable<string> tagIds, CancellationToken ct = default);
}
```

### 2.4 `IFallbackService` (调度核心)
内部服务的入口，负责将 `FallbackTask` 投递到处理队列中。

---

## 3. 核心数据模型

### `FallbackTask`
描述补偿任务的 DTO：
- `DriverId`: 原始驱动标识。
- `FallbackSourceId`: 路由到的补偿驱动标识。
- `StartTime` / `EndTime`: 时间范围。
- `Tags`: 可选。若为空则代表全驱动补偿。

### `TagValue`
补偿结果的标准载体，包含标签名、值、时间戳及质量码。

---

## 4. 如何运行与测试

### 构建
```bash
dotnet build
```

### 运行测试
项目包含完整的单元测试与集成测试：
```bash
dotnet test
```

## 5. 待办事项与路线图
- [ ] 支持基于优先级（Priority）的任务调度。
- [ ] 增加补偿结果的持久化重试机制。
- [ ] 增加针对 CSV/Excel 等文件源的内置驱动实现。
