/**
 * A - Ice Cream Collection (Memory Optimized Beam Search)
 * 変更点:
 * 1. InventoryManagerを `shared_ptr` を用いた Copy-on-Write 構成に変更。
 * 変更のないショップの在庫はメモリを共有し、消費量を劇的に削減。
 * 2. BEAM_WIDTH を 120 に設定 (1024MiB制限に合わせて調整)。
 */

#include <iostream>
#include <vector>
#include <algorithm>
#include <random>
#include <chrono>
#include <set>
#include <map>
#include <queue>
#include <bitset>
#include <memory> // shared_ptr用
#include <cassert>

using namespace std;

// --- 定数・設定 ---
const int MAX_T = 10000;
const double TIME_LIMIT = 1.90;
int BEAM_WIDTH = 120; // メモリ対策により 100~150 程度は安全

// --- 乱数 ---
struct Random
{
    mt19937 mt;
    Random() : mt(chrono::steady_clock::now().time_since_epoch().count()) {}
    int next_int(int l, int r)
    {
        uniform_int_distribution<int> dist(l, r);
        return dist(mt);
    }
    double next_double()
    {
        uniform_real_distribution<double> dist(0.0, 1.0);
        return dist(mt);
    }
} rnd;

struct Input
{
    int N, M, K, T;
    vector<vector<int>> adj;
    vector<pair<int, int>> coords;
    vector<bool> is_shop;
};
Input input;

// --- Rolling Hash ---
using HashType = unsigned long long;
const HashType BASE = 10007;
const HashType MOD = 1000000000000000003ULL;

struct ConeHash
{
    HashType val;
    int len;
    ConeHash() : val(0), len(0) {}
    ConeHash add(int flavor) const
    {
        ConeHash next;
        next.len = len + 1;
        unsigned __int128 temp = (unsigned __int128)val * BASE + (flavor + 1);
        next.val = (HashType)(temp % MOD);
        return next;
    }
};

// --- 在庫管理 (Memory Optimized) ---
// vectorそのものではなく、vectorへのshared_ptrを持つことでコピーを回避
using ShopInventory = vector<HashType>;
using ShopInventoryPtr = shared_ptr<ShopInventory>;

struct InventoryState
{
    // 各ショップの在庫へのポインタを持つ (K=10)
    vector<ShopInventoryPtr> shops;

    InventoryState(int K)
    {
        shops.resize(K);
        for (int i = 0; i < K; ++i)
        {
            shops[i] = make_shared<ShopInventory>();
        }
    }
};

struct InventoryManager
{
    // 全状態をプールする
    vector<InventoryState> pool;

    InventoryManager() {}

    void init(int K)
    {
        pool.clear();
        pool.reserve(MAX_T * BEAM_WIDTH / 2); // ある程度確保
        pool.push_back(InventoryState(K));    // ID 0: 初期状態
    }

    // old_id をベースに、shop_idx に item_hash を追加
    // 追加された場合は新しいIDを返す。すでにあった場合は old_id を返す。
    pair<int, bool> add_item(int old_id, int shop_idx, HashType item_hash)
    {
        const auto &current_inv_ptr = pool[old_id].shops[shop_idx];
        const auto &vec = *current_inv_ptr;

        // 存在確認 (sorted前提)
        auto it = lower_bound(vec.begin(), vec.end(), item_hash);
        if (it != vec.end() && *it == item_hash)
        {
            return {old_id, false}; // 既に存在する
        }

        // 新しい状態を作成
        int new_id = pool.size();
        pool.push_back(pool[old_id]); // ポインタ配列(サイズ10)をコピー。中身は共有される。

        // 変更対象のショップだけ Deep Copy して更新
        auto new_vec_ptr = make_shared<ShopInventory>(vec); // コピー発生
        auto &new_vec = *new_vec_ptr;

        // 挿入
        new_vec.insert(lower_bound(new_vec.begin(), new_vec.end(), item_hash), item_hash);

        // 新しいポインタをセット
        pool[new_id].shops[shop_idx] = new_vec_ptr;

        return {new_id, true};
    }
};

InventoryManager inv_manager;

// --- ビームサーチの状態 ---
struct State
{
    int current_node;
    int prev_node;
    ConeHash cone;
    int inventory_id;
    bitset<100> tree_colors;
    int score;

    int parent_idx;
    int action_type;
    int action_val;

    double eval_score;
};

// --- BFS ---
vector<int> min_dist_to_shop;
void calc_dist_to_shop()
{
    min_dist_to_shop.assign(input.N, 1e9);
    queue<int> q;
    for (int i = 0; i < input.K; ++i)
    {
        min_dist_to_shop[i] = 0;
        q.push(i);
    }
    while (!q.empty())
    {
        int u = q.front();
        q.pop();
        for (int v : input.adj[u])
        {
            if (min_dist_to_shop[v] > min_dist_to_shop[u] + 1)
            {
                min_dist_to_shop[v] = min_dist_to_shop[u] + 1;
                q.push(v);
            }
        }
    }
}

// --- 評価関数 ---
double evaluate(const State &s)
{
    double val = 0;
    val += s.score * 100000.0;
    val += s.cone.len * 50.0;
    if (s.cone.len > 0)
        val -= min_dist_to_shop[s.current_node] * 10.0;
    val += s.tree_colors.count() * 5.0;
    val += rnd.next_double() * 20.0;
    return val;
}

