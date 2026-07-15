# 02_TDD — 技術設計書（Technical Design Document）
> **【v3.5】スタックはUnityに変更（13_UNITY_EDITIONが実装の正）。** 本書のうち言語非依存の部分——三層の掟（§1）・型定義（§2）・エンジンAPIと検証仕様（§3）・状態設計・エラー方針——はC#移植の原本として引き続き有効。Web固有節（§5レンダリング・§6性能予算・§7 i18n実装・§10 lint規約の一部）は13が上書きする。
*Status: v3.0 / Phase 0確定・Phase 1-2の拡張点を注記*

# 1. アーキテクチャ

```
┌────────────────────── ブラウザ (PWA) ──────────────────────┐
│  scenes/ (React)      ← 画面。ロジックを持たない            │
│     │ hooks経由でstoreを購読                                │
│  store/ (Zustand)     ← ゲーム状態の唯一の置き場            │
│     │ actionsはengineの純関数を呼ぶだけ                     │
│  engine/ (純TypeScript)← 交換判定・レシピ・スコア・進行     │
│     │                    Reactに依存しない（テスト容易性）   │
│  data/ (JSON)         ← コンテンツ。コードから完全分離      │
│  persistence/         ← localStorageアダプタ（差替可能IF）  │
└────────────────────────────────────────────────────────────┘
```

**三層の掟**: scenesはengineを直接呼ばない／engineはDOM・React・localStorageを知らない／dataはロジックを含まない（`if`をJSONに書かない）。この分離がPhase 1のSupabase移行・Phase 3のサーバー権威化・海外版データ差替をすべて無傷にする。

# 2. 型定義（完全版・`src/engine/types.ts` にこのまま置く）

```typescript
// ===== コンテンツ型（dataのスキーマ）=====
export type ItemId = string;      // "item_wara" 形式
export type NpcId = string;       // "npc_" prefix
export type LocationId = string;  // "loc_" prefix

export interface Item {
  id: ItemId;
  name: string;              // 表示名（ふりがなは name_ruby）
  name_ruby: string;         // 例 "きびだんご"
  emoji: string;
  origin: string;            // 産地表示
  trivia: string;            // 図鑑豆知識（Phase 0は記録のみ）
  baseValue: 1|2|3|4|5;      // 「あなたにとって」の星
}

export interface Recipe {
  inputs: [ItemId, ItemId];  // 順不同で照合
  output: ItemId;
}

export interface NpcQuestion { q: string; a: string; }

export interface AcceptRule {
  item: ItemId;
  valueForNpc: 1|2|3|4|5;    // 「あいてにとって」の星（> baseValue必須）
  reasonLine: string;        // 価値メーターの一言理由
  gives: ItemId;
  acceptLines: string[];     // 成立時セリフ（順に表示）
  postEvent?: EventId;       // 例: 馬の世話イベント
}

export interface Npc {
  id: NpcId;
  name: string;
  portrait: string;          // アセットキー
  intro: string[];           // 初回会話（困りごとの提示。答えは書かない）
  idleLine: string;          // 再話しかけ時
  questions: NpcQuestion[];  // 最大2
  accepts: AcceptRule[];     // Phase 0は各NPC 1件
  declineLines: [string, string, string]; // L1/L2/L3拒否文（L1にヒント内包）
  hintL2: string;            // ぶんの吹き出し
  hintL3: string;            // ぶんの実質答え＋ハイライト対象
  highlightTarget?: string;  // L3で光らせる対象（itemId or "abu"）
  afterTradeLine: string;    // 取引後の再会話
  observables?: { target: string; bunLine: string }[]; // タップで気づき（観察ボーナス対象・v3.2）
}

export interface Stop {
  id: LocationId;
  name: string;
  region: string;
  mapX: number;              // 絵巻座標(px, 論理幅3600基準)
  bg: string;                // 背景アセットキー
  npcIds: NpcId[];
  ambientEvent?: EventId;    // 例: "ev_abu_flying"
}

export interface Route {
  id: string;
  title: string;
  startItem: ItemId;
  goalItem: ItemId;
  stops: LocationId[];       // 順序が進行順
}

export type EventId = "ev_abu_catch" | "ev_horse_care" | "ev_boat_ride";

// ===== ランタイム型 =====
export interface OfferResult {
  outcome: "accept" | "decline" | "duplicate";
  hintLevelShown: 0|1|2|3;
  gained?: ItemId;
  lines: string[];
  valueMeter?: { mine: number; theirs: number; reason: string };
}

export interface StopProgress {
  questionsUsed: number;     // 0-2
  rejections: number;
  deepestHint: 0|1|2|3;
  offeredItems: ItemId[];    // 重複提示防止
  cleared: boolean;
}

export interface SaveData {
  version: 1;                // マイグレーション用
  playerName: string;        // v2で profiles[] に移行（兄弟3人対応・Phase 1 T-031で定義）
  routeId: string;
  stopIndex: number;
  inventory: ItemId[];
  progress: Record<LocationId, StopProgress>;
  bestScore: number;
  clearedRoutes: string[];
  zukanItems: ItemId[];      // Phase 0から記録（UIはPhase 1）
  zukanNpcs: NpcId[];
  settings: { tts: boolean; bgm: boolean; se: boolean };
}
```

# 3. エンジンAPI（`src/engine/` 公開関数・すべて純関数）

