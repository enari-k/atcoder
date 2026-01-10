using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace AHC_StackToMatch
{
    public class FastScanner {
        private readonly Stream _stream;
        private readonly byte[] _buffer = new byte[1024];
        private int _ptr = 0, _buflen = 0;
        public FastScanner(Stream stream) { _stream = stream; }
        private bool HasNextByte() {
            if (_ptr < _buflen) return true;
            _ptr = 0; _buflen = _stream.Read(_buffer, 0, _buffer.Length);
            return _buflen > 0;
        }
        private int ReadByte() => HasNextByte() ? _buffer[_ptr++] : -1;
        public int NextInt() {
            int b = ReadByte();
            while (b != -1 && (b < '0' || b > '9')) b = ReadByte();
            if (b == -1) throw new EndOfStreamException();
            int res = 0;
            while (b >= '0' && b <= '9') { res = res * 10 + (b - '0'); b = ReadByte(); }
            return res;
        }
    }

    class Solver {
        static int N;
        static int[,] Board;
        static (int r, int c)[] Order; // 400マスの訪問順
        static int[] CardAt;           // Order[i]にあるカード番号
        static Random rand = new Random(42);

        // 高速化用のスタックバッファ
        static int[] StackBuf = new int[405];
        static bool[] InStack = new bool[405];

        static void Main() {
            var sc = new FastScanner(Console.OpenStandardInput());
            try { N = sc.NextInt(); } catch { return; }

            Board = new int[N, N];
            var posMap = new List<(int r, int c)>[N * N / 2];
            for (int i = 0; i < N * N / 2; i++) posMap[i] = new List<(int, int)>();

            for (int i = 0; i < N; i++) {
                for (int j = 0; j < N; j++) {
                    int val = sc.NextInt();
                    Board[i, j] = val;
                    posMap[val].Add((i, j));
                }
            }

            // 初期解: ペアを崩さない順序 (A1, A2, B1, B2...)
            Order = new (int r, int c)[N * N];
            CardAt = new int[N * N];
            for (int i = 0; i < N * N / 2; i++) {
                Order[i * 2] = posMap[i][0];
                Order[i * 2 + 1] = posMap[i][1];
            }

            SolveSA();
            Output();
        }

        static void SolveSA() {
            var sw = Stopwatch.StartNew();
            const double TimeLimit = 1980.0; // 1.9秒

            double currentDist = Evaluate(Order, out bool currentValid);
            double bestScore = currentValid ? currentDist : double.MaxValue;
            var bestOrder = ( (int r, int c)[] )Order.Clone();

            // 焼きなましパラメータ
            double startTemp = 30.0;
            double endTemp = 0.1;
            long iterations = 0;

            while (true) {
                if ((iterations & 1023) == 0) {
                    double elapsed = sw.ElapsedMilliseconds;
                    if (elapsed > TimeLimit) break;
                    
                    // 時間による温度減衰 (線形)
                    double progress = elapsed / TimeLimit;
                    double temp = startTemp + (endTemp - startTemp) * progress;

                    // 進捗に合わせて近傍のサイズを変えるなどの工夫も可能
                    PerformSA(temp, ref currentDist, ref bestScore, bestOrder, progress);
                }
                iterations++;
            }
            Order = bestOrder;
            Console.Error.WriteLine($"Iterations: {iterations}");
        }

        static void PerformSA(double temp, ref double currentDist, ref double bestScore, (int r, int c)[] bestOrder, double progress) {
            int n = N * N;
            int i = rand.Next(n), j = rand.Next(n);
            if (i > j) (i, j) = (j, i);
            if (i == j) return;

            // 2-opt (区間反転)
            Array.Reverse(Order, i, j - i + 1);
            
            double newDist = Evaluate(Order, out bool isValid);
            
            // 評価値: 無効なスタック順序には巨大なペナルティ
            double newScore = isValid ? newDist : newDist + 1000000;
            double currentScore = currentDist; // isValidは常に維持されるように遷移させるのがコツ

            if (newScore <= currentScore || rand.NextDouble() < Math.Exp((currentScore - newScore) / temp)) {
                currentDist = newScore;
                if (newScore < bestScore) {
                    bestScore = newScore;
                    Array.Copy(Order, bestOrder, n);
                }
            } else {
                // ロールバック
                Array.Reverse(Order, i, j - i + 1);
            }
        }

        // スタック整合性をチェックしつつ距離を計算
        static double Evaluate((int r, int c)[] order, out bool isValid) {
            double d = 0;
            int curR = 0, curC = 0;
            int top = -1;
            isValid = true;

            // 高速化のためinStackの状態管理を最小限に
            for (int i = 0; i < 405; i++) InStack[i] = false;

            for (int i = 0; i < order.Length; i++) {
                var p = order[i];
                d += Math.Abs(curR - p.r) + Math.Abs(curC - p.c);
                curR = p.r; curC = p.c;

                int card = Board[p.r, p.c];
                if (!InStack[card]) {
                    StackBuf[++top] = card;
                    InStack[card] = true;
                } else {
                    if (top >= 0 && StackBuf[top] == card) {
                        top--;
                    } else {
                        isValid = false; // Xを使わない場合、スタックトップ以外とのペアは不可
                        return d;
                    }
                }
            }
            if (top != -1) isValid = false;
            return d;
        }

        static void Output() {
            var sb = new StringBuilder();
            int curR = 0, curC = 0;
            foreach (var p in Order) {
                while (curR < p.r) { sb.AppendLine("D"); curR++; }
                while (curR > p.r) { sb.AppendLine("U"); curR--; }
                while (curC < p.c) { sb.AppendLine("R"); curC++; }
                while (curC > p.c) { sb.AppendLine("L"); curC--; }
                sb.AppendLine("Z");
            }
            Console.Write(sb.ToString());
        }
    }
}