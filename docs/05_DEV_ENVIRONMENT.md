# 05_DEV_ENVIRONMENT — 開発環境・CI/CD・エージェント運用規約
> **【v3.5】スタックはUnityに変更。** 環境構築・CI・CLAUDE.mdは13_UNITY_EDITION §4/§6/§7が正。本書で引き続き有効なのは: §8運用リズム（学生PO・1日1時間）・Git/PR規約・週次の型。
*Status: v3.0 / このとおりに構築すれば環境差ゼロで再現できる*

# 1. 前提ツール

| ツール | バージョン | 備考 |
|---|---|---|
| Node.js | 22 LTS | `.nvmrc` に `22` を置く |
| pnpm | 9系 | `corepack enable` で導入 |
| Git | 2.40+ | |
| GitHub | **事業主体確定後の専用org**（例: warashibe-works）/ `warashibe` | private。IP帰属はv2.0 §6.4の確定に従う。**共同所有org（irigonworks）への配置は不可**（v3.2） |
| デプロイ | Firebase Hosting（preview/prod 2チャネル） | GCPプロジェクト `warashibe-app` を新規作成 |

# 2. リポジトリ初期化（このまま実行）

```bash
pnpm create vite warashibe --template react-ts
cd warashibe && git init
pnpm add zustand framer-motion zod howler html-to-image
pnpm add -D vitest @testing-library/react @testing-library/user-event jsdom \
  playwright @playwright/test eslint prettier eslint-plugin-import \
  eslint-plugin-react-hooks size-limit @size-limit/preset-app \
  vite-plugin-pwa workbox-window
```

## ディレクトリ構成（確定）

```
warashibe/
├── CLAUDE.md                  ← §6の内容をそのまま
├── docs/                      ← 本スイート00〜07＋v1.0/v2.0を全部置く
├── public/assets/{bg,portrait,se,bgm}/
├── src/
│   ├── engine/    (types.ts, exchange.ts, recipe.ts, score.ts, progress.ts, validate.ts)
│   ├── store/     (gameStore.ts, selectors.ts)
│   ├── persistence/ (local.ts, types.ts)
│   ├── scenes/    (TitleScene, MapScene, EncounterScene, ResultScene)
│   ├── components/ (04_UI_SPEC §3の一覧どおり)
│   ├── data/routes/kibi-01/ (items.json, npcs.json, stops.json, route.json,
│   │                          recipes.json, events.json)
│   ├── data/strings.ja.json
│   ├── styles/tokens.css
│   └── utils/ (ruby.tsx, tts.ts, share.ts)
├── scripts/validate-content.ts   ← CIとpre-commitで実行
├── tests/ (engine/*.test.ts, e2e/full-playthrough.spec.ts)
└── .github/workflows/ci.yml, deploy.yml
```

# 3. 設定ファイル要点

- **tsconfig**: `strict: true`, `noUncheckedIndexedAccess: true`, paths alias `@engine/* @components/*` 等
- **vite**: base './'、vite-plugin-pwa（`registerType: autoUpdate`・全アセットprecache・オフライン完走要件）
- **ESLint**: 02_TDD §10の禁止事項をルール化（`no-restricted-imports` でscenes→engine直importを遮断、`no-restricted-globals` でlocalStorage直叩き禁止）
- **size-limit**: `dist/**/*.js` gzip 300KB
- **package.json scripts**:
```json
{
  "dev": "vite", "build": "tsc -b && vite build",
  "test": "vitest run", "test:watch": "vitest",
  "e2e": "playwright test",
  "content:check": "tsx scripts/validate-content.ts",
  "lint": "eslint src --max-warnings 0",
  "check:all": "pnpm lint && pnpm content:check && pnpm test && pnpm build"
}
```

# 4. CI/CD（GitHub Actions・このままコミット）

## .github/workflows/ci.yml
```yaml
name: CI
on: { pull_request: { branches: [main] } }
jobs:
  check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: pnpm/action-setup@v4
        with: { version: 9 }
      - uses: actions/setup-node@v4
        with: { node-version: 22, cache: pnpm }
      - run: pnpm install --frozen-lockfile
      - run: pnpm lint
      - run: pnpm content:check     # 詰みゼロ・語彙・ふりがなCI（03 §9）
      - run: pnpm test              # engine単体
      - run: pnpm build
      - run: pnpm dlx size-limit    # 性能予算
      # v3.2: E2E・Lighthouseはnightly.yml（cron・自動リトライ2回）へ分離。flakyなブラウザテストでPRを止めない
```

