# DemOo

一款基于 Unity 的卡牌战斗养成类游戏 Demo，集成了 **HybridCLR 热更新** 与 **YooAsset 资源管理**，游戏逻辑全部由 **YokiFrame** 框架驱动。

## 技术栈

| 模块 | 技术 |
|---|---|
| 引擎 | Unity 2022.3.62f3c1 |
| 代码热更新 | HybridCLR |
| 资源管理 / 热更资源 | YooAsset |
| 游戏框架 | YokiFrame（UIKit / EventKit / ResKit / AudioKit / FsmKit 等） |
| 配置解析 | LitJson（JSON 配置驱动） |
| 本地资源服务器 | Python `http.server`（开发用 8080） |

## 玩法与系统

- **卡牌战斗（回合制）**
  - 角色、技能、能量管理、回合与行动顺序、波次推进
  - 近战 / 远程 / 法球等技能表现，头像移动、武器挥砍、飞行物、命中特效
  - 退出战斗时自动清理全部战斗表现对象
- **角色养成**
  - 角色升级：属性由 JSON 配置的 Lv.1 与满级值线性成长
  - 装备武器、遗器（头部 / 手部 / 躯干 / 脚部）
  - 天赋（星魂）升级
- **遗器系统**
  - 主词条按部位规则生成（头=固定生命、手=固定攻击、躯干/脚=随机池）
  - 副词条随机、强化成长、套装效果
- **武器系统**：Lv.1 到满级属性由 JSON 配置线性成长
- **抽卡 / 背包 / 分解 / 商店 / 副本选择 / 登录注册** 等完整养成闭环

## 目录结构

```text
Assets/
├── DataConfig/                  # JSON 配置（角色/武器/遗器/副本/波次/技能/抽卡等）
├── HotUpdateCodeAssets/         # 热更程序集产物（*.dll.bytes）
├── HybridCLRGenerate/           # HybridCLR AOT 泛型补充与 link.xml
├── Scripts/
│   ├── GameLogic/               # 游戏逻辑（HotUpdate 热更程序集）
│   │   ├── Battle/              # 战斗核心、技能处理、回合、波次
│   │   ├── Data/                # 账户、养成、抽卡、商店等系统
│   │   └── UI/                  # 各 UI 面板
│   ├── Shared/                  # 共享主程序集（AOT）
│   │   ├── Configs/             # 配置数据模型
│   │   ├── LitJson/             # JSON 解析库
│   │   └── ...                  # JsonMgr / GameDataMgr / AudioManager 等
│   ├── HybridCLR/               # HybridCLR 启动管理
│   └── GameBootstrap.cs         # 启动流程
├── StreamingAssets/
│   ├── old/                     # 旧版配置备份
│   └── yoo/                     # YooAsset 内置包（构建产物，不入库）
├── BundleCollectorSetting.asset # YooAsset 收集器配置
Bundles/                         # YooAsset 构建输出（不入库）
HybridCLRData/                   # HybridCLR 构建输出（不入库）
```

## 热更新 / 构建流程

1. 在 Unity 中完成 **HybridCLR → Generate All / Build**，生成热更程序集与 Player。
2. 运行 `CopyHotUpdateDlls.bat`：把最新 `HotUpdate.dll` 等复制到 `Assets/HotUpdateCodeAssets`。
3. 在 Unity 中构建 YooAsset 资源包。
4. 运行 `UpdateServerManifest.bat`：自动选择**最新版本目录**并把版本清单、manifest、bundle 同步到 `Bundles/StandaloneWindows64`。
5. 运行 `StartHttpServer.bat`：在 `http://127.0.0.1:8080` 启动本地资源服务器。
6. 客户端启动时通过 YooAsset 请求最新版本并完成热更加载。

> 注意：修改 `Shared` 程序集（如新增配置字段 / 类型）后，需要重新 Build Player 作为新基线，仅更新 `HotUpdate.dll` 无法修复 AOT 主程序集变更。

## 运行环境

- Unity 2022.3.62f3c1
- 本地资源服务器：`python -m http.server 8080`（见 `StartHttpServer.bat`）
