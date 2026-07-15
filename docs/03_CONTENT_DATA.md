# 03_CONTENT_DATA — 吉備の旅 完全データ（実装用JSON最終稿）
*Status: v3.0 / このJSONをそのまま `src/data/routes/kibi-01/` に配置する。セリフは最終稿（変更時はライティング6原則で校閲）*

# 1. items.json

```json
[
  { "id": "item_wara", "name": "わら", "name_ruby": "わら", "emoji": "🌾",
    "origin": "吉備の田んぼ", "baseValue": 1,
    "trivia": "おこめを とったあとの くき。むかしは やねや ぞうりの ざいりょうに した。" },
  { "id": "item_abu", "name": "アブ", "name_ruby": "あぶ", "emoji": "🪰",
    "origin": "吉備津神社", "baseValue": 1,
    "trivia": "ブーンと とぶ むし。うるさいけれど、この たびでは だいかつやく？" },
  { "id": "item_abumushi_toy", "name": "アブつきわらのおもちゃ", "name_ruby": "あぶつきわらのおもちゃ", "emoji": "🧸",
    "origin": "じぶんで つくった", "baseValue": 1,
    "trivia": "わらに アブを むすんだだけ。でも くるくる とんで、こどもには たからもの！" },
  { "id": "item_kibidango", "name": "きびだんご", "name_ruby": "きびだんご", "emoji": "🍡",
    "origin": "岡山（おかやま）", "baseValue": 2,
    "trivia": "おかやまの めいぶつ。ももたろうも これを もって たびに でた。" },
  { "id": "item_bizenyaki", "name": "備前焼の小鉢", "name_ruby": "びぜんやきの こばち", "emoji": "🏺",
    "origin": "岡山・備前（びぜん）", "baseValue": 3,
    "trivia": "くすりを ぬらずに やく、ちゃいろい やきもの。1000ねん つづく でんとう。" },
  { "id": "item_hanpu", "name": "倉敷帆布の反物", "name_ruby": "くらしきはんぷの たんもの", "emoji": "🧵",
    "origin": "岡山・倉敷（くらしき）", "baseValue": 3,
    "trivia": "ふねの ほにも つかう、とても じょうぶな ぬの。くらしきは ぬのの まち。" },
  { "id": "item_tai", "name": "立派な鯛", "name_ruby": "りっぱな たい", "emoji": "🐟",
    "origin": "瀬戸内海（せとないかい)", "baseValue": 3,
    "trivia": "おいわいの さかな。せとないかいは さかなの たからばこ。" },
  { "id": "item_uma_weak", "name": "弱った馬", "name_ruby": "よわった うま", "emoji": "🐴",
    "origin": "山陽道（さんようどう）", "baseValue": 2,
    "trivia": "つかれて うごけない。おせわを したら、げんきに なるかな？" },
  { "id": "item_uma", "name": "元気な馬", "name_ruby": "げんきな うま", "emoji": "🐎",
    "origin": "山陽道（さんようどう）", "baseValue": 4,
    "trivia": "おせわで げんきに なった！ てまを かけると、かちは あがる。" },
  { "id": "item_yashiki", "name": "屋敷", "name_ruby": "やしき", "emoji": "🏠",
    "origin": "姫路（ひめじ）への かいどう", "baseValue": 5,
    "trivia": "わら いっぽんから、ここまで きた！ こうかんの ちからは すごい。" }
]
```

# 2. recipes.json

```json
[ { "inputs": ["item_wara", "item_abu"], "output": "item_abumushi_toy" } ]
```

# 3. route.json

```json
{
  "id": "route_kibi_01",
  "title": "吉備の旅",
  "startItem": "item_wara",
  "goalItem": "item_yashiki",
  "stops": ["loc_tanbo", "loc_kibitsu", "loc_touge", "loc_kurashiki",
            "loc_minato", "loc_shukuba", "loc_kaido"]
}
```

