# 13_UNITY_EDITION — Unity版 技術計画書
*Status: v3.5 / 技術スタックをUnityに変更する決定の記録と、Unity前提の実装・運用計画。02_TDD/05のWeb固有節は本書が上書きする（三層の掟・GDD・コンテンツ・事業計画は不変）*

# 0. 決定記録（正直な差分から始める）

**決定45: スタックをWeb PWAからUnityへ変更。** 理由は運営者（PO）の意思——4年間続くソロプロジェクトの最重要資源はPOのモチベーションと成長であり、Unityスキルはそれ自体が留学期間の資産になる。

**この変更で失うもの（隠さない）**:
1. 超軽量配布（Web版の300KB/TTI3秒 → WebGLは数十MB級）
2. 学校GIGA端末（特にChromebook）でのブラウザ軽量動作 — **学校チャネルの武器が1つ弱る**
3. AIエージェントの得意領域ど真ん中（React/TS）からの半歩後退

**ガードレール（失わないために計画で固定するもの）**:
- G1: **コンテンツはJSONのまま**。03の全データ・CI検証・11の量産テンプレを無傷で流用（ScriptableObject化しない）
- G2: **純C#コア＋単体テスト**で三層の掟を維持（§2）。エンジンはUnityなしでテストできる
- G3: **Phase 0のURL配布はWebGLビルドで維持**。テストプレイヤーにはこれまでどおりURLを送るだけ
- G4: 学校チャネルの軽量要件はPhase 4で再検討（軽量Web版の別建て or iPad MDM配布）。リスク登録して先送り——今は決めない

利点も正直に: Phase 1のストア配信で**Capacitorラップが不要になり、ネイティブが本道になる**。アニメ・演出・将来の3D演出の天井も上がる。

# 1. 変わらないもの（＝計画書の大半）

01_GDD（ルール・数式）／03（全コンテンツJSON）／04の意匠（色トークン・文言・アニメ数値表——実装手段だけ変わる）／06（QA・プレイテスト手順）／07のE6以降・E8／08〜12／v2.0事業計画。**warashibe-proto.html は「正解の手触り見本」として保存**——Unity版の受入基準は常に「プロトタイプと同等以上の手触り」。

# 2. アーキテクチャ（三層の掟のUnity翻訳）

```
Assets/
├── Scripts/
│   ├── Core/        ← Warashibe.Core.asmdef（純C#・UnityEngine参照なし）
│   │   Types.cs / Exchange.cs / Recipe.cs / Score.cs / Progress.cs / Validate.cs
│   ├── Game/        ← Warashibe.Game.asmdef（MonoBehaviour・演出・入力）
│   └── Tests/       ← Warashibe.Core.Tests.asmdef（EditMode・NUnit）
├── StreamingAssets/routes/kibi-01/  ← 03のJSONをそのまま配置
├── StreamingAssets/strings.ja.json
└── Fonts/ (BIZ UDGothicのTMPアセット)
```

- **掟の物理的強制**: Core.asmdef の参照リストからUnityEngine系を外す→CoreにMonoBehaviourを書くとコンパイルが通らない（Web版のlint禁止事項と同じ役割）
- JSONロード: `com.unity.nuget.newtonsoft-json`。起動時に全ロード→Validate.csで03 §9の9項目を実行時検証（開発ビルドはエラーで停止）
- セーブ: `Application.persistentDataPath` にJSON（SaveData v1の型は02 §2のまま移植・localStorage不使用の悩みが消える）
- 乱数: 決定的seed注入のみ（02 §3の原則を継承）

# 3. UI実装方針

