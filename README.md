# VRCJapaneseInputSystem

VRChat向けに開発した、UdonやSKK辞書を使用した、日本語入力システムを構成するプログラム群です。
<img width="848" height="758" alt="image" src="https://github.com/user-attachments/assets/0dae4ad5-b445-428c-8d4f-2b5ce4051de2" />


## ライセンス (License)

本リポジトリは、VRChat環境やUnity等での利便性を考慮し、プログラム本体と辞書データで異なるライセンスを適用しています。

### プログラム・3Dモデル等 (MIT License)

本リポジトリに含まれる**すべてのソースコード（C#スクリプト）、Prefab、およびその他のUnityアセット**は、**MIT License** のもとで公開されています。
これにより、VRChatのワールド制作者は、他のプロプライエタリアセットと組み合わせてワールド内に配置し、自由にビルド・公開（配布）することが可能です。

ライセンスの全文については同梱の `LICENSE` ファイルをご確認ください。

### 辞書データ (GPL v2)

本プログラムの変換機能は、SKK辞書の `SKK-JISYO.L` を変換したデータ (`dictionary.txt`) を同梱し、利用しています。
`SKK-JISYO.L` には The Free Software Foundation が発行している **The GNU General Public License version 2 (GPL v2) またはそれ以降** が適用されるため、この辞書データ自体は同ライセンスの下で提供されます。
（※プログラムが実行時に外部データとして辞書を読み込む処理は、プログラム自体のGPL化を強制するものではありません。単なる集積として扱われます。）

* 辞書データ配布元: [skk-dev](https://github.com/skk-dev/dict)
* 辞書データの著作権について: Masahiko Sato, SKK Development Team および各コントリビューターが著作権を有します。詳細な著作権情報については同梱の `Resources/SKK_COPYRIGHT.txt` をご確認ください。

## リポジトリに含まれるもの

* **含まれるもの**: 本機能の実行に必要なプログラム（ソースコード、Prefab等）および、Udon向けに変換された辞書データ。

## 利用時のクレジット表記について

本プログラムを組み込んだUnityプロジェクトやVRChat向けワールドなどをビルドして公開・配布する際は、利用環境内（ワールド内のクレジットボードやメニュー画面など）に、以下のようなクレジットと本リポジトリへのリンクを掲示していただくようお願いいたします。

**クレジット例**
> - KeisXR/VRCJapaneseInputSystem (MIT License): https://github.com/KeisXR/VRCJapaneseInputSystem
> - 内部辞書データとして SKK-JISYO.L (GPL v2) を使用しています: Copyright (C) SKK Development Team and others. https://github.com/skk-dev/dict