# 4. stops.json

```json
[
  { "id": "loc_tanbo",    "name": "吉備の田んぼ",   "region": "岡山", "mapX": 200,  "bg": "bg_tanbo",    "npcIds": ["npc_grandma"] },
  { "id": "loc_kibitsu",  "name": "吉備津神社の参道", "region": "岡山", "mapX": 700,  "bg": "bg_kibitsu",  "npcIds": ["npc_mother", "npc_child"], "ambientEvent": "ev_abu_catch" },
  { "id": "loc_touge",    "name": "峠の茶屋",       "region": "岡山", "mapX": 1200, "bg": "bg_touge",    "npcIds": ["npc_merchant"] },
  { "id": "loc_kurashiki","name": "倉敷の川辺",     "region": "倉敷", "mapX": 1750, "bg": "bg_kurashiki","npcIds": ["npc_musume"] },
  { "id": "loc_minato",   "name": "瀬戸内の港",     "region": "瀬戸内", "mapX": 2300, "bg": "bg_minato",  "npcIds": ["npc_ryoshi"] },
  { "id": "loc_shukuba",  "name": "山陽道の宿場",   "region": "山陽道", "mapX": 2850, "bg": "bg_shukuba", "npcIds": ["npc_samurai"] },
  { "id": "loc_kaido",    "name": "姫路への街道",   "region": "姫路", "mapX": 3400, "bg": "bg_kaido",    "npcIds": ["npc_master"] }
]
```

# 5. npcs.json（前半: ストップ0〜3）