- **uGUI + TextMeshPro**（UI Toolkitではなく。理由: エージェントの学習データ量・事例量が圧倒的で「丸投げ」の成功率が上がる）
- 日本語フォント: BIZ UDGothicをTMPフォントアセット化＋ダイナミックフォールバック。Phase 0は全ひらがな文体なので**ルビ実装は不要**（「ふつう文体」導入のPhase 1で方式を判断）
- **UIは極力コードで生成する**（プレハブの手作業配置を最小化）。理由が本書の核心: シーンやプレハブのYAMLをエージェントが安全に編集するのは難しいが、**C#コードなら完全に所有できる**。「MCPに丸投げできる形」とは「プロジェクトの真実がコードとJSONに寄っている形」のこと
- シーン構成: **1シーン（Main）＋UIステート切替**（Title/Map/Encounter/Resultはパネルの出し分け）。シーンを増やさない=マージ事故と手作業をゼロに近づける
- 04のトークン: 色・寸法・アニメ時間を `DesignTokens.cs` 定数に転記（生値のハードコード禁止は同じ）

# 4. Claude MCP 丸投げ体制（本題）

## 4.1 接続方式は2択（まずAで始める）

**A. OSS: Unity-MCP（CoplayDev/unity-mcp・無料）**
1. Package ManagerにGit URLでパッケージ導入 → Unityメニューから **MCP for Unity → Start Server**
2. ターミナルで `claude mcp add` によりサーバーを登録（README/wikiの最新コマンドに従う。macOSはHub起動だとPATHが通らないことがあるので、ターミナルから起動するか、MCPウィンドウでclaudeの絶対パスを指定）
3. Claude Code内で `/mcp` を実行し接続確認
- 毎回の起動順: **Unity起動 → MCPサーバー起動 → Claude Code起動**。接続エラー時はUnity再起動＋ポート占有プロセスの確認が定石

**B. Unity公式 MCP Server（Unity AI）**
- Project Settings > AI > Unity MCP → Bridge起動 → Integrations で Claude Code を Configure → 初回接続の Pending を承認
- ただし**Unity AIサブスクリプション必須**（Personalは別途登録・14日トライアル後有料）。**Aで不足を感じてから検討**（学生予算の原則）

## 4.2 エージェントに渡す作業ループ（1チケットの型）

```
1. POがチケットを指示（07/本書§7の1枚）
2. Claude Code: Core/GameのC#を書く（ここが主戦場・MCP不要でも進む）
3. MCP経由: コンパイル結果とコンソールを読む → エラーを自走修正
4. MCP経由: Play Modeで起動・スモーク確認（「タイトル→S1まで進めてコンソール要約して」）
5. EditModeテスト実行（Core 100%）→ green
6. コミット・PR（1PR=1チケット）
7. 見た目の最終判定だけPOがスクショで行う（美的判断はMCPに任せない）
```

## 4.3 CLAUDE.md への追記（Unity章・そのまま貼る）

```markdown
## Unity運用の絶対規則
- .metaファイルを手で消さない・リネームはUnity経由（MCPツール or エディタ）で行う
- Library/ Temp/ obj/ は触らない（.gitignore済み）
- Warashibe.Core に UnityEngine を import しない（asmdefで遮断済み・回避策を探さない）
- シーンは Main 1つだけ。UIはコード生成。プレハブの新規作成は理由をPRに明記
- パッケージ追加は理由をPRに明記（原則追加しない）
- コンテンツJSON（StreamingAssets）はWeb版と共通資産。スキーマ変更は docs/03 を先に更新
- 作業前にコンソールを読み、作業後にコンソールがクリーンなことを確認してから完了報告
```

# 5. ビルドと配布（G3の実装）

- **Phase 0テスト配布 = WebGLビルド → Firebase Hostingへアップ → URL共有**（従来戦略そのまま）
- WebGL設定: 圧縮=Brotli／Code Stripping=High／2D最小構成／目標: **圧縮後 ≤30MB・初回ロード ≤15秒（Wi-Fi）**——Web版の予算（300KB/3秒）は物理的に不可能なので置換。対面テスト会ではローカル実行を併用し、ロード待ちで子どもを失わない
- Phase 1: iOS/Androidネイティブビルドでストア申請（Capacitor行程は削除）
- モバイル予算: 実機60fps／アプリサイズ ≤150MB／iPad第9世代を基準機に維持

# 6. CI（GameCI）

