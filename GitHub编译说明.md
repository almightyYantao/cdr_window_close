# GitHub Actions 编译问题说明

## 问题说明

原始版本的 `CorelDrawPlugin.cs` 使用了CorelDRAW的COM类型库引用,这在GitHub Actions中无法编译,因为:

1. GitHub Actions使用.NET Core SDK的MSBuild
2. .NET Core MSBuild不支持`ResolveComReference`任务
3. CorelDRAW的类型库在GitHub Actions环境中不存在

## 解决方案

项目现在包含两个版本的插件主类:

### 1. CorelDrawPlugin.cs (原始版本)
- ✅ 完整的CorelDRAW API集成
- ✅ 可以访问CorelDRAW应用程序对象
- ✅ 可以监听文档事件
- ❌ 依赖CorelDRAW COM类型库
- ❌ 无法在GitHub Actions中编译
- 💡 适合本地开发和测试

### 2. CorelDrawPluginSimple.cs (简化版本,默认使用)
- ✅ 可以在GitHub Actions中编译
- ✅ 使用标准COM互操作
- ✅ 核心功能(窗口监控)完全正常
- ⚠️ 不能直接访问CorelDRAW API
- ⚠️ 不能监听文档事件
- 💡 适合自动化编译和发布

## 功能对比

| 功能 | 原始版本 | 简化版本 |
|------|---------|---------|
| 自动检测错误对话框 | ✅ | ✅ |
| 自动点击"忽略"按钮 | ✅ | ✅ |
| 后台监控运行 | ✅ | ✅ |
| GitHub Actions编译 | ❌ | ✅ |
| CorelDRAW事件监听 | ✅ | ❌ |
| 文档打开前激活监控 | ✅ | ⚠️ 持续运行 |

## 使用说明

### 对于用户
**没有区别!** 简化版本的核心功能(自动关闭错误对话框)与原始版本完全相同。

### 对于开发者

#### 使用GitHub Actions自动编译(推荐)
项目默认配置使用简化版本,可以直接运行:
```bash
./release.sh
```

#### 本地开发时使用完整版本

如果你在Windows上开发,想使用CorelDRAW API:

1. 修改 `CorelDrawAutoIgnoreError.csproj`,移除排除规则:
```xml
<ItemGroup>
  <!-- 注释或删除这两行 -->
  <!-- <Compile Remove="CorelDrawPlugin.cs" /> -->
  <!-- <None Include="CorelDrawPlugin.cs" /> -->

  <!-- 添加这行排除简化版本 -->
  <Compile Remove="CorelDrawPluginSimple.cs" />
</ItemGroup>
```

2. 添加CorelDRAW类型库引用(在Visual Studio中):
   - 项目 → 添加引用 → COM → CorelDRAW XX.X Type Library

3. 本地编译测试

## 技术细节

### 简化版本的工作原理

虽然简化版本不能监听CorelDRAW事件,但它的工作方式非常有效:

1. **插件加载**: CorelDRAW加载COM组件时调用`OnConnection()`
2. **启动监控**: 立即启动`ErrorDialogMonitor`,持续运行
3. **后台监控**: 独立线程每100ms检查一次错误对话框
4. **自动处理**: 检测到错误时立即点击"忽略"

相比原始版本的"事件驱动"方式,简化版本采用"持续监控"方式,实际效果相同。

### 性能影响

- 监控线程使用极少CPU(每100ms唤醒一次)
- 内存占用约1-2MB
- 对CorelDRAW性能影响可以忽略不计

## 推荐配置

✅ **推荐**: 使用默认配置(简化版本)
- 可以自动编译
- 功能完全满足需求
- 维护更简单

⚠️ **可选**: 切换到原始版本
- 仅当需要扩展功能时
- 需要手动在Windows上编译
- 需要CorelDRAW开发环境

## 常见问题

**Q: 简化版本功能会不会打折扣?**
A: 不会。核心的错误对话框自动处理功能完全相同。

**Q: 为什么不在GitHub Actions中安装CorelDRAW?**
A: CorelDRAW是商业软件,无法在CI环境中合法安装和使用。

**Q: 能否手动上传在Windows编译的版本?**
A: 可以!但GitHub Actions更方便,推荐使用简化版本自动编译。

**Q: 如果我以后需要添加CorelDRAW API功能怎么办?**
A: 可以切换回原始版本,但需要在Windows上手动编译和发布。
