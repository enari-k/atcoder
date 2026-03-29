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
        int Q = sc.Int();
        for(int j = 0;j < Q;j++)
        {
            int N = sc.Int();
            List<int>[] G = new List<int>[N+1];
            for(int i = 0;i < N+1;i++)
            {
                G[i] = new();
            }
            for(int i = 1;i < N;i++)
            {
                int A = sc.Int(),B = sc.Int();
                G[A].Add(B);
                G[B].Add(A);
            }
            int startNode = 1;
            // 距離配列の初期化 (無限大)
            long[] dist = new long[N + 1];
            Array.Fill(dist, long.MaxValue);
            
            // PriorityQueue<頂点, 距離> (距離が小さい順に取り出す)
            var pq = new PriorityQueue<int, long>();
            
            // スタート地点の設定
            dist[startNode] = 0;
            pq.Enqueue(startNode, 0);
            
            while (pq.Count > 0)
            {
                // 最短距離候補を取り出す
                pq.TryDequeue(out int u, out long d);
            
                // 既に記録されている距離より長ければスキップ (高速化の肝)
                if (d > dist[u]) continue;
            
                // 隣接頂点を探索 (G[u] は (行き先v, コストcost) のリスト想定)
                foreach (var v in G[u])
                {
                    if (dist[v] > dist[u] + 1)
                    {
                        dist[v] = dist[u] + 1;
                        pq.Enqueue(v, dist[v]);
                    }
                }
            }
            long max = 0;
            startNode = 0;
            for(int i = 1;i < N+1;i++)
            {
                if(dist[i]>max)
                {
                    max = dist[i];
                    startNode = i;
                }
            }
            // 距離配列の初期化 (無限大)
            long[] dist1 = new long[N + 1];
            Array.Fill(dist1, long.MaxValue);
            
            // PriorityQueue<頂点, 距離> (距離が小さい順に取り出す)
            var pq1 = new PriorityQueue<int, long>();
            
            // スタート地点の設定
            dist1[startNode] = 0;
            pq1.Enqueue(startNode, 0);
            
            while (pq1.Count > 0)
            {
                // 最短距離候補を取り出す
                pq1.TryDequeue(out int u, out long d);
            
                // 既に記録されている距離より長ければスキップ (高速化の肝)
                if (d > dist1[u]) continue;
            
                // 隣接頂点を探索 (G[u] は (行き先v, コストcost) のリスト想定)
                foreach (var v in G[u])
                {
                    if (dist1[v] > dist1[u] + 1)
                    {
                        dist1[v] = dist1[u] + 1;
                        pq1.Enqueue(v, dist1[v]);
                    }
                }
            }
            long answer = 0;
            max = 0;
            int final = 0;
            for (int i = 1; i < N + 1; i++)
            {
                if (dist1[i] >= max)
                {
                    max = dist1[i];
                    final = i;
                }
            }
            int ima = final;
            int next = 0;
            var visited = new bool[N+1];
            Array.Fill(visited,false);
            while(true)
            {
                int point = 0;
                visited[ima] = true;
                foreach(int i in G[ima])
                {
                    if(dist1[i]==dist1[ima]+1&&!visited[i])
                    {
                        point++;
                        visited[i] = true;
                    }
                    if(dist1[i]<dist1[ima])
                    {
                        next = i;
                    }
                }
                if(point >= 2)
                {
                    answer++;
                }
                if(ima == startNode) break;
                ima = next;
            }
            Console.WriteLine(answer);
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