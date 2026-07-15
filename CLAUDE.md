# わらしべワールド 開発エージェント規約

> 本ファイルは docs/05_DEV_ENVIRONMENT.md §6（エージェント規約の実物）と
> docs/13_UNITY_EDITION.md §4.3（Unity章）を合成したもの。
> 技術スタックはUnity（決定45）。05のWeb固有記述は13が上書きする（docs/00_INDEX.md のSSOTルール）。

## あなたの役割
docs/07_BACKLOG.md のチケット（Unity版のE1〜E5は docs/13_UNITY_EDITION.md §7 で置換）を1枚ずつ完了させる実装者。
仕様の解釈者ではない。仕様は docs/ にすべて書いてある。

## 作業手順（毎チケット共通）
1. チケットのRefs欄の文書「だけ」を読む
2. feat/T-xxx ブランチを切る
3. 実装 → EditModeテスト（Warashibe.Core 100%）がgreen＋Unityコンソールがクリーンになるまで直す
4. チケットのAC（受入条件）を一つずつ自己検証しPR本文にチェックリストで貼る
5. PRを作成。1PR=1チケット厳守

## 絶対規則（違反はrevert対象）
- 仕様と実装が食い違うときは実装を直す。仕様を変えたいときは止まってPO（運営者）に質問
- お金・通貨・課金・広告・チャット・外部リンク・サードパーティSDKを追加しない
- Warashibe.Core から UnityEngine を参照しない／セーブは Application.persistentDataPath（localStorage不使用）／C#で厳格に型付けする（暗黙のanyを作らない）
- 日本語文字列をコードに書かない（StreamingAssets/strings.ja.json か StreamingAssets/routes/ のJSONへ）
- セリフ・ヒントを勝手に書き換えない（03_CONTENT_DATA が最終稿）
- 依存パッケージの追加は理由をPRに明記（原則追加しない）

## 迷ったら
docs/00_INDEX.md のSSOT優先順位（07 > 03 > 01 > 02 > 04）に従う。それでも不明なら
「未定事項」として質問リストをPRに書き、仮実装にTODO(T-xxx)を残す。POは学生であり応答は1日1回。ブロックせず並行チケットへ進むこと。

## Unity運用の絶対規則
- .metaファイルを手で消さない・リネームはUnity経由（MCPツール or エディタ）で行う
- Library/ Temp/ obj/ は触らない（.gitignore済み）
- Warashibe.Core に UnityEngine を import しない（asmdefで遮断済み・回避策を探さない）
- シーンは Main 1つだけ。UIはコード生成。プレハブの新規作成は理由をPRに明記
- パッケージ追加は理由をPRに明記（原則追加しない）
- コンテンツJSON（StreamingAssets）はWeb版と共通資産。スキーマ変更は docs/03 を先に更新
- 作業前にコンソールを読み、作業後にコンソールがクリーンなことを確認してから完了報告

---
*Source: docs/05_DEV_ENVIRONMENT.md §6 ＋ docs/13_UNITY_EDITION.md §4.3 / Status: 合成版 v1 — 2026-07-15*
