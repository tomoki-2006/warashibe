# WebGL ビルド & Firebase 配布 Runbook（T-U10）

Phase 0 のテスト配布は **WebGLビルド → Firebase Hosting → URL共有**（docs/13 §5 / G3）。
コード側（ビルド設定・非同期ローダ・Hosting設定）は本リポジトリに入っている。以下は**人間（PO）が一度だけ行う**認証・配布・実機確認の手順。

## 前提
- Unity 6000.5.x（WebGLモジュール導入済み）
- Node.js ＋ Firebase CLI: `npm i -g firebase-tools`
- Firebase プロジェクト（無料 Sparkプランで可）を1つ作成しておく

## 1. WebGLビルド
- Unity エディタ: メニュー **Warashibe ▸ Build WebGL** を実行
- または CLI（CI/T-U11 でも使用）:
  ```
  <Unityの実行ファイル> -batchmode -quit -projectPath . \
    -executeMethod Warashibe.Editor.WebGLBuilder.BuildFromCommandLine
  ```
- 出力先: `Builds/WebGL/`（`index.html` ＋ `Build/*.br`）。設定は docs/13 §5 準拠で
  **Brotli圧縮 / Managed Stripping=High / エンジンコードstrip**（`WebGLBuilder.ApplyWebGLSettings`）。
- サイズ確認（docs/13 §5 目標 **圧縮後 ≤30MB**）:
  ```
  du -ch Builds/WebGL/Build/*.br | tail -1
  ```

## 2. Firebase 初期設定（初回のみ）
- ログイン（このセッションなら `! firebase login` と打つと直接実行できます）:
  ```
  firebase login
  ```
- プロジェクトを紐付け（どちらかでOK）:
  - `.firebaserc.example` を `.firebaserc` にコピーして `YOUR_FIREBASE_PROJECT_ID` を自分のIDに、または
  - `firebase use --add` で対話選択
- `firebase.json`（Hosting設定・`.br`用の `Content-Encoding: br` ヘッダ入り）は編集不要。

## 3. デプロイ
```
firebase deploy --only hosting
```
表示される `Hosting URL` をテストプレイヤーに共有する。

## 4. 実機確認（AC）
- **iPhone / Android / PC** のブラウザでURLを開き、起動〜アブ捕獲〜交換まで動作すること
- 初回ロード ≤15秒（Wi-Fi）／圧縮後 ≤30MB を確認
- 対面テスト会では**ローカル実行を併用**（ロード待ちで子どもを飽きさせない, docs/13 §5・§8）

## メモ
- `.firebaserc` と `Builds/` は `.gitignore` 済み（プロジェクト固有・生成物のため）。
- Brotli 配信には Hosting 側の `Content-Encoding` ヘッダが必須。`firebase.json` に設定済み。
- High stripping で Newtonsoft のリフレクション型が落ちないよう `Assets/link.xml` で
  `Warashibe.Core` と `Newtonsoft.Json` を保護済み。**初回の実機起動でコンテンツJSONの
  読み込み（＝デシリアライズ）が成功することを必ず確認**すること。
- CI（GitHub Actions + GameCI）での nightly ビルドは **T-U11** で整備。
