#!/bin/bash

echo "🚀 CorelDRAW 插件 - 快速发布脚本"
echo "=================================="
echo ""

# 检查是否在git仓库中
if [ ! -d .git ]; then
    echo "❌ 错误: 当前目录不是Git仓库"
    echo "请先运行: git init"
    exit 1
fi

# 检查是否有未提交的更改
if [[ -n $(git status -s) ]]; then
    echo "📝 检测到未提交的更改:"
    git status -s
    echo ""
    read -p "是否提交所有更改? (y/n): " commit_changes

    if [ "$commit_changes" = "y" ]; then
        read -p "请输入提交信息: " commit_msg
        git add .
        git commit -m "$commit_msg"
        echo "✓ 更改已提交"
    else
        echo "⚠ 请先提交更改后再发布"
        exit 1
    fi
fi

echo ""
read -p "请输入版本号 (例如: 1.0.0): " VERSION

if [ -z "$VERSION" ]; then
    echo "❌ 版本号不能为空"
    exit 1
fi

# 检查标签是否已存在
if git rev-parse "v$VERSION" >/dev/null 2>&1; then
    echo "❌ 错误: 标签 v$VERSION 已存在"
    echo "请使用不同的版本号或删除旧标签: git tag -d v$VERSION"
    exit 1
fi

echo ""
echo "📦 准备发布版本: v$VERSION"
echo "=================================="
echo ""

# 确认发布
read -p "确认发布 v$VERSION 吗? (y/n): " confirm

if [ "$confirm" != "y" ]; then
    echo "❌ 已取消发布"
    exit 0
fi

echo ""
echo "[1/3] 创建标签..."
git tag -a "v$VERSION" -m "Release version $VERSION"
echo "✓ 标签 v$VERSION 已创建"

echo ""
echo "[2/3] 推送到远程仓库..."
git push origin main
git push origin "v$VERSION"

if [ $? -eq 0 ]; then
    echo "✓ 成功推送到GitHub"
else
    echo "❌ 推送失败,请检查网络连接和仓库权限"
    echo "如需重试,请先删除本地标签: git tag -d v$VERSION"
    exit 1
fi

echo ""
echo "[3/3] 完成!"
echo "=================================="
echo "✓ 版本 v$VERSION 发布成功!"
echo ""
echo "📋 后续步骤:"
echo "  1. GitHub Actions 正在自动编译插件..."
echo "  2. 预计3-5分钟后完成编译"
echo "  3. 访问以下页面查看进度和下载:"
echo ""
echo "     https://github.com/YOUR_USERNAME/YOUR_REPO/actions"
echo "     https://github.com/YOUR_USERNAME/YOUR_REPO/releases"
echo ""
echo "  4. 编译完成后,下载 CorelDRAW-AutoIgnoreError-Plugin.zip"
echo "  5. 将ZIP文件发送给用户即可"
echo ""
echo "=================================="
