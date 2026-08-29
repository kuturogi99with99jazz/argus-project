# Argus

Argus は、登録した Web ページの更新有無をユーザーが任意のタイミングで確認する、Windows / macOS デスクトップアプリです。

## 目的

ブログ、お知らせ、配布ページ、更新履歴などを一つずつブラウザで開く手間を減らし、前回確認時から変化したページを一覧で把握できるようにします。v0.2.0では自動監視サービスではなく、ユーザーの明示的な操作でチェックするローカルアプリです。

## 正式採用方針

2026-08-12、Avalonia UI PoC の結果を受け、Avalonia を唯一の正式 UI として採用しました。WinForms版は v0.2.0 で廃止し、Core と schema v1 JSON を維持したままクロスプラットフォーム版へ一本化します。

- バージョン: 0.2.0
- UI: Avalonia 12.1.0
- OS: Windows 10 / 11 x64、macOS 14以降 Apple Silicon
- Windows成果物: 正式HTMLマニュアルを内包した自己完結型 `Argus.exe` 1ファイル
- macOS成果物: 自己完結型 `Argus.app`
- 対象外: Linux、Intel Mac、Windows arm64、署名・公証、インストーラー、自動更新

## 主な機能

- 監視対象の追加、編集、削除、有効／無効切替
- 全件または選択項目の手動チェック
- HTMLテキスト、HTML全体、CSSセレクタの3比較方式
- 初回取得、更新なし、更新あり、エラーの一覧表示
- 更新ありの対象について、前回と今回の比較対象の差分表示
- 選択したページをOSの既定ブラウザで開く
- 通信・解析・保存エラー時の前回正常データ保護
- ライト／ダークテーマ切替

## データ保存

監視対象と正常終了した比較結果は、差分表示用の直近の比較対象内容を含めて UTF-8 の schema v1 JSON として保存します。チェック履歴は保存しません。

```text
Windows: %APPDATA%\Argus\targets.json
macOS:   ~/Library/Application Support/Argus/targets.json
```

WindowsではWinForms v0.1.0と同じパスと契約を使用するため、既存データの移行は不要です。外部データベースや秘密情報は使用しません。

## プロジェクト構成

```text
Argus/                 Avalonia UI、ViewModel、UIサービス
Argus.Tests/           ViewModelと表示変換のテスト
Argus.Core/            取得、抽出、比較、保存、チェック調停
Argus.Core.Tests/      Coreの自動テスト
Design/                要件、設計、タスク、発行確認手順
Manual/                実行ファイルへ埋め込む正式HTMLユーザーマニュアルと画像
assets/                バナーとアプリアイコン
```

Core は Avalonia を参照しません。View に業務ロジックを置かず、MVVM と UIサービス境界により、Core と ViewModel をウィンドウなしでテストします。

## 開発

```powershell
dotnet restore
dotnet build Argus.sln
dotnet test Argus.sln
dotnet run --project Argus
```

WindowsではVisual Studio 2026または.NET CLI、macOSではVS Codeと.NET 10 CLIを使用します。IDE拡張は必須ではありません。

## 初期版で対応しないこと

- 自動チェック、常駐監視、スケジュール実行、通知
- ログイン、Cookie、CAPTCHAが必要なサイト
- JavaScript実行後のSPA解析、ブラウザレンダリング
- チェック履歴、外部データベース、永続ログ、JSONバックアップ、設定のインポート／エクスポート
- Wails、Tauriなど別UIフレームワークへの移行

## MVP後の実装候補

MVP後は、次の機能を追加する計画です。いずれも v0.2.0 には含まれません。

- 有効な監視対象を設定した間隔で自動チェックする
- アプリのバックグラウンド動作、または Windows のログイン時起動／タスクスケジューラー、macOS の `launchd` を利用して監視する
- 更新ありを検出したとき、Windows または macOS の OS 標準ローカル通知を表示する
- 未確認の更新あり対象数を Windows のタスクバーまたは macOS の Dock のアイコンバッジへ表示する
- LINE、Slack、メールなど外部サービスへの通知は、別途要件を追加するまで対象外とする

## ライセンス

MIT License
