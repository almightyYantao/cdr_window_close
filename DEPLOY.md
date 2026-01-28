# 发布和部署指南

## 📦 为用户打包插件(Mac环境)

### 方法1: 使用GitHub Actions自动编译(推荐)

这是最简单的方法,无需Windows环境,完全在Mac上操作:

#### 步骤1: 推送代码到GitHub

```bash
# 在Mac上,进入项目目录
cd /Users/yantao/Documents/yantao/yantao_code/cdr_window_close

# 初始化git仓库(如果还没有)
git init

# 添加所有文件
git add .

# 提交
git commit -m "Initial commit: CorelDRAW Auto Ignore Error Plugin"

# 关联远程仓库(替换为你的GitHub仓库地址)
git remote add origin https://github.com/YOUR_USERNAME/cdr-auto-ignore-error.git

# 推送到GitHub
git push -u origin main
```

#### 步骤2: 创建Release触发自动编译

在Mac上继续操作:

```bash
# 创建版本标签
git tag v1.0.0

# 推送标签到GitHub
git push origin v1.0.0
```

GitHub Actions会自动:
1. ✓ 在Windows环境中编译插件
2. ✓ 创建部署包
3. ✓ 打包成ZIP文件
4. ✓ 创建GitHub Release
5. ✓ 上传 `CorelDRAW-AutoIgnoreError-Plugin.zip`

#### 步骤3: 下载分发包

1. 访问你的GitHub仓库的Releases页面
2. 下载 `CorelDRAW-AutoIgnoreError-Plugin.zip`
3. 将这个ZIP文件发送给用户

### 方法2: 在Windows虚拟机或远程Windows上编译

如果你有Windows虚拟机或远程Windows服务器:

#### 在Windows上操作:

```batch
# 1. 克隆或复制项目到Windows
git clone https://github.com/YOUR_USERNAME/cdr-auto-ignore-error.git
cd cdr-auto-ignore-error

# 2. 运行编译脚本(不需要管理员权限)
build.bat

# 3. 获取生成的ZIP文件
# 文件位置: CorelDRAW-AutoIgnoreError-Plugin.zip
```

### 方法3: 使用远程Windows编译服务

可以使用在线的Windows编译服务,例如:
- GitHub Actions(推荐,免费)
- AppVeyor
- Azure DevOps

## 👥 用户安装流程

用户收到ZIP文件后,安装非常简单:

### 用户操作步骤:

1. **解压ZIP文件**
   - 解压 `CorelDRAW-AutoIgnoreError-Plugin.zip` 到任意文件夹
   - 例如: `C:\CorelDRAW插件\`

2. **运行安装程序**
   - 右键点击 `install.bat`
   - 选择 **"以管理员身份运行"**
   - 等待几秒,看到"安装成功"提示

3. **启动CorelDRAW**
   - 启动或重启CorelDRAW
   - 应该看到提示: "CorelDRAW自动忽略错误插件已加载"

4. **完成**
   - 插件已激活,会自动处理错误对话框

## 🔄 更新插件

当需要发布新版本时:

### 在Mac上:

```bash
# 修改代码后
git add .
git commit -m "Update: 修复某个问题"
git push

# 创建新版本
git tag v1.0.1
git push origin v1.0.1
```

GitHub Actions会自动编译并创建新的Release。

### 用户更新:

1. 先运行 `uninstall.bat` 卸载旧版本
2. 下载新版本ZIP
3. 运行新版本的 `install.bat`
4. 重启CorelDRAW

## 📋 分发清单

给用户的ZIP包应包含:

```
CorelDRAW-AutoIgnoreError-Plugin.zip
├── CorelDrawAutoIgnoreError.dll  ← 插件主文件
├── install.bat                   ← 安装脚本
├── uninstall.bat                 ← 卸载脚本
├── 使用说明.txt                  ← 简明使用说明
└── README.md                     ← 详细文档
```

## 🛠️ 本地测试(在Windows上)

如果你需要在Windows上测试:

```batch
# 1. 编译
dotnet build CorelDrawAutoIgnoreError.csproj -c Release

# 2. 手动注册COM(以管理员权限)
regasm /codebase bin\Release\net472\CorelDrawAutoIgnoreError.dll

# 3. 启动CorelDRAW测试

# 4. 测试完成后卸载
regasm /unregister bin\Release\net472\CorelDrawAutoIgnoreError.dll
```

## ⚙️ 自定义配置

### 修改GUID(重要!)

在首次发布前,必须生成唯一的GUID:

**在Mac上生成GUID:**

```bash
# 使用uuidgen命令
uuidgen
# 输出示例: A1B2C3D4-E5F6-7890-ABCD-EF1234567890
```

**在Windows上生成GUID:**

```powershell
[Guid]::NewGuid()
```

然后在 `CorelDrawPlugin.cs` 中替换:

```csharp
[Guid("YOUR-NEW-GUID-HERE")] // 第11行
```

### 修改版本号

在多个地方需要更新版本号:

1. `README.md` - 更新日志部分
2. `installer/使用说明.txt` - 版本信息部分
3. Git标签 - `git tag v1.0.1`

## 🚀 快速发布命令(Mac)

创建一个快速发布脚本 `release.sh`:

```bash
#!/bin/bash

echo "🚀 开始发布新版本..."

# 读取版本号
read -p "请输入版本号(例如: 1.0.1): " VERSION

# 提交所有更改
git add .
git commit -m "Release v${VERSION}"

# 创建标签
git tag "v${VERSION}"

# 推送
git push origin main
git push origin "v${VERSION}"

echo "✓ 版本 v${VERSION} 已推送到GitHub"
echo "✓ GitHub Actions 正在自动编译..."
echo "✓ 请在几分钟后检查 GitHub Releases 页面"
```

使用方法:

```bash
chmod +x release.sh
./release.sh
```

## 📊 发布检查清单

发布前确认:

- [ ] 代码已测试
- [ ] GUID已更改为唯一值
- [ ] 版本号已更新
- [ ] README和使用说明已更新
- [ ] GitHub Actions workflow配置正确
- [ ] 在Windows上测试过(如果可能)

## 🎯 给用户的简化说明

你可以给用户发送这样的消息:

```
【CorelDRAW 自动忽略错误插件 - 安装说明】

1. 下载: [附件] CorelDRAW-AutoIgnoreError-Plugin.zip

2. 解压到任意文件夹

3. 右键点击 "install.bat" → "以管理员身份运行"

4. 重启CorelDRAW

5. 完成! 以后打开CDR文件遇到错误会自动忽略

【遇到问题?】
打开文件夹里的"使用说明.txt"查看详细说明
```

## 💡 提示

- **Mac用户**: 推荐使用GitHub Actions自动编译,完全不需要Windows环境
- **快速迭代**: 每次修改后推送标签即可触发自动编译
- **版本管理**: 使用语义化版本号(1.0.0, 1.0.1, 1.1.0等)
- **用户友好**: ZIP包内包含详细的使用说明
- **一键安装**: 用户只需双击install.bat即可
