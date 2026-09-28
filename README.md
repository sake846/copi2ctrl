# Copi2Ctrl (Copilot to Ctrl Remapper)

WindowsノートPCに搭載されている **Copilotキー** を **Ctrlキー**（左Ctrl / 右Ctrl）に置き換える常駐ユーティリティです。  
C# / .NET 10 環境で動作し、軽量かつ邪魔にならないタスクトレイ常駐型アプリケーションとして設計されています。

---

## 主な特徴

1. **完全なCtrlキー動作**:
   - 単体での押下はもちろん、`Copilot + C`（コピー）、`Copilot + V`（貼り付け）、`Copilot + A`（全選択）などのショートカットキーを通常のCtrlキーと全く同じ感覚で利用できます。
2. **スタートメニューの誤爆防止**:
   - Copilotキーはハードウェア的に `Win + Shift + F23` のコンビネーションを送信しますが、本ツールはWinキー単体押し判定によるスタートメニューの誤表示を完全に抑制します。
3. **低遅延 & 高互換性**:
   - Win32 低レベルキーボードフック (`WH_KEYBOARD_LL`) と `SendInput` API によるミリ秒未満の高速リマップ。
4. **タスクトレイ常駐型 GUI**:
   - 起動時は自動的にタスクトレイに格納され、作業を邪魔しません。
   - トレイアイコンの右クリックから「有効/無効の切り替え」「左Ctrl/右Ctrlの切り替え」「Windows起動時の自動実行（スタートアップ登録）」「キー監視・動作テスト画面」の呼び出しが可能です。
5. **キー監視・動作テスト画面（診断機能）**:
   - Copilotキーを押した際にキーボードからどのようなキーコード（仮想キーコード・スキャンコード）が送られているかをリアルタイムで確認できるモニター機能を搭載しています。

---

## 動作環境

- **OS**: Windows 11 / Windows 10 (64-bit)
- **ランタイム**: .NET 10.0 (Windows Desktop Runtime) または .NET 10 SDK

---

## クイックスタート

### 1. 起動方法

```powershell
# 通常起動（タスクトレイに常駐）
dotnet run

# キー監視・テスト画面を同時に開いて起動
dotnet run -- --monitor

# コンソールにキーログを出力するデバッグモード
dotnet run -- --console
```

### 2. リリースビルドの作成

単一の実行可能ファイル（`.exe`）として配布・自己完結実行したい場合：

```powershell
# フレームワーク依存ビルド (軽量)
dotnet publish -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -o ./publish

# ランタイム非依存ビルド (.NETランタイム未インストールのPCでも動く単一exe)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish_standalone
```

出力先フォルダ内の `Copi2Ctrl.exe` を実行するだけで即座に利用可能です。

---

## 使い方・機能一覧

### タスクトレイアイコン
起動すると、通知領域（タスクトレイ）に青い `Ctrl` アイコンが表示されます。

- **アイコンダブルクリック**:
  - キー監視・テスト画面を開きます。
- **右クリックメニュー**:
  - **有効 (E)**: リマップの有効/無効をワンクリックで切り替えます（無効時はアイコンがグレーになります）。
  - **置き換え先キー (T)**:
    - `左Ctrl (Left Control)`（デフォルト）
    - `右Ctrl (Right Control)`
  - **キー監視・テスト画面を開く (M)**:
    - リアルタイムログとテスト入力ボックスを備えた診断ウィンドウを表示します。
  - **Windows起動時に自動実行 (S)**:
    - チェックを入れると、Windowsのスタートアップ（レジストリ）に登録され、PC起動時に自動で常駐します。
  - **バージョン情報 (A)**: バージョンダイアログを表示。
  - **終了 (X)**: アプリを終了し、キーフックを安全に解除します。

---

## Copilotキーのリマップの仕組み

最新のWindowsノートPC（Dell, HP, Lenovo, ASUS, Surfaceなど）のCopilotキーは、ハードウェアのキーボードコントローラーから以下のキーコンビネーションが送信されています：

```
[KeyDown]
  1. Left Windows (VK: 0x5B)
  2. Left Shift   (VK: 0xA0)
  3. F23          (VK: 0x86)

[KeyUp]
  4. F23          (VK: 0x86)
  5. Left Shift   (VK: 0xA0)
  6. Left Windows (VK: 0x5B)
```

本プログラムは以下の手順で安全に置き換えを行います：
1. `F23 KeyDown` を検知した瞬間に、イベントをOSに渡さずブロック。
2. 直前に届いた `LWin` と `LShift` の押下状態を解除（`KeyUp` を注入）。この際、スタートメニューの誤発火を防ぐマスクキー（`0xFF`）を送信。
3. 指定されたターゲットCtrlキー（左Ctrlまたは右Ctrl）の `KeyDown` を注入。
4. `F23 KeyUp` を検知した瞬間に、ターゲットCtrlキーの `KeyUp` を注入し、後続のハードウェア由来 `LShift Up` / `LWin Up` を一定時間抑制。
5. 通常のWinキー単体押しやShiftキー操作には一切干渉せず、通常通り利用可能。
