# VRCJapaneseInputSystem

VRChat向けに開発した、UdonやSKK辞書を使用した、日本語入力システムを構成するプログラム群です。

## ライセンス (License)

このリポジトリに含まれるソースコードおよび辞書データは、**GNU General Public License version 2 (GPL v2) またはそれ以降**のもとで公開されています。

本プログラムは、SKK辞書の `SKK-JISYO.L` を利用して開発されています。
`SKK-JISYO.L` には the Free Software Foundation が発行している the GNU General Public License version 2 以降が適用されるため、本プログラム全体も同ライセンスを継承します。

* 辞書データ配布元: [skk-dev](https://github.com/skk-dev/dict)
* 辞書データの著作権について: Masahiko Sato, SKK Development Team および各コントリビューターが著作権を有します。詳細な著作権情報については同梱の `SKK_COPYRIGHT.txt` をご確認ください。

ライセンスの全文（詳細）については、同梱の `COPYING` ファイルをご覧ください。

## リポジトリに含まれるもの

* **含まれるもの**: 本機能の実行に必要なプログラム（ソースコード）および、Udon向けに変換された辞書データ。

## 利用時のクレジット表記について

本プログラムを組み込んだUnityプロジェクトやVRChat向けワールドなどをビルドして公開・配布する際は、利用環境内（ワールド内のクレジットボードやメニュー画面など）に、以下のようなクレジットと本リポジトリへのリンクを掲示していただくようお願いいたします。

**【クレジット記載例】**
> 本プロジェクトの一部機能には、GPL v2に基づき以下のプログラムを利用しています。
> - KeisXR/VRCJapaneseInputSystem (https://github.com/KeisXR/VRCJapaneseInputSystem)
> - 内部辞書データとして SKK-JISYO.L を使用しています: Copyright (C) SKK Development Team and others. (https://github.com/skk-dev/dict)
