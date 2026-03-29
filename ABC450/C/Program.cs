using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AtCoder;

class Program
{
    static void Main()
    {
        var sc = new FastScanner();
        var sw = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = false };
        Console.SetOut(sw);
        int H = sc.Int();
        int W = sc.Int();
        char[,] masume = new char[H+2,W+2];
        bool[,] tansaku = new bool[H+2,W+2];
        string tempt = "";
        for(int i = 1;i < H+1;i++)
        {
            tempt = sc.Str();
            for(int j = 1;j < W+1;j++)
            {
                masume[i,j] = tempt[j-1];
            }
        }
        long answer = 0;
        Queue<(int, int)> tan = new();
        for (int i = 1;i < H+1;i++)
        {
            for(int j = 1;j < W+1;j++)
            {
                if(masume[i,j]=='.'&&!tansaku[i, j])
                {
                    bool flag = true;
                    tan.Enqueue((i,j));
                    tansaku[i, j] = true;
                    while (tan.Count()>0)
                    {
                        var (x , y) = tan.Dequeue();
                        tansaku[x, y] = true;
                        if (masume[x - 1, y] == '.'&& !tansaku[x-1, y])
                        {
                            tan.Enqueue((x-1,y));
                            tansaku[x-1, y] = true;
                        }
                        if(masume[x + 1, y] == '.'&& !tansaku[x+1, y])
                        {
                            tan.Enqueue((x+1,y));
                            tansaku[x+1, y] = true;
                        }
                        if(masume[x, y + 1] == '.'&&!tansaku[x, y+1])
                        {
                            tan.Enqueue((x,y+1));
                            tansaku[x, y+1] = true;
                        }
                        if(masume[x, y - 1] == '.'&& !tansaku[x, y-1])
                        {
                            tan.Enqueue((x,y-1));
                            tansaku[x, y-1] = true;
                        }
                        if(x == 1||x == H||y == 1||y == W)
                        {
                            flag =false;
                        }
                    }
                    if(flag)
                    {
                        answer++;
                    }
                    
                }
            }
        }
        Console.WriteLine(answer);
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