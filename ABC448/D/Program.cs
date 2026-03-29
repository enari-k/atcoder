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
        int N = sc.Int();
        long[] A = sc.LongArr(N);
        bool[] answer = new bool[N+1];
        List<int>[] G = new List<int>[N+1];
        for(int i = 0;i < N;i++)
        {
            G[i+1] = new();
        }
        int[] kazu = new int[N+1];
        HashSet<long>(kazu) = HashSet<long>();
        for (int i = 0; i < N; i++)
        {
            kazu[i + 1] = new();
            kazu[i+1].Add(A[i]);
        }
        for (int i = 0;i < N-1;i++)
        {
            int U = sc.Int();
            int V = sc.Int();
            G[U].Add(V);
            G[V].Add(U);
        }
        // 距離配列の初期化 (-1)
        var dist = new int[N + 1];
        Array.Fill(dist, -1);
        
        var queue = new Queue<int>();
        dist[1] = 0;
        queue.Enqueue(1);
        
        while (queue.Count > 0)
        {
            var u = queue.Dequeue();
        
            foreach (var v in G[u])
            {
                if (dist[v] == -1)
                {
                    dist[v] = dist[u] + 1;
                    queue.Enqueue(v);
                }
            }
        }
        for(int i = 0;i < N;i++)
        {
            int dists = dist[i+1];
            int u = i+1;
            while(dists>0)
            {
                foreach(var v in G[u])
                {
                    if(dist[v]-1==dists)
                    {
                        dists--;
                        u = v;
                        if(kazu[i+1].Contains(A[v-1]))
                        {
                            dists = -5;
                            break;
                        }
                        else
                        {
                            kazu[i+1].Add(A[v-1]);
                            break;
                        }
                    }
                }
            }
            if(dists == -5)
            {
                Console.WriteLine("Yes");
            }
            else
            {
                Console.WriteLine("No");
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