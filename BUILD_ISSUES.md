# WinUI 3 项目编译问题排查手册

> 本文档记录了 VoiceInput 项目在 GitHub Actions CI 上遇到的所有编译问题、根因分析和解决方案。

---

## 环境信息

| 项目 | 值 |
|------|-----|
| 框架 | WinUI 3 (Microsoft.WindowsAppSDK) + .NET 8 |
| TFM | `net8.0-windows10.0.19041.0` 或 `net8.0-windows` |
| CI Runner | `windows-latest` / `windows-2025-vs2026` |
| Runner 预装 | .NET 8 SDK + .NET 10 Runtime/SDK |
| WindowsAppSDK | 1.4.240802001 或 1.5.x |

---

## 问题 1: NETSDK1083 — win10-* RID 不被识别

### 错误信息

```
error NETSDK1083: The specified RuntimeIdentifier 'win10-arm' is not recognized.
error NETSDK1083: The specified RuntimeIdentifier 'win10-x64' is not recognized.
```

### 根因

CI runner 预装了 **.NET 10 SDK/Runtime**，其 `RuntimeIdentifierGraph.json` 注册了 `win10-arm`、`win10-x64`、`win10-x64-aot` 等 RID。.NET 8 SDK 的 `FrameworkReferenceResolution.targets` 在解析时会尝试处理所有注册的 RID，但 .NET 8 SDK 的 RID 图中没有 `win10-*`，导致识别失败。

**触发条件**：使用 `net8.0-windows10.0.19041.0` TFM 时，Windows SDK 会注册 `win10-*` RID。

### 解决方案

**方案 A（推荐）：使用 `net8.0-windows` TFM + `Directory.Build.props`**

```xml
<!-- Directory.Build.props (仓库根目录) -->
<Project>
  <PropertyGroup>
    <TargetPlatformIdentifier>Windows</TargetPlatformIdentifier>
    <TargetPlatformVersion>10.0.19041.0</TargetPlatformVersion>
  </PropertyGroup>
</Project>
```

```xml
<!-- VoiceInput.csproj -->
<TargetFramework>net8.0-windows</TargetFramework>
```

**方案 B：`RuntimeIdentifiers=win-x64` + `RuntimeIdentifier=win-x64`**

在 csproj 或 `Directory.Build.targets` 中设置 `RuntimeIdentifiers=win-x64` 可以限制 RID 解析范围，但**会导致 XamlCompiler.exe 失败**（见问题 3）。

### 注意事项

- `DOTNET_RUNTIME_IDENTIFIER_GRAPH_PATH` 环境变量**不适用于 .NET 8 SDK**（该 SDK 不从文件读取 RID 图，而是内嵌在运行时中）
- 删除 .NET 10 SDK 的 `RuntimeIdentifierGraph.json` **不可行**（文件被占用）
- `global.json` 固定 .NET 8 SDK 版本**不能解决此问题**（问题来自 .NET 10 Runtime，不是 SDK）

---

## 问题 2: MSB4086 — TargetPlatformVersion 为空

### 错误信息

```
error MSB4086: A numeric comparison was attempted on "$(TargetPlatformVersion)"
that evaluates to "" instead of a number,
in condition "'$(TargetPlatformIdentifier)' == 'Windows' and '$(TargetPlatformVersion)' >= '8.0'"
```

### 根因

使用 `net8.0-windows` TFM 时，`TargetPlatformVersion` 不会自动设置。WindowsAppSDK 的 `Microsoft.UI.Xaml.Markup.Compiler.interop.targets` 在评估条件时发现 `TargetPlatformVersion` 为空字符串。

### 解决方案

在**仓库根目录**的 `Directory.Build.props` 中显式设置：

```xml
<Project>
  <PropertyGroup>
    <TargetPlatformIdentifier>Windows</TargetPlatformIdentifier>
    <TargetPlatformVersion>10.0.19041.0</TargetPlatformVersion>
  </PropertyGroup>
</Project>
```

**关键**：必须放在 `Directory.Build.props`（而非 csproj）中，因为 WindowsAppSDK 的 `.props` 文件会在 csproj 评估之前就读取此属性。

---

## 问题 3: MSB3073 — XamlCompiler.exe 静默失败 (exit code 1)

### 错误信息

```
error MSB3073: The command "...XamlCompiler.exe" "input.json" "output.json" exited with code 1.
```

**没有任何 XAML 编译错误输出**，XamlCompiler.exe 直接返回 exit code 1。

### 根因

