// GdiHook.cpp - GDI文本绘制Hook
#include <windows.h>
#include <string>
#include <fstream>
#include <sstream>
#include "MinHook.h"

#pragma comment(lib, "libMinHook.x64.lib")

// 共享内存名称
#define SHARED_MEM_NAME L"Global\\CorelDrawGdiTextCapture"
#define SHARED_MEM_SIZE 8192

// 共享内存结构
struct SharedTextData {
    WCHAR latestText[4000];
    DWORD processId;
    DWORD timestamp;
};

// 全局变量
HANDLE g_hSharedMem = NULL;
SharedTextData* g_pSharedData = NULL;
std::wofstream g_logFile;

// 原始函数指针
typedef BOOL (WINAPI *PFN_TextOutW)(HDC, int, int, LPCWSTR, int);
typedef BOOL (WINAPI *PFN_ExtTextOutW)(HDC, int, int, UINT, CONST RECT*, LPCWSTR, UINT, CONST INT*);
typedef int (WINAPI *PFN_DrawTextW)(HDC, LPCWSTR, int, LPRECT, UINT);

PFN_TextOutW Original_TextOutW = NULL;
PFN_ExtTextOutW Original_ExtTextOutW = NULL;
PFN_DrawTextW Original_DrawTextW = NULL;

// 写入共享内存
void WriteToSharedMemory(LPCWSTR text, int length) {
    if (!g_pSharedData || !text) return;

    // 限制长度
    int copyLen = min(length, 3999);
    if (copyLen > 0) {
        wcsncpy_s(g_pSharedData->latestText, 4000, text, copyLen);
        g_pSharedData->latestText[copyLen] = L'\0';
        g_pSharedData->processId = GetCurrentProcessId();
        g_pSharedData->timestamp = GetTickCount();

        // 同时写入日志文件用于调试
        if (g_logFile.is_open()) {
            g_logFile << L"[TextOut] " << text << L"\n";
            g_logFile.flush();
        }
    }
}

// Hook: TextOutW
BOOL WINAPI Hook_TextOutW(HDC hdc, int x, int y, LPCWSTR lpString, int c) {
    if (lpString && c > 0) {
        WriteToSharedMemory(lpString, c);
    }
    return Original_TextOutW(hdc, x, y, lpString, c);
}

// Hook: ExtTextOutW
BOOL WINAPI Hook_ExtTextOutW(HDC hdc, int x, int y, UINT options, CONST RECT* lprect, LPCWSTR lpString, UINT c, CONST INT* lpDx) {
    if (lpString && c > 0) {
        WriteToSharedMemory(lpString, c);
    }
    return Original_ExtTextOutW(hdc, x, y, options, lprect, lpString, c, lpDx);
}

// Hook: DrawTextW
int WINAPI Hook_DrawTextW(HDC hdc, LPCWSTR lpchText, int cchText, LPRECT lprc, UINT format) {
    if (lpchText) {
        int len = (cchText == -1) ? wcslen(lpchText) : cchText;
        WriteToSharedMemory(lpchText, len);
    }
    return Original_DrawTextW(hdc, lpchText, cchText, lprc, format);
}

// 初始化Hook
BOOL InitializeHook() {
    // 打开调试日志
    wchar_t logPath[MAX_PATH];
    GetModuleFileNameW(NULL, logPath, MAX_PATH);
    wcscat_s(logPath, L".hook.log");
    g_logFile.open(logPath, std::ios::app);
    g_logFile << L"\n=== Hook Initialized ===\n";

    // 创建共享内存
    g_hSharedMem = CreateFileMappingW(
        INVALID_HANDLE_VALUE,
        NULL,
        PAGE_READWRITE,
        0,
        SHARED_MEM_SIZE,
        SHARED_MEM_NAME
    );

    if (g_hSharedMem == NULL) {
        g_logFile << L"Failed to create shared memory\n";
        return FALSE;
    }

    g_pSharedData = (SharedTextData*)MapViewOfFile(
        g_hSharedMem,
        FILE_MAP_ALL_ACCESS,
        0, 0,
        SHARED_MEM_SIZE
    );

    if (!g_pSharedData) {
        g_logFile << L"Failed to map shared memory\n";
        return FALSE;
    }

    // 初始化MinHook
    if (MH_Initialize() != MH_OK) {
        g_logFile << L"MinHook initialize failed\n";
        return FALSE;
    }

    // Hook TextOutW
    if (MH_CreateHookApi(L"gdi32", "TextOutW", &Hook_TextOutW, (LPVOID*)&Original_TextOutW) != MH_OK) {
        g_logFile << L"Failed to hook TextOutW\n";
    }

    // Hook ExtTextOutW
    if (MH_CreateHookApi(L"gdi32", "ExtTextOutW", &Hook_ExtTextOutW, (LPVOID*)&Original_ExtTextOutW) != MH_OK) {
        g_logFile << L"Failed to hook ExtTextOutW\n";
    }

    // Hook DrawTextW
    if (MH_CreateHookApi(L"user32", "DrawTextW", &Hook_DrawTextW, (LPVOID*)&Original_DrawTextW) != MH_OK) {
        g_logFile << L"Failed to hook DrawTextW\n";
    }

    // 启用Hook
    if (MH_EnableHook(MH_ALL_HOOKS) != MH_OK) {
        g_logFile << L"Failed to enable hooks\n";
        return FALSE;
    }

    g_logFile << L"All hooks enabled successfully\n";
    return TRUE;
}

// 清理Hook
void CleanupHook() {
    MH_DisableHook(MH_ALL_HOOKS);
    MH_Uninitialize();

    if (g_pSharedData) {
        UnmapViewOfFile(g_pSharedData);
        g_pSharedData = NULL;
    }

    if (g_hSharedMem) {
        CloseHandle(g_hSharedMem);
        g_hSharedMem = NULL;
    }

    if (g_logFile.is_open()) {
        g_logFile << L"=== Hook Cleaned Up ===\n";
        g_logFile.close();
    }
}

// DLL入口点
BOOL APIENTRY DllMain(HMODULE hModule, DWORD dwReason, LPVOID lpReserved) {
    switch (dwReason) {
        case DLL_PROCESS_ATTACH:
            DisableThreadLibraryCalls(hModule);
            return InitializeHook();

        case DLL_PROCESS_DETACH:
            CleanupHook();
            break;
    }
    return TRUE;
}
