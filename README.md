プログラミング言語「Ripple」
======

Rippleは、数値シミュレーションを簡単に記述するためのプログラミング言語、およびその処理系です。

## 経緯

Rippleは作者が東京工業大学情報工学科の授業「[情報工学創作実習](http://www.ocw.titech.ac.jp/index.php?module=General&action=T0300&GakubuCD=101&GakkaCD=53&KougiCD=7230&lang=JA)」で作成したものです。

中学生・高校生やプログラミング初心者の利用を想定して、数値シミュレーションを簡単に記述するという目的のもと開発しました。

## 概要

(この項目は書きかけです)

Rippleは**「言語」**と**「ビジュアル化システム」**の二大要素により構成されます。

### 言語

Rippleの言語仕様は、時間推移とともに状態が変化していくような数値シミュレーションの記述に特化しています。

[ねずみ算](http://ja.wikipedia.org/wiki/%E3%81%AD%E3%81%9A%E3%81%BF%E7%AE%97)を例にとり、増え続けるネズミの数をシミュレーションするコードは次のようになります(ねずみの数が単位時間ごとに何倍かになるというシミュレーションです)。

```
// ステージ
//   - 「ネズミの数」
stage n as long;

// パラメーター
//   - 「一匹のネズミが産む子どもの数」
//     値を初期化していないので、
//     シミュレーション開始時に入力するよう促される
param c as int;

// 初期化
init {
    n<0> = 2;
}

// 各時刻で実行するコード
operation {
    n<next> = n<now> * c;
}
```

「ネズミの数」のようなシミュレーションの直接の対象は**stage**と呼ばれます。ここでは、``n``がそれに相当します。

stageに対して各時刻ごとに行う処理は、**operation**という形で記述します。operationは漸化式のようなものです。ここでは、``n``を``c``倍しています。

その``c``(ネズミの数が何倍になるか)のようにシミュレーションに影響を与える変数は**param**(パラメーター)として宣言します。

上記コードを実行する様子は[こちら](https://github.com/Ripple-Lang/Ripple#%E3%81%AD%E3%81%9A%E3%81%BF%E7%AE%97)です。

### ビジュアル化システム

シミュレーションの結果は目に見える形にする必要がありますが、そのためにコードを書き足す必要はありません。Rippleでは、シミュレーション結果を簡単にビジュアル化(可視化)できます。

シミュレーションのコードを書いて実行すると、ビジュアル化ツールの選択を促されます。適切なツール(先のねずみ算の例では、「折れ線グラフとして表示するツール」など)を選択すれば、簡単にビジュアル化できます。

ビジュアル化を行うツールはプラグインとして提供され、言語本体とは切り離されています。そのため拡張性があります。

## 特徴

* **高パフォーマンス**

  Rippleは静的型付け言語です。いったんC#コードへ変換されたのち、C#コンパイラにより共通中間言語(CIL)にコンパイルされます。実行時には機械語へJITコンパイルされるため、高いパフォーマンスが得られます。

  また、(簡易的ですが)ループを**並列処理**することが簡単にできます。

* **シンプルな記述**

  記述するシミュレーションの内容にもよりますが、言語レベルで数値シミュレーションに特化しているため、シンプルに記述できる可能性があります。また、シミュレーション結果をビジュアル化するためにコードを書き足す必要はありません。
  
  これらは、MATLABなどの言語にある利点と似ています。

## 使用法

(この項目は書きかけです)

## プログラム例・スクリーンショット

### ねずみ算

ねずみ算のシミュレーションコードは[こちら][Code_Mouse]です。

![ねずみ算](https://raw.githubusercontent.com/wiki/Ripple-Lang/Ripple/ScreenShots/Mouse_1.PNG)

### ライフゲーム

[ライフゲーム](http://ja.wikipedia.org/wiki/%E3%83%A9%E3%82%A4%E3%83%95%E3%82%B2%E3%83%BC%E3%83%A0)のシミュレーションコードは[こちら][Code_LifeGame]です。

![ライフゲーム(1)](https://raw.githubusercontent.com/wiki/Ripple-Lang/Ripple/ScreenShots/LifeGame_1.PNG)
![ライフゲーム(2)](https://raw.githubusercontent.com/wiki/Ripple-Lang/Ripple/ScreenShots/LifeGame_2.PNG)

### ロトカ＝ヴォルテラの方程式

[ロトカ＝ヴォルテラの方程式](http://ja.wikipedia.org/wiki/%E3%83%AD%E3%83%88%E3%82%AB%EF%BC%9D%E3%83%B4%E3%82%A9%E3%83%AB%E3%83%86%E3%83%A9%E3%81%AE%E6%96%B9%E7%A8%8B%E5%BC%8F)にしたがって、食う・食われるの関係にある個体の数をシミュレーションします。Rippleのコードは[こちら][Code_LotkaVolterra]です。

![ロトカ＝ヴォルテラの方程式](https://raw.githubusercontent.com/wiki/Ripple-Lang/Ripple/ScreenShots/LotkaVolterra_1.PNG)

## その他

Copyright(C) 2014 Yuya Watari. All rights reserved.

[Code_Mouse]: https://github.com/Ripple-Lang/SampleCodes/blob/master/Codes/%E3%81%AD%E3%81%9A%E3%81%BF%E7%AE%97.txt "ねずみ算のコード"
[Code_LifeGame]: https://github.com/Ripple-Lang/SampleCodes/blob/master/Codes/%E3%83%A9%E3%82%A4%E3%83%95%E3%82%B2%E3%83%BC%E3%83%A0.txt "ライフゲームのコード"
[Code_LotkaVolterra]: https://github.com/Ripple-Lang/SampleCodes/blob/master/Codes/%E3%83%AD%E3%83%88%E3%82%AB%EF%BC%9D%E3%83%B4%E3%82%A9%E3%83%AB%E3%83%86%E3%83%A9%E3%81%AE%E6%96%B9%E7%A8%8B%E5%BC%8F.txt "ロトカ＝ヴォルテラの方程式のコード"