## .github/workflows/deploy.yml
```yaml
name: Deploy
on: { push: { branches: [main] } }
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: pnpm/action-setup@v4
        with: { version: 9 }
      - uses: actions/setup-node@v4
        with: { node-version: 22, cache: pnpm }
      - run: pnpm install --frozen-lockfile && pnpm build
      - uses: FirebaseExtended/action-hosting-deploy@v0
        with:
          repoToken: ${{ secrets.GITHUB_TOKEN }}
          firebaseServiceAccount: ${{ secrets.FIREBASE_SA }}
          channelId: live
          projectId: warashibe-app
```
- PRごとにpreviewチャネル自動発行（action-hosting-deployのPRモード）→ **テストプレイURLが毎PR生成される**（子どもテストの高速化）
- ブランチ保護: main直push禁止・CI必須・レビュー0人可（ソロ運用）だがPO承認ラベル運用

# 5. シークレット・環境変数

| 名前 | 用途 | 保管 |
|---|---|---|
| FIREBASE_SA | デプロイ用SA JSON | GitHub Secrets |
| （Phase 1〜）VITE_SUPABASE_URL / ANON_KEY | Supabase | GitHub Secrets＋.env.local |
- Phase 0は環境変数ゼロで動くこと（オンボーディング摩擦ゼロ）

# 6. CLAUDE.md（リポジトリ直下に置く実物・全文）

```markdown
# わらしべワールド 開発エージェント規約

## あなたの役割
docs/07_BACKLOG.md のチケットを1枚ずつ完了させる実装者。
仕様の解釈者ではない。仕様は docs/ にすべて書いてある。

## 作業手順（毎チケット共通）
1. チケットのRefs欄の文書「だけ」を読む
2. feat/T-xxx ブランチを切る
3. 実装 → `pnpm check:all` が通るまで直す
4. チケットのAC（受入条件）を一つずつ自己検証しPR本文にチェックリストで貼る
5. PRを作成。1PR=1チケット厳守

## 絶対規則（違反はrevert対象）
- 仕様と実装が食い違うときは実装を直す。仕様を変えたいときは止まってPO（運営者）に質問
- お金・通貨・課金・広告・チャット・外部リンク・サードパーティSDKを追加しない
- scenesからengineを直接importしない／localStorageを直接叩かない／any禁止
- 日本語文字列をコードに書かない（strings.ja.json か data/ へ）
- セリフ・ヒントを勝手に書き換えない（03_CONTENT_DATA が最終稿）
- 依存パッケージの追加は理由をPRに明記（原則追加しない）

## 迷ったら
docs/00_INDEX.md のSSOT優先順位に従う。それでも不明なら
「未定事項」として質問リストをPRに書き、仮実装にTODO(T-xxx)を残す。POは学生であり応答は1日1回。ブロックせず並行チケットへ進むこと。
```

# 7. アセット運用

- 命名: `bg_kibitsu.webp` / `pt_child.webp` / `se_accept.mp3`（03/04のキーと一致必須）
- 仮素材期: 背景=単色＋地名テキスト、立ち絵=絵文字拡大表示で全機能を先に完成させる（**アート待ちで開発を止めない**）
- 本素材差替はファイル置換のみで完了する構造（コード変更ゼロ）を受入条件化

# 8. 運用リズム（運営者=学生PO・1日1時間×毎日／v3.3）

- 平日（各60分）: ①完了PRの確認とマージ（15分）→ ②次のチケット1枚を子セッションに投入（15分）→ ③preview URLで通しプレイ・違和感をIssue化（30分）
- 週末どちらか（60分）: バックログ整列・決定ログ更新・翌週の計画
- **学業が最優先。** 試験期間は「新規投入なし・マージのみ」の省エネモードに落とす（CI緑のPRを溜めておけば再開が軽い）
- 週7時間は旧体制（週90分）の約5倍の意思決定帯域。Phase 0の90日計画は60〜75日に短縮可能だが、**締切は前倒ししない**——生まれた余裕は品質・プレイテスト・学業に回す
- 時差がある場合はむしろ有利: 就寝前にチケット投入→起床時にPRが揃う「時差ドリブン開発」を基本形にする
