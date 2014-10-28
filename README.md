プログラミング言語「Ripple」
======

Rippleは、数値シミュレーションを簡単に記述するためのプログラミング言語、およびその処理系です。

## 経緯

Rippleは作者が東京工業大学情報工学科の授業「[情報工学創作実習](http://www.ocw.titech.ac.jp/index.php?module=General&action=T0300&GakubuCD=101&GakkaCD=53&KougiCD=7230&lang=JA)」で作成したものです。

## 概要

(詳細はWikiなどに記載する予定です。この項目は書きかけです)

Rippleの言語仕様は、時間推移とともに状態が変化していくような数値シミュレーションの記述に特化しています。主に、中学生・高校生やプログラミング初心者の利用を想定しています。

作成した主な構成物は次の通りです。

* プログラミング言語そのもの(Ripple)
* Rippleのコンパイラ
* シミュレーション結果をビジュアル化(可視化)するシステム
  - ビジュアル化部分は言語本体とは切り離されています。ビジュアル化を行うツールはプラグインとして提供されるため拡張性があります。
* 上記を束ねるGUIツール

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

### ライフゲーム

[ライフゲーム](http://ja.wikipedia.org/wiki/%E3%83%A9%E3%82%A4%E3%83%95%E3%82%B2%E3%83%BC%E3%83%A0)のシミュレーションコードは[こちら](https://github.com/Ripple-Lang/SampleCodes/blob/master/%E3%83%A9%E3%82%A4%E3%83%95%E3%82%B2%E3%83%BC%E3%83%A0.txt)です。

![ライフゲーム(1)](https://raw.githubusercontent.com/wiki/Ripple-Lang/Ripple/ScreenShots/LifeGame_1.PNG)
![ライフゲーム(2)](https://raw.githubusercontent.com/wiki/Ripple-Lang/Ripple/ScreenShots/LifeGame_2.PNG)

### ロトカ＝ヴォルテラの方程式

[ロトカ＝ヴォルテラの方程式](http://ja.wikipedia.org/wiki/%E3%83%AD%E3%83%88%E3%82%AB%EF%BC%9D%E3%83%B4%E3%82%A9%E3%83%AB%E3%83%86%E3%83%A9%E3%81%AE%E6%96%B9%E7%A8%8B%E5%BC%8F)にしたがって、食う・食われるの関係にある個体の数をシミュレーションします。Rippleのコードは[こちら](https://github.com/Ripple-Lang/SampleCodes/blob/master/%E3%83%AD%E3%83%88%E3%82%AB%EF%BC%9D%E3%83%B4%E3%82%A9%E3%83%AB%E3%83%86%E3%83%A9%E3%81%AE%E6%96%B9%E7%A8%8B%E5%BC%8F.txt)です。

![ロトカ＝ヴォルテラの方程式](https://raw.githubusercontent.com/wiki/Ripple-Lang/Ripple/ScreenShots/LotkaVolterra_1.PNG)