```json
[
  {
    "id": "npc_grandma", "name": "おばあさん", "portrait": "pt_grandma",
    "intro": [
      "おやおや、はでに ころんだねえ。",
      "その てに にぎった わら…… それも なにかの えん じゃ。",
      "たいせつに しんさい。おもいがけない ものに かわるかも しれんよ。"
    ],
    "idleLine": "いってらっしゃい。きをつけてな。",
    "questions": [], "accepts": [],
    "declineLines": ["", "", ""], "hintL2": "", "hintL3": "",
    "afterTradeLine": ""
  },
  {
    "id": "npc_child", "name": "泣いている子", "portrait": "pt_child",
    "intro": [
      "うえーん！ うえーん！",
      "（こどもは ないている。でも、めは なにかを おいかけている……）"
    ],
    "idleLine": "ぐすっ…… ひっく……",
    "questions": [
      { "q": "どうして ないてるの？", "a": "「たいくつなの！ じんじゃ、つまんないもん！」" },
      { "q": "なにが すき？", "a": "「うごくもの！ ぴょんぴょん くるくる するの！」" }
    ],
    "accepts": [
      {
        "item": "item_abumushi_toy", "valueForNpc": 5,
        "reasonLine": "「くるくる とんでる！ たからもの！」",
        "gives": "item_kibidango",
        "acceptLines": [
          "「わあっ！ とんでる！ くるくる まわってる！」",
          "こどもは なきやんで、めを かがやかせた。"
        ]
      }
    ],
    "declineLines": [
      "「いらない…… もっと たのしいのが いい」",
      "「うごく おもちゃが ほしいの！」",
      "「……」"
    ],
    "hintL2": "ぶんぶん…… あの子、さっきから ぼくたちの ほうを 見てない？ 空の あたり……",
    "hintL3": "そうだ！ アブを つかまえて、わらと あわせて みたら？ くるくる とぶ おもちゃに なるかも！",
    "highlightTarget": "ev_abu_catch",
    "afterTradeLine": "「みてみて！ とんでるよ！」"
  },
  {
    "id": "npc_mother", "name": "おかあさん", "portrait": "pt_mother",
    "intro": [
      "すみません、うちの子が ないてしまって……",
      "おまいりも まだ おわらないのに、こまったわ。"
    ],
    "idleLine": "この子、いちど なきだすと ながいのよ……",
    "questions": [
      { "q": "おまいり、たいへん？", "a": "「ええ。この子が しずかに してくれたら いいのだけど」" },
      { "q": "なにか できることは？", "a": "「この子の きが まぎれたら、たすかるわ」" }
    ],
    "accepts": [
      {
        "item": "item_abumushi_toy", "valueForNpc": 5,
        "reasonLine": "「この子が わらったのは ひさしぶり！」",
        "gives": "item_kibidango",
        "acceptLines": [
          "「まあ！ ありがとう、たびの ひと。」",
          "「おれいに、これを。きびの めいぶつ、きびだんごよ。」"
        ]
      }
    ],
    "declineLines": [
      "「ありがたいけれど、いま ほしいのは この子の えがおなの」",
      "「この子が よろこぶものなら…… ねえ、なにを 見ているのかしら」",
      "「……」"
    ],
    "hintL2": "おかあさんは『この子が よろこぶもの』って いってたね。ぶんぶん。",
    "hintL3": "アブを つかまえて わらと あわせよう！ 子どもに わたすんだ！",
    "highlightTarget": "ev_abu_catch",
    "afterTradeLine": "「ほんとうに たすかったわ。よい たびを！」"
  },
  {
    "id": "npc_merchant", "name": "行商人", "portrait": "pt_merchant",
    "intro": [
      "はあ…… はらが へって、ちからが でん……",
      "くらしきまで いそぎの あきないなのに、ひるめしを くいそこねた。",
      "ちゃやに よる ひまも ないんじゃ。"
    ],
    "idleLine": "はらへった…… はらへった……",
    "questions": [
      { "q": "なにを うってるの？", "a": "「びぜんの やきものよ。かまもとから しいれた、いいしなじゃ」" },
      { "q": "どこへ いくの？", "a": "「くらしきよ。ぬのの まちには きゃくが おおいでな」" }
    ],
    "accepts": [
      {
        "item": "item_kibidango", "valueForNpc": 5,
        "reasonLine": "「はらぺこには なによりの ごちそう！」",
        "gives": "item_bizenyaki",
        "acceptLines": [
          "「おお！ きびだんご！ いただこう いただこう！」",
          "「……うまい！ ちからが わいてきた。おれいに この こばちを もっていけ。」",
          "「びぜんやき じゃ。われんように、だいじにな。」"
        ]
      }
    ],
    "declineLines": [
      "「すまんが、いま ほしいのは くいものだけ じゃ」",
      "「はらに たまるものは もっとらんかね？」",
      "「……ぐう（おなかの おと）」"
    ],
    "hintL2": "『はらがへった』って いってたよ。たべものを もってないかな？ ぶんぶん。",
    "hintL3": "きびだんごを わたして あげよう！",
    "highlightTarget": "item_kibidango",
    "afterTradeLine": "「よい あきないを！ くらしきで また あおう！」"
  }
]
```

*（後半のNPC・イベント・UI文字列は §6〜§8 に続く）*

# 6. npcs.json（後半: ストップ4〜6）

