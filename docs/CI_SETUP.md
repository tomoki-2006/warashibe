# GameCI セットアップ Runbook（T-U11）

GitHub Actions + [GameCI](https://game.ci) で **PRごとに EditModeテスト自動実行**＋**nightly WebGLビルド**（docs/13 §6）。ワークフローは `.github/workflows/` にコミット済み。以下は**PO（人間）が一度だけ行う** Unityライセンス登録の手順。

## ワークフロー一覧
| ファイル | いつ走る | 何をする |
|---|---|---|
| `ci.yml` | PR / main への push | EditModeテスト（`Warashibe.Core.Tests`）。**ValidateTests がコンテンツ検証を兼ねる**＝docs/13 §6 の「Coreテスト＋コンテンツ検証」ゲート |
| `nightly-webgl.yml` | 毎晩(cron) / 手動 | WebGLビルド（Brotli/High stripping・committed設定準拠）→ 成果物アップロード |
| `activation.yml` | 手動 | Unity Personalライセンスの `.alf` を発行（初回のみ） |

## Unityライセンス登録（初回・必須）
テスト/ビルドには Unity ライセンスが要る。**Personal（無料）**の場合:

1. GitHub の **Actions タブ → "Acquire Unity activation file" → Run workflow** を実行
2. 完了後、成果物 **`Manual Activation File`（`.alf`）** をダウンロード
3. <https://license.unity3d.com/manual> を開き、`.alf` をアップロード → **`.ulf`（ライセンスファイル）** を取得
4. リポジトリ **Settings → Secrets and variables → Actions → New repository secret**:
   - `UNITY_LICENSE` = 取得した **`.ulf` の中身（XML全文）をそのまま貼る**
   - （Personalなら `UNITY_EMAIL` / `UNITY_PASSWORD` も登録推奨）
5. 以後、PR を出すと `ci.yml` が自動で走る。

> Plus/Pro を使う場合は `.alf` 手順は不要。`UNITY_SERIAL` / `UNITY_EMAIL` / `UNITY_PASSWORD` を Secrets に登録し、ワークフローの env をシリアル方式に合わせる。

## メモ
- **Unityバージョン**は `ProjectSettings/ProjectVersion.txt`（現在 `6000.5.3f1`）から自動検出。該当の GameCI エディタ Docker イメージが無い場合はビルドが「image not found」で落ちるので、その時は近い LTS へ寄せるか GameCI の対応版を待つ。
- **初回の CI は UNITY_LICENSE 未登録だと赤**（ライセンス無しでは Unity が起動しない）。上記登録後に緑になる＝これは仕様。
- **nightly ビルドの Firebase 配信**は自動化していない（deploy は `docs/DEPLOY_WEBGL.md` の手動手順）。必要になったら `FIREBASE_TOKEN` を足して nightly の後段に deploy ステップを追加できる。
- Library はキャッシュ（`actions/cache`）。LFS は不使用（フォントは通常アセットとしてコミット）。
