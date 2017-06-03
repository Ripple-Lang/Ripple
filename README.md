Programming language "Ripple"
======

Ripple is a programming language which is well designed for numerical simulations.

## Caution

This program has only Japanese version.

## Summary

Ripple consists of two principal elements, **language** and **visualization system**.

### Language

Ripple's language specification is focused on the numerical simulations where the state changes with time.

The Ripple's sample code simulating "nezumisan" is as follows ("nezumisan" means "computing proliferation of mice". The number of mice is geometric progression):

```
// Stage
//   - A target object of this simulation
stage n as long; // the number of mice

// Parameter
param c as int;  // the number of mice which one mouse gives birth to

// Initialization
init {
    n<0> = 2; // n<0> means the value of n when time is 0
}

// Operation
//   - Code which is executed every time (like recurrence formula)
operation {
    n<next> = n<now> * c; // "now" and "next" are keywords in Ripple. next == now + 1.
}
```

Example of execution is [here](https://github.com/Ripple-Lang/Ripple#nezumizan).

### Visualization system

You don't have to write any code for visualizing results of simulation. It's very easy in Ripple because the visualizing components are provided as plug-in.

## Building

In order to build Ripple, Visual Studio 2015 or later is required.

From "Developer Command Prompt for Visual Studio", type the following commands.

```
git clone https://github.com/Ripple-Lang/Ripple.git
cd Ripple\Src
MSBuild /t:Rebuild /p:Configuration=Release /m
cd GUI\RippleGUISimulator\bin\x86\Release\
RippleGUISimulator.exe
```

Let's start [Game of Life](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life) simulation.

1. Type [this code][Code_LifeGame_Txt] to editor.
2. Press F5 key.
3. Input time and parameters to the right-side panel. Here, we use 1000, 150, 150, 0.33.
4. Press "開始(S)" button, then simulation starts.
5. After simulation, select "PlainVisualizer" as visualization plug-in. Besides, click "plain" to select it.
6. Press "ビジュアル化(V)" button.
7. You can see visualized result on your screen. Press "再生(P)" to play the animation.

Several sample codes are available from [here][SampleRepos].

## Screenshots

### Nezumizan

![ねずみ算](https://raw.githubusercontent.com/wiki/Ripple-Lang/Ripple/ScreenShots/Mouse_1.PNG)

### Conway's Game of Life

![ライフゲーム(1)](https://raw.githubusercontent.com/wiki/Ripple-Lang/Ripple/ScreenShots/LifeGame_1.PNG)
![ライフゲーム(2)](https://raw.githubusercontent.com/wiki/Ripple-Lang/Ripple/ScreenShots/LifeGame_2.PNG)

### Lotka-Volterra equations

![ロトカ＝ヴォルテラの方程式](https://raw.githubusercontent.com/wiki/Ripple-Lang/Ripple/ScreenShots/LotkaVolterra_1.PNG)

## Copyright

Copyright(C) 2014 Yuya Watari. All rights reserved.

[Code_Mouse]: https://github.com/Ripple-Lang/SampleCodes/blob/master/Codes/%E3%81%AD%E3%81%9A%E3%81%BF%E7%AE%97.txt "ねずみ算のコード"
[Code_LifeGame]: https://github.com/Ripple-Lang/SampleCodes/blob/master/Codes/%E3%83%A9%E3%82%A4%E3%83%95%E3%82%B2%E3%83%BC%E3%83%A0.txt "ライフゲームのコード"
[Code_LifeGame_Txt]: https://raw.githubusercontent.com/Ripple-Lang/SampleCodes/master/Codes/%E3%83%A9%E3%82%A4%E3%83%95%E3%82%B2%E3%83%BC%E3%83%A0.txt "ライフゲームのコード"
[Code_LotkaVolterra]: https://github.com/Ripple-Lang/SampleCodes/blob/master/Codes/%E3%83%AD%E3%83%88%E3%82%AB%EF%BC%9D%E3%83%B4%E3%82%A9%E3%83%AB%E3%83%86%E3%83%A9%E3%81%AE%E6%96%B9%E7%A8%8B%E5%BC%8F.txt "ロトカ＝ヴォルテラの方程式のコード"
[SampleRepos]: https://github.com/Ripple-Lang/SampleCodes "サンプルコードのリポジトリ"