```json
[
  {
    "id": "npc_musume", "name": "布問屋の娘", "portrait": "pt_musume",
    "intro": [
      "どうしましょう…… たいせつな ちゃかいで つかう うつわを、わってしまったの。",
      "きょうじゅうに かわりが いるのに、まちの みせは どこも しまっていて……"
    ],
    "idleLine": "おきゃくさまが くるまでに、なんとか しないと……",
    "questions": [
      { "q": "どんな うつわが いるの？", "a": "「おきゃくさまは やきものに くわしい かたなの。ちゃんとした ものでないと」" },
      { "q": "おしごとは なに？", "a": "「ぬのの とんやよ。くらしきの ぬのは にっぽんいち、じょうぶなの」" }
    ],
    "accepts": [
      {
        "item": "item_bizenyaki", "valueForNpc": 5,
        "reasonLine": "「びぜんやきなら おきゃくさまも きっと まんぞく！」",
        "gives": "item_hanpu",
        "acceptLines": [
          "「まあ……！ これは びぜんやき ではないの！」",
          "「たすかったわ。おれいに、うちで おった はんぷの たんものを どうぞ。」",
          "「じょうぶさは にっぽんいちよ。」"
        ]
      }
    ],
    "declineLines": [
      "「ごめんなさい。いま いるのは、ちゃかいで つかえる ものなの」",
      "「やきものに くわしい おきゃくさまに おだしできる もの…… ないかしら」",
      "「……こまったわ」"
    ],
    "hintL2": "『やきもの』って いってたね。ぼくたち、やきもの もってなかったっけ？ ぶんぶん。",
    "hintL3": "びぜんやきの こばちを わたそう！",
    "highlightTarget": "item_bizenyaki",
    "afterTradeLine": "「ちゃかい、きっと うまくいくわ。ありがとう！」"
  },
  {
    "id": "npc_ryoshi", "name": "漁師", "portrait": "pt_ryoshi",
    "intro": [
      "ちくしょう、ほが やぶれちまった！",
      "これじゃ ふねが だせん。りょうに いけなきゃ、おまんまの くいあげだ。",
      "じょうぶな ぬのが あれば、なおせるんだがなあ。"
    ],
    "idleLine": "かぜは いいのに、ほが これじゃあなあ……",
    "questions": [
      { "q": "ほって なに？", "a": "「ふねの うえに はる おおきな ぬのよ。かぜを うけて ふねを はしらせるんだ」" },
      { "q": "どんな ぬのが いるの？", "a": "「やわな ぬのじゃ すぐ やぶれる。うんと じょうぶな やつが いる」" }
    ],
    "accepts": [
      {
        "item": "item_hanpu", "valueForNpc": 5,
        "reasonLine": "「くらしきの はんぷなら かぜにも まける もんか！」",
        "gives": "item_tai",
        "acceptLines": [
          "「こ、これは くらしきの はんぷ！ さいこうの ほに なるぞ！」",
          "「おれいだ、けさ あがった いちばん りっぱな たいを もってけ！」",
          "「ついでだ、むこうぎしまで ふねで おくってやろう。」"
        ],
        "postEvent": "ev_boat_ride"
      }
    ],
    "declineLines": [
      "「わるいが、いま いるのは ほを なおす ものだ」",
      "「じょうぶ〜な ぬのは もってねえか？」",
      "「……かぜが もったいねえなあ」"
    ],
    "hintL2": "『じょうぶな ぬの』だって。にもつに ぬの、あったよね？ ぶんぶん。",
    "hintL3": "はんぷの たんものを わたそう！",
    "highlightTarget": "item_hanpu",
    "afterTradeLine": "「たいりょう たいりょう！ あんたの おかげだ！」"
  },
  {
    "id": "npc_samurai", "name": "急ぎの侍", "portrait": "pt_samurai",
    "intro": [
      "むう…… こまった。うまが つかれて うごかぬ。",
      "それがしは いそぎの つとめで ひめじへ まいらねば ならん。",
      "だが この うまを みすてても いけぬ…… ぬしどの、ちえは ないか。"
    ],
    "idleLine": "うまよ、すまぬな……",
    "questions": [
      { "q": "おうまさん、だいじょうぶ？", "a": "「ながたびで つかれきって おる。みずと やすみが いるだろう」" },
      { "q": "おさむらいさんは どうするの？", "a": "「あるいてでも ゆかねば ならん。だが たびの くいものが つきてしまってな」" }
    ],
    "accepts": [
      {
        "item": "item_tai", "valueForNpc": 5,
        "reasonLine": "「これほどの たいなら みちちゅうの かてに じゅうぶん！」",
        "gives": "item_uma_weak",
        "acceptLines": [
          "「おお、みごとな たい！ これが あれば あるいて ゆける。」",
          "「ぬしどの、この うまを たのむ。せわを してやって くれ。」",
          "「げんきに なったら、そなたの うまに するが よい。さらばだ！」"
        ],
        "postEvent": "ev_horse_care"
      }
    ],
    "declineLines": [
      "「かたじけないが、いま いるのは たびの かてだ」",
      "「くいものは もっておらぬか。それがし、はらが へっては うごけぬ」",
      "「……むう」"
    ],
    "hintL2": "おさむらいさん、『たびのくいもの』が ほしいんだって。ぶんぶん。",
    "hintL3": "たいを わたそう！ おさむらいさんの たびの ごはんに なるよ！",
    "highlightTarget": "item_tai",
    "afterTradeLine": "（さむらいは もう とおくへ いってしまった）"
  },
  {
    "id": "npc_master", "name": "大店の主人", "portrait": "pt_master",
    "intro": [
      "ほう、みごとな うまじゃ。",
      "わしは これから ながい たびに でる。はるまで もどらん。",
      "あしの つよい うまが どうしても いるのじゃが…… ぬしどの、そのうま、ゆずっては くれんかね。"
    ],
    "idleLine": "はるに なったら もどってくる つもりじゃ。",
    "questions": [
      { "q": "おうちは どうするの？", "a": "「るすの あいだ、この やしきを まもってくれる ひとが おらんで こまっとる」" },
      { "q": "どこへ いくの？", "a": "「にしの くにへ、おおきな あきないの たびじゃ」" }
    ],
    "accepts": [
      {
        "item": "item_uma", "valueForNpc": 5,
        "reasonLine": "「これほど げんきな うまは めったに おらん！」",
        "gives": "item_yashiki",
        "acceptLines": [
          "「おお、なんと げんきの よい うま！ きに いった！」",
          "「では やくそくじゃ。わしが もどるまで、この やしきを ぬしどのに あずけよう。」",
          "「にわの かきも、くらの こめも、すきに つかうが よい。」",
          "「…… わら いっぽんから、ようも ここまで きたものよのう。」"
        ]
      }
    ],
    "declineLines": [
      "「ふむ、それは いらんのう。わしが いるのは うまじゃ」",
      "「げんきな うまで なければ、ながたびは できんのじゃ」",
      "「……よわった うまでは だめじゃぞ？」"
    ],
    "hintL2": "げんきな うまが ほしいんだって。あの うま、まだ よわってない？ ぶんぶん。",
    "hintL3": "さきに うまの おせわを しよう！ みずを のませる ばしょを さがすんだ！",
    "highlightTarget": "ev_horse_care",
    "afterTradeLine": "「やしきを たのんだぞ、わかき ちょうじゃどの！」"
  }
]
```

