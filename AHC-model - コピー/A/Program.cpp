#pragma GCC optimize("O3,unroll-loops")
#include <iostream>
#include <vector>
#include <cmath>
#include <chrono>
#include <algorithm>
#include <cstdint>

using namespace std;
using namespace std::chrono;

const int N = 200;
const double TIME_LIMIT = 2.85;

#ifndef SA_START_TEMP
#define SA_START_TEMP 272085758.36
#endif

#ifndef SA_END_TEMP
#define SA_END_TEMP 1.0
#endif

#ifndef SA_LEN_POW
#define SA_LEN_POW 2.273
#endif

#ifndef SA_OROPT_RATE
#define SA_OROPT_RATE 54
#endif

int A[N][N];
int pos_in_path[N][N];

// ★最適化1: 構造体に人口(val)を持たせて超高速化
struct Point
{
    int x, y;
    int val;
};

vector<Point> path;

inline uint32_t xor32()
{
    static uint32_t x = 123456789;
    x ^= x << 13;
    x ^= x >> 17;
    x ^= x << 5;
    return x;
}

inline int next_int(int max_val)
{
    return xor32() % max_val;
}

inline double next_double()
{
    return (double)xor32() / 4294967295.0;
}

double get_time()
{
    static auto start_time = high_resolution_clock::now();
    auto current_time = high_resolution_clock::now();
    return duration_cast<duration<double>>(current_time - start_time).count();
}

// ★工夫: 「2行編み込み(Width-2 Weave)」初期解
void create_initial_solution()
{
    path.clear();
    for (int i = 0; i < N; i += 2)
    {
        if ((i / 2) % 2 == 0)
        {
            // 左から右へジッパーのように進む
            for (int j = 0; j < N; ++j)
            {
                path.push_back({i, j, A[i][j]});
                path.push_back({i + 1, j, A[i + 1][j]});
            }
        }
        else
        {
            // 右から左へ
            for (int j = N - 1; j >= 0; --j)
            {
                path.push_back({i, j, A[i][j]});
                path.push_back({i + 1, j, A[i + 1][j]});
            }
        }
    }

    // 位置情報の記録
    for (int k = 0; k < N * N; ++k)
    {
        pos_in_path[path[k].x][path[k].y] = k;
    }
}

// ★最適化2: 配列Aへのアクセスがなくなり、爆速化
inline long long calc_diff(int i, int j)
{
    long long diff = 0;
    int len = j - i;
    int half = len / 2;
    for (int k = 0; k <= half; ++k)
    {
        diff += (long long)(len - 2 * k) * (path[i + k].val - path[j - k].val);
    }
    return diff;
}

inline void apply_2opt(int i, int j)
{
    int left = i, right = j;
    while (left < right)
    {
        swap(path[left], path[right]);
        pos_in_path[path[left].x][path[left].y] = left;
        pos_in_path[path[right].x][path[right].y] = right;
        left++;
        right--;
    }
}

long long calc_total_favorability()
{
    long long total = 0;
    for (int k = 0; k < N * N; ++k)
    {
        total += 1LL * k * path[k].val;
    }
    return total;
}

