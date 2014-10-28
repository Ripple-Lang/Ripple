///
/// このコードは、Ripple.Core.Compiler 0.0.1.0 によって自動的に生成されました。
///

namespace __N1
{
    public class __C1 : Ripple.Components.ISimulation
    {
        public event Ripple.Components.__OnTimeChangedEventHandler __OnTimeChanged;

        public System.Double[][][] @desert;

        public System.Int32 @width { get; set; }

        public System.Int32 @wind { get; set; }

        public System.Double @amount { get; set; }

        public System.Double @collapse { get; set; }

        private System.Double @initmin
        {
            get
            {
                return 3;
            }
        }

        private System.Double @initmax
        {
            get
            {
                return 4;
            }
        }

        public void @__Initialize(System.Int32 @__maxtime)
        {
            @desert = new System.Double[__maxtime + 1][][];
            System.Int32 __now = -1;
            @desert[__now + 1] = new System.Double[@width][];

            for (int __tempver_0 = 0; __tempver_0 < @width; __tempver_0++)
            {
                @desert[__now + 1][__tempver_0] = new System.Double[@width];
            }

            // コンパイラが生成したコメント - ステージのキャッシュコード
            var @__cached_desert_1 = @desert[__now + 1];

            // コンパイラが生成したコメント - ユーザーによるinitコード
            {
                for (int @i_10 = (0); @i_10 < (@__cached_desert_1.Length); @i_10++)
                    for (int @j_11 = (0); @j_11 < (@__cached_desert_1[i_10].Length); @j_11++)
                    {
                        @__cached_desert_1[@i_10][@j_11] = (Ripple.Components.Libraries.Randoms.GetRandomDouble(@initmin, @initmax));
                    }
            }
        }

        public void @__Run(System.Int32 @__maxtime)
        {

            // コンパイラが生成したコメント - シミュレーション中変化しないパラメーターのキャッシュコード
            var @width = this.@width;
            var @wind = this.@wind;
            var @amount = this.@amount;
            var @collapse = this.@collapse;
            var @initmin = this.@initmin;
            var @initmax = this.@initmax;

            // コンパイラが生成したコメント - 各時刻で実行するコード
            for (int @__now = 0; @__now < @__maxtime; @__now++)
            {
                {
                    var __event_OnTimeChanged = __OnTimeChanged;
                    if (__event_OnTimeChanged != null)
                    {
                        __event_OnTimeChanged(this, __now);
                    }
                }
                // コンパイラが生成したコメント - 各ステージのメモリー領域を確保するコード
                @desert[__now + 1] = new System.Double[@width][];

                for (int __tempver_1 = 0; __tempver_1 < @width; __tempver_1++)
                {
                    @desert[__now + 1][__tempver_1] = new System.Double[@width];
                }

                // コンパイラが生成したコメント - ステージのキャッシュコード
                var @__cached_desert_0 = @desert[__now];
                var @__cached_desert_1 = @desert[__now + 1];

                // コンパイラが生成したコメント - ユーザーによるoperationコード
                {
                    for (int @i_14 = (0); @i_14 < (@__cached_desert_1.Length); @i_14++)
                        for (int @j_15 = (0); @j_15 < (@__cached_desert_1[i_14].Length); @j_15++)
                        {
                            @__cached_desert_1[@i_14][@j_15] = (@__cached_desert_0[@i_14][@j_15]);
                        }
                    for (int @i_18 = (0); @i_18 < (@__cached_desert_1.Length); @i_18++)
                        for (int @j_19 = (0); @j_19 < (@__cached_desert_1[i_18].Length); @j_19++)
                        {
                            System.Double amountFromHere_20 = (__N1.__C1.@min(@__cached_desert_0[@i_18][@j_19], @amount));
                            System.Int32 fallpoint_21 = ((((@j_19) + (__N1.__C1.@round(@__cached_desert_0[@i_18][@j_19]))) + (@wind)) % (@width));
                            @__cached_desert_1[@i_18][@fallpoint_21] = ((@__cached_desert_1[@i_18][@fallpoint_21]) + (@amountFromHere_20));
                            @__cached_desert_1[@i_18][@j_19] = ((@__cached_desert_1[@i_18][@j_19]) - (@amountFromHere_20));
                        }
                    for (int @i_24 = (0); @i_24 < (@__cached_desert_1.Length); @i_24++)
                        for (int @j_25 = (0); @j_25 < (@__cached_desert_1[i_24].Length); @j_25++)
                        {
                            System.Double here_26 = (@__cached_desert_1[@i_24][@j_25]);
                            System.Double up_27 = (((@i_24) == (0)) ? (0) : ((@__cached_desert_1[(@i_24) - (1)][@j_25]) - (@here_26)));
                            System.Double left_28 = (((@j_25) == (0)) ? ((@__cached_desert_1[@i_24][(@width) - (1)]) - (@here_26)) : ((@__cached_desert_1[@i_24][(@j_25) - (1)]) - (@here_26)));
                            System.Double right_29 = (((@j_25) == ((@width) - (1))) ? ((@__cached_desert_1[@i_24][0]) - (@here_26)) : ((@__cached_desert_1[@i_24][(@j_25) + (1)]) - (@here_26)));
                            System.Double down_30 = (((@i_24) == ((@width) - (1))) ? (0) : ((@__cached_desert_1[(@i_24) + (1)][@j_25]) - (@here_26)));
                            @__cached_desert_1[@i_24][@j_25] = ((@__cached_desert_1[@i_24][@j_25]) + ((@collapse) * ((((@up_27) + (@left_28)) + (@right_29)) + (@down_30))));
                        }
                }
            }
        }

        public static System.Double @min(System.Double @a, System.Double @b)
        {
            return ((@a) < (@b)) ? (@a) : (@b);
        }

        public static System.Int32 @round(System.Double @x)
        {
            return (System.Int32)((@x) + (0.5));
        }

    }
}