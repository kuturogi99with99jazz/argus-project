# v0.2.0 Release 発行物の手動確認手順

## 共通確認

1. `dotnet restore`、`dotnet build Argus.sln -c Release`、`dotnet test Argus.sln -c Release`を実行する
2. 発行先とは別のディレクトリへ成果物をコピーして起動する
3. schema v1 JSONの読込、一覧、CRUD、全件／選択チェック、4状態、更新ありの差分表示、エラー時データ保持、ブラウザ起動を確認する
4. 「マニュアル」ボタンが未選択時と起動データエラー時にも利用でき、既定ブラウザで本文と2枚の画像を表示できることを確認する
5. ライト／ダーク、キーボード、ダイアログ、アイコン、`v0.2.0`、Releaseで`DEBUG`非表示を確認する

## Windows 10 / 11 x64

```powershell
dotnet publish Argus/Argus.csproj `
  -p:PublishProfile=Windows-x64-SingleFile
```

確認対象:

```text
artifacts\Argus-win-x64-single\Argus.exe
```

- 配布必須ファイルが`Argus.exe` 1ファイルだけであること
- 発行先とは別の場所から起動しても、埋め込みマニュアルが一時領域へ展開されて表示できること
- .NETランタイムを別途必要とせず起動すること
- `%APPDATA%\Argus\targets.json`の既存データを移行なしで利用できること

## macOS 14以降 Apple Silicon

macOS実機上で実行します。

```bash
dotnet publish Argus/Argus.csproj \
  -p:PublishProfile=macOS-arm64-AppBundle
```

確認対象:

```text
artifacts/Argus-macos-arm64/Argus.app
```

- `Contents/Info.plist`、`Contents/MacOS/Argus`、`Contents/Resources/argus-app-icon.icns`が存在すること
- 発行先とは別の場所から`Argus.app`を起動できること
- `~/Library/Application Support/Argus/targets.json`へ保存し、再起動後も利用できること
- 既定ブラウザ起動が成功すること
- 「マニュアル」ボタンからオフライン本文と画像を表示できること

v0.2.0ではApple Developer署名と公証を行いません。Gatekeeperによる初回起動制限が発生した場合は検証記録へ残します。

## 記録

OSバージョン、CPU、.NET SDK、発行コマンド、成果物サイズ、テスト結果、手動確認結果を`Design/tasks.md`のT-032へ記録します。実行していないOSの項目は完了扱いにしません。