XamlCompiler.exe 是一个 **.NET Framework 4.7.2** 应用程序。当 csproj 中设置了 `RuntimeIdentifiers=win-x64` 时，输出路径变为 `obj\Release\net8.0-windows10.0.19041.0\win-x64\`，XamlCompiler.exe 无法正确处理这个路径。

**验证方法**：直接运行 XamlCompiler.exe 同样无输出、exit code 1。

### 解决方案

**不要在 csproj 中设置 `RuntimeIdentifiers`**。XamlCompiler.exe 不支持自定义 RID 的输出路径。

正确的做法是：
1. 使用 `net8.0-windows` TFM（而非 `net8.0-windows10.0.19041.0`）
2. 在 `Directory.Build.props` 中设置 `TargetPlatformVersion`
3. 通过 `dotnet publish -r win-x64` 在 CI 中指定目标 RID

---

## 问题 4: MC6000 — WPF PresentationCore 缺失

### 错误信息

```
error MC6000: Project file must include the .NET Framework assembly
'PresentationCore, PresentationFramework' in the reference list.
```

### 根因

当 csproj 中设置 `Platform=x64` 时，.NET SDK 的 Windows Desktop 目标会启用 WPF XAML 编译器（`Microsoft.WinFX.targets`），而 WinUI 3 项目不需要 WPF 组件。

`UseWindowsForms=true` 在某些情况下会隐式启用 WPF 目标。

### 解决方案

- **不要设置 `Platform=x64`**（在 csproj 或命令行中）
- 如果需要指定平台，通过 `dotnet publish --runtime win-x64` 而非 `-p:Platform=x64`
- 如果移除了 `UseWindowsForms=true`（用 P/Invoke 替代），则不会触发此问题

---

## 问题 5: "AnyCPU" is not supported for Self Contained mode

### 错误信息

```
error: The platform 'AnyCPU' is not supported for Self Contained mode.
```

来源：`Microsoft.WindowsAppSDK.SelfContained.targets`

### 根因

WindowsAppSDK 的 `SelfContained.targets` 检查 `$(Platform)` 属性，不接受 `AnyCPU`。当设置了 `RuntimeIdentifiers` 但未设置 `Platform` 时，默认值为 `AnyCPU`。

### 解决方案

- **不要使用 `WindowsAppSDKSelfContained=true`**
- 通过 `dotnet publish --self-contained true` 在命令行中传递，而不是在 csproj 中设置
- 如果确实需要在 csproj 中设置，同时设置 `<Platform>x64</Platform>`（但这会触发问题 4）

---

## 问题 6: WindowsAppSDK 版本解析警告 (NU1603)

### 错误信息

```
warning NU1603: VoiceInput depends on Microsoft.WindowsAppSDK (>= 1.5.250106002)
but Microsoft.WindowsAppSDK 1.5.250106002 was not found.
An approximate best match of Microsoft.WindowsAppSDK 1.5.250108004 was resolved.
```

### 根因

指定了 NuGet 上不存在的精确版本号。

### 解决方案

使用 NuGet 上实际存在的版本号，如 `1.4.240802001` 或 `1.5.250108004`。

---

## 问题 7: UseWindowsForms 触发 WPF 目标

### 现象

`UseWindowsForms=true` 在 WinUI 3 项目中可能隐式启用 WPF XAML 编译目标，导致 MC6000 错误。

### 解决方案

- 移除 `UseWindowsForms=true`
- 将 `System.Windows.Forms.NotifyIcon` 替换为 Win32 `Shell_NotifyIcon` P/Invoke
- 将 `System.Windows.Forms.Clipboard` 替换为 Win32 剪贴板 API
- 将 `System.Windows.Forms.Screen` 替换为 Win32 `MonitorFromPoint` + `GetMonitorInfo`
- 使用 `System.Drawing.Common` NuGet 包替代 `System.Drawing`（WinForms 内置）

---

## 正确的 csproj 配置

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <UseWinUI>true</UseWinUI>
    <!-- 不要使用 UseWindowsForms -->
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <!-- 不要设置 RuntimeIdentifier 或 RuntimeIdentifiers -->
    <!-- 不要设置 Platform 或 PlatformTarget -->
    <!-- 不要设置 WindowsAppSDKSelfContained -->
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <RootNamespace>VoiceInput</RootNamespace>
    <AssemblyName>VoiceInput</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.4.240802001" />
    <PackageReference Include="NAudio" Version="2.2.1" />
    <PackageReference Include="System.Drawing.Common" Version="8.0.0" />
    <PackageReference Include="System.Speech" Version="8.0.0" />
  </ItemGroup>
</Project>
```

## 正确的 Directory.Build.props (仓库根目录)

```xml
<Project>
  <PropertyGroup>
    <TargetPlatformIdentifier>Windows</TargetPlatformIdentifier>
    <TargetPlatformVersion>10.0.19041.0</TargetPlatformVersion>
  </PropertyGroup>
</Project>
```

## 正确的 CI Workflow

```yaml
- name: Publish
  run: dotnet publish src/VoiceInput --configuration Release --output publish --self-contained true --runtime win-x64 --verbosity minimal /p:Version=1.0.0
```

关键点：
- 不要在 csproj 中设置 `RuntimeIdentifier`/`RuntimeIdentifiers`
- 通过 `--runtime win-x64` 在命令行指定
- 不要使用 `-p:Platform=x64`
- 不要使用 `-p:RuntimeIdentifiers=win-x64`
- 不需要删除 .NET 10 SDK 的 RID 图

---

## 核心原则

1. **WinUI 3 项目不应该设置 `RuntimeIdentifier`/`RuntimeIdentifiers`**——XamlCompiler.exe 不支持
2. **`net8.0-windows` TFM 需要通过 `Directory.Build.props` 设置 `TargetPlatformVersion`**
3. **不要使用 `UseWindowsForms`**——用 P/Invoke 替代
4. **CI 中通过 `--runtime win-x64` 指定目标平台**，而非 csproj 属性
5. **不要设置 `Platform=x64`**——会触发 WPF XAML 编译器