# 7. events.json（ミニイベント定義）

```json
[
  {
    "id": "ev_abu_catch", "type": "tap_catch",
    "trigger": "loc_kibitsu入場時から常時（未捕獲の間）",
    "spec": { "taps_required": 3, "hitbox_scale": 2.0, "path": "figure8_slow",
              "slowdown_per_tap": 0.35 },
    "lines_on_success": ["つかまえた！", "『ぶん、っていうんだ。つれてって！』",
                         "（ぶんが なかまに なった！ いごとの たびに ついてくる）"],
    "gives": "item_abu"
  },
  {
    "id": "ev_horse_care", "type": "map_choice",
    "trigger": "item_uma_weak入手直後に自動開始",
    "spec": { "prompt": "うまに みずを のませよう。どこへ つれていく？",
              "choices": [
                { "label": "かわの ほとり", "correct": true,
                  "result": "うまは ごくごく みずを のみ、ゆっくり やすんだ。" },
                { "label": "いわばの さか", "correct": false,
                  "result": "ここには みずが ないみたい。（ぶん『かわは どっちかな？』）" }
              ],
              "retry_until_correct": true },
    "on_complete": { "replace_item": ["item_uma_weak", "item_uma"],
      "lines": ["うまは げんきに いななきを あげた！", "（よわった馬 → 元気な馬 に かわった！）",
                "ぶん『てまを かけたら、かちが あがったね！』"] }
  },
  {
    "id": "ev_boat_ride", "type": "cutscene",
    "spec": { "duration_ms": 3000, "skippable_after_ms": 800,
              "visual": "ふねで うみを わたる よこスクロール演出",
              "line": "ぶん『うみの うえは きもちいいねえ！』" },
    "on_complete": { "advance_to": "loc_shukuba" }
  }
]
```

