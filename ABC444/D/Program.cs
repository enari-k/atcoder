using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AtCoder;

class Program
{
    // ★ Mainメソッドの外(class Programの中など)に貼り付けてください
    struct MyOp : ILazySegtreeOperator<long, long>
    {
        // --------------------------------------
        // ■ S: データ (long) 側の定義
        // --------------------------------------

        // 1. Operate (データの合成)
        //    役割: 2つの子ノードの値(x, y)から、親ノードの値を作る。
        //    タイミング: 更新や取得時、下から上へ値を計算するとき。
        //    例: 区間最大値なら Math.Max(x, y)、区間和なら x + y
        public long Operate(long x, long y) => x + y;

        // 2. Identity (データの単位元)
        //    役割: 範囲外や初期状態のときの値。Operateしても結果が変わらない値。
        //    例: Maxなら long.MinValue, Minなら long.MaxValue, 和なら 0
        public long Identity => 0;

        // --------------------------------------
        // ■ F: 作用素 (long) 側の定義 (遅延させる操作)
        // --------------------------------------

        // 3. Mapping (操作の適用: F -> S)
        //    役割: データ x に対して、操作 f を適用した結果を返す。
        //    タイミング: 遅延配列から実際のデータ配列に値を反映(伝播)するとき。
        //    例(更新): f が -1(無効値) でなければ x を f に書き換える
        //    例(加算): x + f (※区間和の場合は (x + f * 幅) になる点に注意)
        public long Mapping(long f, long x) => x + f;

        // 4. Composition (操作の合成: F x F -> F)
        //    役割: すでに溜まっている操作 g の上に、新しい操作 f を追加する。
        //    注意: 引数の順序は (新, 旧) です。数学的には f ∘ g。
        //    例(更新): f が有効なら g を上書きする (f == -1 ? g : f)
        //    例(加算): f + g (今までの加算分 g に、さらに f を足す)
        public long Composition(long f, long g) => f + g;

        // 5. FIdentity (操作の単位元)
        //    役割: 「何もしない」を表す操作の値。
        //    タイミング: 遅延配列の初期化や、操作がないことの判定に使われる。
        //    例: 加算なら 0, 更新なら -1 や long.MinValue などのあり得ない値
        public long FIdentity => 0;
    }

    static void Main()
    {
        var sc = new FastScanner();
        var sw = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = false };
        Console.SetOut(sw);
        int N = sc.Int();
        int[] A = sc.IntArr(N);
        Array.Sort(A);
        var seg = new LazySegtree<long, long, MyOp>(A[N-1]);
        for(int i = 0;i < N;i++)
        {
            seg.Apply(0,A[i],1);
        }
        long tempt = 0;
        for(int i = 0;i < A[N-1];i++)
        {
            seg[i] += tempt;
            tempt = 0;
            tempt += seg[i]/ 10;
            seg[i] = seg[i]%10;
        }
        if(tempt > 0) Console.Write(tempt);
        for(int i = A[N-1]-1;i >= 0;i--)
        {
            Console.Write(seg[i]);
        }
        Console.WriteLine();
        Console.Out.Flush();
    }

    static int LowerBound(List<long> list, long value)
    {
        int left = 0, right = list.Count;
        while (left < right)
        {
            int mid = left + (right - left) / 2;
            if (list[mid] < value) left = mid + 1;
            else right = mid;
        }
        return left;
    }

    static int UpperBound(List<long> list, long value)
    {
        int left = 0, right = list.Count;
        while (left < right)
        {
            int mid = left + (right - left) / 2;
            if (list[mid] <= value) left = mid + 1;
            else right = mid;
        }
        return left;
    }
    static void D(object o)
    {
    #if DEBUG
        Console.WriteLine(o);
    #endif
    }
}

class FastScanner
{
    private readonly Stream _stream;
    private readonly byte[] _buffer = new byte[1024];
    private int _ptr = 0;
    private int _buflen = 0;

    public FastScanner() { _stream = Console.OpenStandardInput(); }

    private bool HasNextByte()
    {
        if (_ptr < _buflen) return true;
        _ptr = 0;
        _buflen = _stream.Read(_buffer, 0, _buffer.Length);
        return _buflen > 0;
    }

    private byte ReadByte() => HasNextByte() ? _buffer[_ptr++] : (byte)0;

    private static bool IsPrintableChar(int c) => 33 <= c && c <= 126;

    private void SkipUnprintable()
    {
        while (HasNextByte() && !IsPrintableChar(_buffer[_ptr])) _ptr++;
    }

    public string Str()
    {
        SkipUnprintable();
        var sb = new StringBuilder();
        while (HasNextByte() && IsPrintableChar(_buffer[_ptr]))
        {
            sb.Append((char)ReadByte());
        }
        return sb.ToString();
    }

    public int Int()
    {
        long n = Long();
        if (n < int.MinValue || n > int.MaxValue) throw new OverflowException();
        return (int)n;
    }

    public long Long()
    {
        SkipUnprintable();
        long n = 0;
        bool minus = false;
        byte b = ReadByte();
        if (b == '-') { minus = true; b = ReadByte(); }
        if (b < '0' || '9' < b) throw new FormatException();
        while (true)
        {
            if ('0' <= b && b <= '9') { n *= 10; n += b - '0'; }
            else if (b == (byte)0 || !IsPrintableChar(b)) return minus ? -n : n;
            else throw new FormatException();
            b = ReadByte();
        }
    }

    public double Double() => double.Parse(Str());
    public int[] IntArr(int n) { var a = new int[n]; for (int i = 0; i < n; i++) a[i] = Int(); return a; }
    public long[] LongArr(int n) { var a = new long[n]; for (int i = 0; i < n; i++) a[i] = Long(); return a; }
}