```typescript
// exchange.ts
evaluateOffer(npc: Npc, item: ItemId, prog: StopProgress): OfferResult
// recipe.ts
combine(a: ItemId, b: ItemId, recipes: Recipe[]): ItemId | null
// score.ts
stopScore(prog: StopProgress): number        // GDD§4の数式そのまま
routeRank(total: number): "choja"|"daishonin"|"gyoshonin"|"minarai"
// progress.ts
applyOffer(state: GameState, npcId: NpcId, item: ItemId): GameState  // イミュータブル更新
advanceStop(state: GameState): GameState
// validate.ts（CIとランタイム共用）
validateRoute(route, stops, npcs, items, recipes): ValidationError[]
//  検証: 全ストップ到達可能 / gives連鎖でgoalItemに到達 / valueForNpc>baseValue /
//        declineLines=3件 / questions≤2 / 参照ID実在 / ふりがな欠落 / 一文40字
```

- Phase 0は乱数不使用（完全決定的）。**Phase 1のデイリー行商から日付seedの決定的乱数を導入**: NPCの需要と提示アイテムを既存プールから日替わり再構成（同一日は全端末同一＝友だちとの攻略会話が成立、翌日は別問題＝リプレイ性の恒久供給）。seed注入式以外の乱数は恒久禁止（v3.2）

# 4. 状態管理（Zustand store設計）

```typescript
interface GameStore {
  save: SaveData;
  ui: { scene: SceneId; modal: ModalId|null; inputLocked: boolean };
  content: LoadedContent;              // 起動時にJSONをZodで検証して保持
  actions: {
    startRoute(routeId): void;
    talk(npcId): void; ask(npcId, qIndex): void;
    offer(npcId, itemId): void;        // engine呼出→save更新→autoSave()
    combineItems(a, b): void;
    moveTo(stopId): void;
    resetForReplay(): void;
  };
}
```

- store層でのみ副作用（autoSave, SE再生のイベント発火）。演出はUIがstoreの結果を購読して再生
- JSONロードは起動時一括（Phase 0の総データ<200KB）。**Zodスキーマで実行時検証**し、型とデータの乖離を起動時に検出

# 5. レンダリング方針

- **Canvas/WebGL不使用。全編DOM＋CSS transform**（理由: AIエージェントの生産性最大・アクセシビリティ・十分な性能）
- 絵巻マップ: 横長divをtransform:translateXでスクロール。will-change指定・画像は遅延読込
- アニメはFramer Motion（宣言的・数値は04_UI_SPECの表が正）
- 結果カード: 非表示DOMを`html-to-image`でPNG化（html2canvasより日本語フォント安定）

# 6. パフォーマンス予算（CIで計測・超過はマージ不可）

| 指標 | 予算 | 計測 |
|---|---|---|
| 初期バンドル(gzip) | ≤ 300KB（画像除く） | vite build + size-limit |
| 初回表示(TTI) | ≤ 3.0s（Moto G相当・Fast 3G） | Lighthouse CI |
| 画像総量(Phase 0) | ≤ 4MB（WebP・背景1枚≤200KB） | assetチェックスクリプト |
| メモリ | ≤ 150MB | 手動（iPad第9世代で確認） |
| フレーム | 演出中60fps・ジャンク1%未満 | Chrome DevTools手動 |

# 7. i18n・将来拡張の仕込み（Phase 0でやること/やらないこと）

- 全UI文字列は `data/strings.ja.json` に外出し（ハードコード禁止）— Trade Up World展開の保険。翻訳作業自体はしない
- ルビは `名前|よみ` 形式のカスタム記法→`<ruby>`変換ユーティリティを共通化
- 通貨・数値フォーマット関数は最初からIntl使用

# 8. 永続化と移行

- Phase 0: `localStorage["ww.save.v1"]`。書込は状態遷移直後にdebounce 500ms
- SaveData.versionでマイグレーション関数チェーン（v1→v2…）を最初から用意
- Phase 1: persistence層をSupabaseアダプタに差替（IF同一）。localStorageはオフラインキャッシュに降格

# 9. エラーハンドリング

- ErrorBoundary全画面（和風の「おや、道に迷ったようだ」画面＋「はじめの町へもどる」）
- content検証エラーは開発時=throw、本番=Sentryなし（子どもカテゴリ配慮）→自前エンドポイントへ匿名送信はPhase 1で判断
- console.errorはCIでゼロを強制（テスト中の握りつぶし防止）

# 10. コーディング規約（抜粋・ESLintで機械化）

- TypeScript strict / noUncheckedIndexedAccess: true
- コンポーネント: 関数コンポーネントのみ・1ファイル1エクスポート・200行超で分割
- 命名: シーン=`XxxScene` / コンポーネント=PascalCase / engine関数=動詞始まり
- コメントは「なぜ」のみ書く（「何を」はコードで表現）
- import順: engine → store → components → assets（eslint-plugin-importで強制）
- **禁止事項（lintルール化）**: scenesからengine直import / localStorage直叩き / any / 日本語ハードコード

# 11. Gitとブランチ運用

- ブランチ: `main`（保護・CI必須）← `feat/T-xxx-短い説明`（チケットID必須）
- コミット: Conventional Commits（feat/fix/content/docs/test/chore）
- PR: テンプレ必須（対応チケット・AC自己チェック・スクショorGIF）。1PR=1チケット
- リリースタグ: `v0.x.y`。Phase 0完了=`v0.1.0`
