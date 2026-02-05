using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AtCoder;

class Program
{
    // ★ Mainメソッドの外(class Programの中など)に貼り付けてください
    struct MyOp : ISegtreeOperator<long>
    {
        // 1. Operate (データの合成)
        //    役割: 2つの子ノードの値(x, y)から、親ノードの値を作る。
        //    タイミング: Setで値を更新した時や、Prodで区間を計算する時。
        //    よくある例:
        //      Math.Max(x, y)  (区間最大値)
        //      Math.Min(x, y)  (区間最小値)
        //      x + y           (区間和)
        //      GCD(x, y)       (区間最大公約数)
        public long Operate(long x, long y) => x+y;
    
        // 2. Identity (単位元)
        //    役割: 計算に影響を与えない「空っぽ」の値。
        //    これと x を Operate した結果が、必ず x になる必要がある。
        //    よくある例:
        //      Maxなら: long.MinValue (または十分小さい値)
        //      Minなら: long.MaxValue (または十分大きい値)
        //      Sum/GCD/XORなら: 0
        //      積なら: 1
        public long Identity => 0;
    }
    
    // ★ Mainメソッド内での定義例:
    // var seg = new Segtree<long, MyOp>(N);
    static void Main()
    {
        var sc = new FastScanner();
        var sw = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = false };
        Console.SetOut(sw);
        int N = sc.Int();
        int Q = sc.Int();
        int[] A = sc.IntArr(N);
        var seg = new Segtree<long, MyOp>(N);
        for(int i = 0;i < N;i++)
        {
            seg[i] = A[i];
        }
        for(int i = 0;i < Q;i++)
        {
            int tempt = sc.Int();
            if(tempt == 1)
            {
                tempt = sc.Int()-1;
                long tempt1 = seg[tempt];
                seg[tempt] = seg[tempt+1];
                seg[tempt+1] = tempt1;
            }
            else
            {
                int l = sc.Int();
                int r = sc.Int();
                Console.WriteLine(seg.Prod(l-1,r));
            }
        }
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