- GitHub Actions + GameCI（unity-test-runner で EditModeテスト、unity-builder で WebGLビルド）。Unityライセンスのアクティベーションファイルを GitHub Secrets に登録
- PRゲート: Coreテスト＋コンテンツ検証（Validate.csをバッチモード実行）。**ビルドはnightly**（Unityビルドは重い——Web版v3.2の「flakyをPRに入れない」原則を継承）
- W2までに整備できなくてもよい（ローカルMacビルドで先行可）。ただしT-U04のテストだけはW1から回す

# 7. バックログ差し替え（07のE1〜E5をUnity版チケットに置換・E6以降は流用）

| ID | 内容 | 規模 | AC要点 |
|---|---|---|---|
| T-U01 | プロジェクト作成（**Universal 2Dテンプレ**・名前warashibe）＋Git初期化（Unity用.gitignore） | S | リポジトリにpush・meta含めコミット |
| T-U02 | MCP接続（§4.1-A）＋CLAUDE.md配置 | S・人間協働 | Claude Codeの/mcpで接続確認・コンソール読取デモ成功 |
| T-U03 | asmdef 3分割＋Newtonsoft導入＋DesignTokens.cs | S | CoreにUnityEngine参照なしをコンパイルで確認 |
| T-U04 | Core移植（Exchange/Recipe/Score/Progress）＋EditModeテスト | L | 06 §2の全ケースgreen・カバレッジ100% |
| T-U05 | JSONロード＋Validate.cs（03 §9の9項目） | M | kibi-01読込・破壊データ9種を検出 |
| T-U06 | UIキット（饅頭ボタン・会話枠・にもつグリッド・階段バー・ぶん）をコード生成 | L | 04のトークン準拠・仮素材=絵文字/TMP |
| T-U07 | 会話・質問・提案・ヒント梯子の結合 | 3L | プロトタイプと同一挙動（並走比較） |
| T-U08 | 交換演出・価値メーター・階段・スコア | 2L | 04 §4の数値±10% |
| T-U09 | ミニイベント3種＋チュートリアル誘導 | L | 01 §7-8準拠 |
| T-U10 | WebGLビルド＋Firebase配布（URL発行） | M | 実機iPhone/Android/PCで起動・≤30MB |
| T-U11 | GameCI（テスト＋nightlyビルド） | M | PRでテスト自動実行 |

以降: 07の T-050〜T-072（音・TTS※・E2E→Play Modeテストに読替・プレイテスト運営）を流用。
※TTS: WebGLはWeb Speech APIをjslibプラグイン経由で、ネイティブはOSのTTSで。Phase 0はWebGL側のみ実装。

# 8. リスク追記（11章のレジスタに合流）

| リスク | 対策 |
|---|---|
| POのUnity学習コスト（最初の2週間は進捗が薄い） | 想定内と最初から宣言。T-U01〜03は「学習を兼ねたチケット」として遅延を許容 |
| WebGLの初回ロードで子どもテストの熱が冷める | 対面会はローカル実行・URL配布は「家で遊んでね」用に役割分担 |
| シーン/プレハブのYAML衝突 | 1シーン方針＋コード生成UIで構造的に回避（§3） |
| MCP接続のflaky（ポートゾンビ等） | 起動順の型化＋トラブル時はUnity再起動を第一手（§4.1） |
| Unity AIサブスク費用 | OSS版で開始。公式版は必要が証明されてから |

# 9. 今日やる3手（スクショの画面から）

1. テンプレートは **Universal 2D** を選ぶ（選択中のUniversal 3Dではなく——この作品は絵巻の2D）。Project name: `warashibe`、Location: `/Users/tomoki/dev/`（クラウド同期フォルダは避ける）→ Create project
2. T-U01: Gitリポジトリ化（Unity用.gitignoreをClaude Codeに作らせる）
3. T-U02: Unity-MCP（OSS）を導入して接続——最初の指示は「コンソールを読んで要約して」。つながった瞬間から、あとはチケットを1枚ずつ渡すだけ