void simulated_annealing()
{
    double start_temp = SA_START_TEMP;
    double end_temp = SA_END_TEMP;
    double temp = start_temp;

    int dx8[] = {-1, -1, -1, 0, 0, 1, 1, 1};
    int dy8[] = {-1, 0, 1, -1, 1, -1, 0, 1};

    long long iter = 0;
    long long accepted = 0;

    // ベースとなる確率（ここは固定でOKです）
    const int BASE_PREFIX = 5;
    const int BASE_SUFFIX = 5;
    const int BASE_TWO_OPT = 100 - BASE_PREFIX - BASE_SUFFIX - SA_OROPT_RATE;

    int current_max_len = N * N;
    double progress = 0.0;

    while (true)
    {
        if ((iter & 1023) == 0)
        {
            double current_time = get_time();
            if (current_time > TIME_LIMIT)
                break;
            progress = current_time / TIME_LIMIT;
            temp = start_temp * pow(end_temp / start_temp, progress);
            current_max_len = 50 + (int)((N * N - 50) * pow(1.0 - progress, SA_LEN_POW));
        }
        iter++;

        // ★改善案: 確率の動的シフトと O(1) スワップの導入
        // 序盤は 2-opt を回し、終盤は 0% にしてミクロ最適化に全振りする
        double prog_factor = max(0.0, 1.0 - progress * 1.5); // 進行度66%で2-optは0になる
        int dyn_two_opt = (int)((100 - SA_OROPT_RATE - 10) * prog_factor);

        // 終盤は端点の変更も不要になるため 0% に落とす
        int dyn_prefix = (progress < 0.8) ? 5 : 0;
        int dyn_suffix = (progress < 0.8) ? 5 : 0;

        // 既存の重い Or-opt は設定値の 60% 程度に抑える（1点と2点に分割）
        int dyn_oropt_1 = SA_OROPT_RATE * 0.3;
        int dyn_oropt_2 = SA_OROPT_RATE * 0.3;

        // ★余った確率（終盤はほぼ全て）を超高速 O(1) スワップに全振り！
        int dyn_o1_swap = 100 - dyn_two_opt - dyn_prefix - dyn_suffix - dyn_oropt_1 - dyn_oropt_2;

        int roll = next_int(100);

        if (roll < dyn_two_opt)
        {
            // --- 通常の2-opt ---
            // (既存の 2-opt の中身をそのまま配置)
            int idx1 = next_int(N * N - 3) + 1;
            Point u = path[idx1 - 1];
            int cands[8];
            int cands_count = 0;
            for (int d = 0; d < 8; ++d)
            {
                int nx = u.x + dx8[d];
                int ny = u.y + dy8[d];
                if (nx >= 0 && nx < N && ny >= 0 && ny < N)
                {
                    int idx2 = pos_in_path[nx][ny];
                    if (idx2 > idx1 && idx2 <= idx1 + current_max_len && idx2 < N * N - 1)
                    {
                        Point v1 = path[idx1];
                        Point v2 = path[idx2 + 1];
                        if (abs(v1.x - v2.x) <= 1 && abs(v1.y - v2.y) <= 1)
                        {
                            cands[cands_count++] = idx2;
                        }
                    }
                }
            }
            if (cands_count == 0)
                continue;
            int idx2 = cands[next_int(cands_count)];
            long long diff = calc_diff(idx1, idx2);
            if (diff < -temp * 10.0)
                continue;
            if (diff >= 0 || exp(diff / temp) > next_double())
            {
                apply_2opt(idx1, idx2);
                accepted++;
            }
        }
        else if (roll < dyn_two_opt + dyn_prefix)
        {
            // --- 始点変更 ---
            // (既存の中身そのまま)
            Point u = path[0];
            int cands[8];
            int cands_count = 0;
            for (int d = 0; d < 8; ++d)
            {
                int nx = u.x + dx8[d];
                int ny = u.y + dy8[d];
                if (nx >= 0 && nx < N && ny >= 0 && ny < N)
                {
                    int idx = pos_in_path[nx][ny];
                    if (idx > 1 && idx <= current_max_len && idx < N * N - 1)
                        cands[cands_count++] = idx - 1;
                }
            }
            if (cands_count == 0)
                continue;
            int j = cands[next_int(cands_count)];
            long long diff = calc_diff(0, j);
            if (diff < -temp * 10.0)
                continue;
            if (diff >= 0 || exp(diff / temp) > next_double())
            {
                apply_2opt(0, j);
                accepted++;
            }
        }
        else if (roll < dyn_two_opt + dyn_prefix + dyn_suffix)
        {
            // --- 終点変更 ---
            // (既存の中身そのまま)
            Point u = path[N * N - 1];
            int cands[8];
            int cands_count = 0;
            for (int d = 0; d < 8; ++d)
            {
                int nx = u.x + dx8[d];
                int ny = u.y + dy8[d];
                if (nx >= 0 && nx < N && ny >= 0 && ny < N)
                {
                    int idx = pos_in_path[nx][ny];
                    if (idx > 0 && idx >= N * N - 1 - current_max_len && idx < N * N - 2)
                        cands[cands_count++] = idx + 1;
                }
            }
            if (cands_count == 0)
                continue;
            int i = cands[next_int(cands_count)];
            long long diff = calc_diff(i, N * N - 1);
            if (diff < -temp * 10.0)
                continue;
            if (diff >= 0 || exp(diff / temp) > next_double())
            {
                apply_2opt(i, N * N - 1);
                accepted++;
            }
        }
        else if (roll < dyn_two_opt + dyn_prefix + dyn_suffix + dyn_oropt_1)
        {
            // --- 1点 Or-opt (再配置) ---
            // (既存の 1点 Or-opt の中身そのまま)
            int i = next_int(N * N - 2) + 1;
            Point p_prev = path[i - 1];
            Point p_next = path[i + 1];
            if (abs(p_prev.x - p_next.x) <= 1 && abs(p_prev.y - p_next.y) <= 1)
            {
                Point u = path[i];
                int cands[8];
                int cands_count = 0;
                for (int d = 0; d < 8; ++d)
                {
                    int nx = u.x + dx8[d];
                    int ny = u.y + dy8[d];
                    if (nx >= 0 && nx < N && ny >= 0 && ny < N)
                    {
                        int j = pos_in_path[nx][ny];
                        if (j >= 0 && j < N * N - 1 && abs(j - i) > 1 && abs(j - i) <= current_max_len)
                        {
                            Point p_j_next = path[j + 1];
                            if (abs(u.x - p_j_next.x) <= 1 && abs(u.y - p_j_next.y) <= 1)
                                cands[cands_count++] = j;
                        }
                    }
                }
                if (cands_count > 0)
                {
                    int j = cands[next_int(cands_count)];
                    long long diff = 0;
                    long long sum_A = 0;
                    if (j < i)
                    {
                        for (int m = j + 1; m < i; ++m)
                            sum_A += path[m].val;
                        diff = sum_A - (long long)(i - j - 1) * u.val;
                    }
                    else
                    {
                        for (int m = i + 1; m <= j; ++m)
                            sum_A += path[m].val;
                        diff = (long long)(j - i) * u.val - sum_A;
                    }
                    if (diff < -temp * 10.0)
                        continue;
                    if (diff >= 0 || exp(diff / temp) > next_double())
                    {
                        if (j < i)
                        {
                            for (int m = i; m > j + 1; --m)
                            {
                                path[m] = path[m - 1];
                                pos_in_path[path[m].x][path[m].y] = m;
                            }
                            path[j + 1] = u;
                            pos_in_path[u.x][u.y] = j + 1;
                        }
                        else
                        {
                            for (int m = i; m < j; ++m)
                            {
                                path[m] = path[m + 1];
                                pos_in_path[path[m].x][path[m].y] = m;
                            }
                            path[j] = u;
                            pos_in_path[u.x][u.y] = j;
                        }
                        accepted++;
                    }
                }
            }
        }
        else if (roll < dyn_two_opt + dyn_prefix + dyn_suffix + dyn_oropt_1 + dyn_oropt_2)
        {
            // --- 2点 Or-opt ---
            // (既存の 2点 Or-opt の中身そのまま)
            int i = next_int(N * N - 3) + 1;
            Point p_prev = path[i - 1];
            Point p_next2 = path[i + 2];
            if (abs(p_prev.x - p_next2.x) <= 1 && abs(p_prev.y - p_next2.y) <= 1)
            {
                Point u1 = path[i];
                Point u2 = path[i + 1];
                int cands[8];
                int cands_count = 0;
                for (int d = 0; d < 8; ++d)
                {
                    int nx = u1.x + dx8[d];
                    int ny = u1.y + dy8[d];
                    if (nx >= 0 && nx < N && ny >= 0 && ny < N)
                    {
                        int j = pos_in_path[nx][ny];
                        if (j >= 0 && j < N * N - 1 && (j < i - 1 || j > i + 1) && abs(j - i) <= current_max_len)
                        {
                            Point p_j_next = path[j + 1];
                            if (abs(u2.x - p_j_next.x) <= 1 && abs(u2.y - p_j_next.y) <= 1)
                                cands[cands_count++] = j;
                        }
                    }
                }
                if (cands_count > 0)
                {
                    int j = cands[next_int(cands_count)];
                    long long diff = 0;
                    long long sum_A = 0;
                    if (j < i - 1)
                    {
                        for (int m = j + 1; m < i; ++m)
                            sum_A += path[m].val;
                        diff = sum_A * 2LL - (long long)(i - j - 1) * (u1.val + u2.val);
                    }
                    else
                    {
                        for (int m = i + 2; m <= j; ++m)
                            sum_A += path[m].val;
                        diff = (long long)(j - i - 1) * (u1.val + u2.val) - sum_A * 2LL;
                    }
                    if (diff < -temp * 10.0)
                        continue;
                    if (diff >= 0 || exp(diff / temp) > next_double())
                    {
                        if (j < i - 1)
                        {
                            for (int m = i + 1; m > j + 2; --m)
                            {
                                path[m] = path[m - 2];
                                pos_in_path[path[m].x][path[m].y] = m;
                            }
                            path[j + 1] = u1;
                            pos_in_path[u1.x][u1.y] = j + 1;
                            path[j + 2] = u2;
                            pos_in_path[u2.x][u2.y] = j + 2;
                        }
                        else
                        {
                            for (int m = i; m < j - 1; ++m)
                            {
                                path[m] = path[m + 2];
                                pos_in_path[path[m].x][path[m].y] = m;
                            }
                            path[j - 1] = u1;
                            pos_in_path[u1.x][u1.y] = j - 1;
                            path[j] = u2;
                            pos_in_path[u2.x][u2.y] = j;
                        }
                        accepted++;
                    }
                }
            }
        }
        else
        {
            // ★完全新規: O(1) 超高速スワップ（隣接 & 1つ飛ばし）
            // 評価も配列更新も O(1) なので、一瞬で終わる（終盤のノイズを完全に消し去る）
            if (next_int(2) == 0)
            {
                // ① 隣接スワップ (i と i+1 を入れ替え)
                int i = next_int(N * N - 3) + 1;
                Point p_prev = path[i - 1];
                Point p_i = path[i];
                Point p_next = path[i + 1];
                Point p_next2 = path[i + 2];

                // スワップ後に道が繋がっているか判定
                if (abs(p_prev.x - p_next.x) <= 1 && abs(p_prev.y - p_next.y) <= 1 &&
                    abs(p_i.x - p_next2.x) <= 1 && abs(p_i.y - p_next2.y) <= 1)
                {

                    long long diff = p_i.val - p_next.val; // O(1) 差分計算

                    if (diff >= 0 || exp(diff / temp) > next_double())
                    {
                        swap(path[i], path[i + 1]);
                        pos_in_path[path[i].x][path[i].y] = i;
                        pos_in_path[path[i + 1].x][path[i + 1].y] = i + 1;
                        accepted++;
                    }
                }
            }
            else
            {
                // ② 1つ飛ばしスワップ (i と i+2 を入れ替え)
                int i = next_int(N * N - 4) + 1;
                Point p_prev = path[i - 1];
                Point p_i = path[i];
                Point p_next2 = path[i + 2];
                Point p_next3 = path[i + 3];

                if (abs(p_prev.x - p_next2.x) <= 1 && abs(p_prev.y - p_next2.y) <= 1 &&
                    abs(p_i.x - p_next3.x) <= 1 && abs(p_i.y - p_next3.y) <= 1)
                {

                    long long diff = 2LL * (p_i.val - p_next2.val); // O(1) 差分計算

                    if (diff >= 0 || exp(diff / temp) > next_double())
                    {
                        swap(path[i], path[i + 2]);
                        pos_in_path[path[i].x][path[i].y] = i;
                        pos_in_path[path[i + 2].x][path[i + 2].y] = i + 2;
                        accepted++;
                    }
                }
            }
        }
    }

    cerr << "Iterations: " << iter << ", Accepted: " << accepted << endl;
}

int main()
{
    ios_base::sync_with_stdio(false);
    cin.tie(NULL);

    int dummy_N;
    if (!(cin >> dummy_N))
        return 0;

    for (int i = 0; i < N; ++i)
    {
        for (int j = 0; j < N; ++j)
        {
            cin >> A[i][j];
        }
    }

    create_initial_solution();
    simulated_annealing();

    cerr << "V=" << calc_total_favorability() << "\n";

    for (int i = 0; i < N * N; ++i)
    {
        cout << path[i].x << " " << path[i].y << "\n";
    }

    return 0;
}