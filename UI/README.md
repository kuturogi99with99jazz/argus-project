# Argus UIモック

WinForms実装前に、画面構成と「夏」テーマの視認性を確認するためのローカル専用UIモックです。
HTML、CSS、Vanilla JavaScriptだけで構成しており、外部サービスや実際のWebサイトへ接続しません。

## Live Serverでの確認

1. VS CodeでArgusリポジトリを開く
2. 拡張機能「Live Server」が有効であることを確認する
3. `UI/index.html` を右クリックする
4. `Open with Live Server` を選ぶ

初期表示はWinFormsのクライアント領域 `1100 x 700` を想定しています。
幅または高さが不足する場合は、ツールバーや一覧領域をスクロールできます。

## 操作確認

- 行をクリックすると単一選択になる
- Ctrlキーを押しながら行をクリックすると複数選択できる
- 行へキーボードフォーカスを移し、EnterまたはSpaceキーでも選択できる
- 「全件チェック」と「選択をチェック」でチェック中件数と完了状態が変化する
- チェック中でも、同じ対象へ新しいチェックを開始できる
- チェック中の対象は編集と削除が無効になる
- 「追加」と「編集」で共通の入力ダイアログが開く
- 名前の空欄とHTTP/HTTPS以外のURLで入力エラーが表示される
- URLを編集すると状態が「未確認」へ戻る
- 「削除」で対象名を含む確認ダイアログが開く
- 「接続確認用ページ」を選択して「ブラウザで開く」を押すと、操作エラー表示を確認できる
- 実際の既定ブラウザ起動、Web取得、JSON保存は行わない

## WinFormsへの対応

| HTMLモック | WinFormsでの想定 |
| --- | --- |
| `.main-form` | `MainForm` のクライアント領域 |
| `.toolbar` と `.button` | `Panel` / `FlowLayoutPanel` と `Button` |
| `.data-grid-view` と `table` | 読み取り専用・複数選択の `DataGridView` |
| `.status-strip` | `StatusStrip` と `ToolStripStatusLabel` |
| `.target-edit-form` | モーダルな `TargetEditForm` |
| `.confirmation-dialog` | 削除確認ダイアログ |
| `.operation-message` | 画面内エラー表示またはユーザー向けメッセージ |

グラデーション、複雑なアニメーション、独自ウィンドウ枠、Web固有のレイアウトは使用していません。
HTMLモックの承認後、配置と表示状態をWinFormsの標準コントロールへ移します。

## SummerPalette対応

`styles.css` のCSS変数は `Design/design.md` 10.8の定義と対応しています。

| CSS変数 | HEX | WinFormsトークン |
| --- | --- | --- |
| `--background` | `#F4FAFD` | `Background` |
| `--surface` | `#FFFFFF` | `Surface` |
| `--primary` | `#0277BD` | `Primary` |
| `--primary-hover` | `#01579B` | `PrimaryHover` |
| `--accent` | `#00ACC1` | `Accent` |
| `--sun` | `#F9A825` | `Sun` |
| `--leaf` | `#2E7D32` | `Leaf` |
| `--text-primary` | `#17324D` | `TextPrimary` |
| `--text-secondary` | `#526D7A` | `TextSecondary` |
| `--border` | `#B8D8E8` | `Border` |
| `--selection` | `#B3E5FC` | `Selection` |
| `--selection-text` | `#102A43` | `SelectionText` |
| `--disabled-background` | `#E8F1F5` | `DisabledBackground` |
| `--disabled-text` | `#718792` | `DisabledText` |
| `--danger` | `#C62828` | `Danger` |
| `--focus` | `#00695C` | `Focus` |

## 承認時の確認項目

- 1100 x 700で情報量と余白のバランスが適切か
- 960 x 600程度でも主要な操作が可能か
- 一覧の列幅、表示順、状態の見分けやすさが適切か
- 夏テーマが明るく爽やかで、業務アプリとして読みやすいか
- 選択、無効、チェック中、入力エラー、操作エラーを色だけに依存せず識別できるか
- メイン画面と追加・編集ダイアログの操作順が自然か
