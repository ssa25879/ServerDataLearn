using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace AsyncBreakfast
{
    // 결과물을 뜻하는 빈 클래스들. 이름표일 뿐 안에는 아무것도 없다
    internal class Coffee { }
    internal class Egg { }
    internal class Bacon { }
    internal class Toast { }
    internal class Juice { }
    internal class HashBrown { }

    class Program
    {
        // 2회차 · 5단계 끝나는 대로 받기: WhenAny로 먼저 끝난 것부터 처리한다
        static async Task Main(string[] args)
        {
            var clock = Stopwatch.StartNew();

            Coffee cup = PourCoffee();
            Console.WriteLine("커피 준비 완료");

            Task<Egg> eggsTask = FryEggsAsync(2);
            Task<Bacon> baconTask = FryBaconAsync(3);
            Task<Toast> toastTask = MakeToastWithButterAndJamAsync(2);
            Task<HashBrown> hashBrwonTask = FryHashBrownAsync(2);

            var breakfastTasks = new List<Task> { eggsTask, baconTask, toastTask, hashBrwonTask };
            while (breakfastTasks.Count > 0)
            {
                Task finishedTask = await Task.WhenAny(breakfastTasks);   // 셋 + 해시브라운 중 먼저 끝난 하나
                await finishedTask;                  // 결과를 꺼내고, 실패했으면 여기서 예외가 난다
                if (finishedTask == eggsTask)
                {
                    Console.WriteLine("달걀 준비 완료");
                }
                else if (finishedTask == baconTask)
                {
                    Console.WriteLine("베이컨 준비 완료");
                }
                else if (finishedTask == toastTask)
                {
                    Console.WriteLine("토스트 준비 완료");
                }
                else if (finishedTask == hashBrwonTask)
                {
                    Console.WriteLine("해시브라운 준비 완료");
                }
                breakfastTasks.Remove(finishedTask);
            }

            Juice oj = PourOJ();
            Console.WriteLine("주스 준비 완료");
            Console.WriteLine($"아침 식사 준비 완료! ({clock.Elapsed.TotalSeconds:F1}초)");
        }

        static async Task<HashBrown> FryHashBrownAsync(int patties)
        {
            Console.WriteLine($"해시브라운 {patties}장을 굽습니다.");
            Console.WriteLine("해시브라운의 한 면을 굽는다");
            await Task.Delay(2000);


            for (int i = 1; i <= patties; i++)
            {
                Console.WriteLine("해시브라운을 뒤집는다.");
                await Task.Delay(2000);
            }
            Console.WriteLine("해시브라운의 다른 면을 굽는다.");
            Console.WriteLine("해시브라운을 접시에 담는다.");

            return new HashBrown();
        }

        static async Task<Toast> MakeToastWithButterAndJamAsync(int number)
        {
            Toast toast = await ToastBreadAsync(number);
            ApplyButter(toast);
            ApplyJam(toast);

            return toast;
        }

        private static Juice PourOJ()
        {
            Console.WriteLine("오렌지 주스를 따른다");
            return new Juice();
        }

        private static void ApplyJam(Toast toast) =>
            Console.WriteLine("토스트에 잼을 바른다");

        private static void ApplyButter(Toast toast) =>
            Console.WriteLine("토스트에 버터를 바른다");

        private static async Task<Toast> ToastBreadAsync(int slices)
        {
            for (int slice = 0; slice < slices; slice++)
            {
                Console.WriteLine("식빵 한 장을 토스터에 넣는다");
            }
            Console.WriteLine("굽기 시작...");
            await Task.Delay(2000);
            // 토스터에 불이 나는 실험을 하려면 아래 두 줄의 주석을 푼다
            Console.WriteLine("불이야! 토스트를 망쳤다");
            throw new InvalidOperationException("토스터에 불이 났다");
            await Task.Delay(1000);
            Console.WriteLine("토스트를 꺼낸다");

            return new Toast();
        }

        private static async Task<Bacon> FryBaconAsync(int slices)
        {
            Console.WriteLine($"베이컨 {slices}장을 팬에 올린다");
            Console.WriteLine("베이컨 한 면을 굽는다...");
            await Task.Delay(3000);
            for (int slice = 0; slice < slices; slice++)
            {
                Console.WriteLine("베이컨을 뒤집는다");
            }
            Console.WriteLine("베이컨 다른 면을 굽는다...");
            await Task.Delay(3000);
            Console.WriteLine("베이컨을 접시에 담는다");

            return new Bacon();
        }

        private static async Task<Egg> FryEggsAsync(int howMany)
        {
            Console.WriteLine("달걀 팬을 데운다...");
            await Task.Delay(3000);
            Console.WriteLine($"달걀 {howMany}개를 깬다");
            Console.WriteLine("달걀을 익힌다...");
            await Task.Delay(3000);
            Console.WriteLine("달걀을 접시에 담는다");

            return new Egg();
        }

        private static Coffee PourCoffee()
        {
            Console.WriteLine("커피를 따른다");
            return new Coffee();
        }
    }
}