# 8. strings.ja.json（UI文字列・抜粋必須分）

```json
{
  "btn_start": "はじめる", "btn_continue": "つづきから",
  "btn_talk": "はなす", "btn_ask": "しつもん", "btn_offer": "こうかん", "btn_bag": "にもつ",
  "btn_combine": "くみあわせる", "btn_map": "ちず", "btn_replay": "もういちど", "btn_share": "みんなにみせる",
  "offer_prompt": "なにを さしだす？",
  "offer_dup_bun": "それは さっき みせたよ",
  "combine_fail_bun": "うーん、なにも おきないみたい",
  "meter_mine": "あなたにとって", "meter_theirs": "あいてにとって",
  "clear_title": "たびの おわり",
  "rank_choja": "ちょうじゃ", "rank_daishonin": "おおあきんど",
  "rank_gyoshonin": "ぎょうしょうにん", "rank_minarai": "みならい",
  "save_broken": "たびの きろくが みつからなかったので、はじめから はじめるよ",
  "error_title": "おや、みちに まよったようだ", "error_btn": "はじめの まちへ もどる",
  "nudge_bun": "ぶんぶん…… よく みてみよ？",
  "tut_ask_bun": "きいてみたら？"
}
```

- `tut_ask_bun`: §7 チュートリアル「しつもん」のぶん一言（原文「聞いてみたら？」を児童向けかな＝ルビ不要表記に）。T-U09で追加。※要PO確認。

# 9. データ整合性チェックリスト（CIのvalidateRouteが機械検証する項目の正）

1. route.stops の全IDが stops.json に存在する
2. startItem から gives 連鎖（＋recipe）を辿って goalItem に到達可能（グラフ探索）
3. 全AcceptRuleで valueForNpc > baseValue（価値メーターの教育的整合）
4. 全NPC: declineLines=3件・questions≤2件・hintL2/L3非空（accepts空のNPCは除外）
5. 参照される全 item/npc/event/portrait/bg IDの実在
6. 全表示文字列: 一文40字以内・小3語彙リスト外の漢字にルビ・カタカナ語検出（許可リスト方式）
7. highlightTarget の実在（itemId or eventId）

# 10. observables（v3.2追加・npcs.jsonの該当NPCに追記するフィールド）

```json
{
  "npc_merchant": [{ "target": "obs_nifuda", "bunLine": "せなかの にふだ…『くらしき ゆき』って かいてある。おもそう…" }],
  "npc_musume":   [{ "target": "obs_kakera", "bunLine": "あしもとに われた かけら…… やきものだったんだね" }],
  "npc_ryoshi":   [{ "target": "obs_ho",     "bunLine": "ほに おおきな あな！ かぜが ぬけちゃうね" }],
  "npc_samurai":  [{ "target": "obs_bento",  "bunLine": "べんとうづつみが からっぽだ。おなか すいてそう…" }]
}
```
- タップで気づき演出＋観察ボーナス対象（01_GDD §4）。**答えは言わない**（ヒント梯子とは独立系統）
- §9チェックリストに追加: **第8項: gives連鎖のチェーン序数が単調増加（postEvent強化取引は半段の例外扱い）／第9項: observablesのtarget実在**
