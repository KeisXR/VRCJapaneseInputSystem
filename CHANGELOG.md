# VRCJapaneseInputSystem 更新履歴 / CHANGELOG

## パフォーマンス最適化アップデート

VRChat環境（Udon）特有のスパイク負荷を解消するための更新を行いました。

### 🚀 最適化 (Performance)
* **KanjiConverter**: これまで入力のたびに辞書配列（数万件）を最初から最後までループ検索していた部分を、**O(log N)の二分探索アルゴリズム (`BinarySearch`)** に刷新。
  * これにより探索ループ回数が平均「数万回」から「最大16回程度」へと劇的に減少し、文字入力と変換時のフレーム落ち（ラグ）が解消されました。
* **KanjiConverter**: リニア検索用の `StartsWithOrdinal` メソッドを廃止し、カルチャ依存の誤一致を防ぎつつ二分探索で利用できるカスタム比較メソッド `CompareOrdinal` を実装しました。
* **SKKDictionaryConverter**: ランタイムで二分探索を可能にするため、辞書生成時（`Download SKK Dictionary` ツール使用時）に辞書データをあらかじめキーの Ordinal（文字コード）順にソートして出力するよう変更しました。

### 🐛 バグ修正 (Bug Fixes)
* `KanjiConverter` 内のプレフィックス一致検索を二分探索に置き換えた際、変数スコープの競合によって発生した CS0136 コンパイルエラーを修正しました。