// --- ソルバー ---
void solve()
{
    auto start_clock = chrono::steady_clock::now();
    calc_dist_to_shop();
    inv_manager.init(input.K);

    State initial_state;
    initial_state.current_node = 0;
    initial_state.prev_node = -1;
    initial_state.cone = ConeHash();
    initial_state.inventory_id = 0;
    initial_state.tree_colors.reset();
    initial_state.score = 0;
    initial_state.parent_idx = -1;
    initial_state.action_type = -1;
    initial_state.eval_score = 0;

    vector<State> current_beam;
    current_beam.reserve(BEAM_WIDTH);
    current_beam.push_back(initial_state);

    struct HistoryNode
    {
        int parent_idx;
        int type;
        int val;
    };
    // Tが長いので1次元配列で管理してメモリ節約
    // history_pool[step][idx] ではなく、フラットに持ち、offsetで管理でもよいが
    // ここはわかりやすさ優先で vector<vector>。ただし reserve する。
    vector<vector<HistoryNode>> history;
    history.reserve(input.T + 1);
    history.push_back({{-1, -1, -1}});

    for (int t = 0; t < input.T; ++t)
    {
        if ((t & 63) == 0)
        {
            auto now = chrono::steady_clock::now();
            double elapsed = chrono::duration_cast<chrono::duration<double>>(now - start_clock).count();
            if (elapsed > TIME_LIMIT)
                BEAM_WIDTH = 1;
        }

        vector<State> next_beam_candidates;
        next_beam_candidates.reserve(BEAM_WIDTH * 4);
        vector<HistoryNode> current_step_history;
        current_step_history.reserve(BEAM_WIDTH);

        for (int i = 0; i < current_beam.size(); ++i)
        {
            const State &s = current_beam[i];

            // Action 2: Change Color
            if (!input.is_shop[s.current_node] && !s.tree_colors[s.current_node])
            {
                State next_s = s;
                next_s.tree_colors[s.current_node] = 1;
                next_s.parent_idx = i;
                next_s.action_type = 2;
                next_s.action_val = -1;
                next_s.eval_score = evaluate(next_s);
                next_beam_candidates.push_back(next_s);
            }

            // Action 1: Move
            for (int neighbor : input.adj[s.current_node])
            {
                if (neighbor == s.prev_node)
                    continue;

                State next_s = s;
                next_s.current_node = neighbor;
                next_s.prev_node = s.current_node;
                next_s.parent_idx = i;
                next_s.action_type = 1;
                next_s.action_val = neighbor;

                if (input.is_shop[neighbor])
                {
                    pair<int, bool> res = inv_manager.add_item(s.inventory_id, neighbor, s.cone.val);
                    next_s.inventory_id = res.first;
                    if (res.second)
                        next_s.score += 1;
                    next_s.cone = ConeHash();
                }
                else
                {
                    int color = s.tree_colors[neighbor] ? 1 : 0;
                    next_s.cone = s.cone.add(color);
                }

                next_s.eval_score = evaluate(next_s);
                next_beam_candidates.push_back(next_s);
            }
        }

        int keep_num = min((int)next_beam_candidates.size(), BEAM_WIDTH);
        if (next_beam_candidates.size() > keep_num)
        {
            nth_element(next_beam_candidates.begin(),
                        next_beam_candidates.begin() + keep_num,
                        next_beam_candidates.end(),
                        [](const State &a, const State &b)
                        {
                            return a.eval_score > b.eval_score;
                        });
        }

        current_beam.clear();
        for (int k = 0; k < keep_num; ++k)
        {
            current_beam.push_back(next_beam_candidates[k]);
            current_step_history.push_back({next_beam_candidates[k].parent_idx,
                                            next_beam_candidates[k].action_type,
                                            next_beam_candidates[k].action_val});
        }
        history.push_back(current_step_history);
    }

    int best_idx = 0;
    double best_val = -1e18;
    for (int i = 0; i < current_beam.size(); ++i)
    {
        if (current_beam[i].eval_score > best_val)
        {
            best_val = current_beam[i].eval_score;
            best_idx = i;
        }
    }

    vector<int> final_actions;
    int curr_idx = best_idx;

    for (int t = input.T; t >= 1; --t)
    {
        const auto &node = history[t][curr_idx];
        if (node.type == 1)
            final_actions.push_back(node.val);
        else
            final_actions.push_back(-1);
        curr_idx = node.parent_idx;
    }
    reverse(final_actions.begin(), final_actions.end());
    for (int v : final_actions)
        cout << v << "\n";
}

int main()
{
    ios::sync_with_stdio(false);
    cin.tie(nullptr);
    if (!(cin >> input.N >> input.M >> input.K >> input.T))
        return 0;
    input.adj.resize(input.N);
    input.coords.resize(input.N);
    input.is_shop.assign(input.N, false);
    for (int i = 0; i < input.M; ++i)
    {
        int u, v;
        cin >> u >> v;
        input.adj[u].push_back(v);
        input.adj[v].push_back(u);
    }
    for (int i = 0; i < input.N; ++i)
    {
        cin >> input.coords[i].first >> input.coords[i].second;
        if (i < input.K)
            input.is_shop[i] = true;
    }
    solve();
    return 0;
}