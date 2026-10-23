using System.Text.Json;
using Microsoft.Data.Sqlite;   // ← 추가: SQLite 데이터베이스를 여는 도구.

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 억지로 코드 안에 집어넣음
// 1-1. 서버가 가진 값 — 지금은 코드 안에 직접 적는다
// var monsters = new List<Monster>
// {
//     new Monster { Id = 1, Name = "일반 좀비", Hp = 100,  Attack = 20, Speed = 2.0f },
//     new Monster { Id = 2, Name = "빠른 좀비", Hp = 60,   Attack = 15, Speed = 4.5f },
//     new Monster { Id = 3, Name = "탱커 좀비", Hp = 300,  Attack = 30, Speed = 1.2f },
//     new Monster { Id = 4, Name = "폭발 좀비", Hp = 40,   Attack = 80, Speed = 3.0f },
//     new Monster { Id = 5, Name = "보스 좀비", Hp = 1000, Attack = 50, Speed = 1.8f },
// };


// 코드 밖에 있는 데이터폴더의 데이터를 읽기
//var dataPath = Path.Combine(app.Environment.ContentRootPath, "Data", "monsters.json");
//var json = File.ReadAllText(dataPath);
//var monsters =
//    JsonSerializer.Deserialize<List<Monster>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

// ── 2회차. DB 파일 준비 ────────────────────────────────────────────────
// Data/game.db 파일 하나가 데이터베이스다. 없으면 처음 열 때 만들어진다
var dbPath = Path.Combine(app.Environment.ContentRootPath, "Data", "game.db");
var connectionString = $"Data Source={dbPath}";

// 테이블이 없으면 만든다. 있으면 그대로 둔다 (IF NOT EXISTS)
using (var conn = new SqliteConnection(connectionString))
{
    conn.Open();
    var create = conn.CreateCommand();
    create.CommandText = """
        CREATE TABLE IF NOT EXISTS monsters (
            id     INTEGER PRIMARY KEY AUTOINCREMENT,
            name   TEXT    NOT NULL,
            hp     INTEGER NOT NULL,
            attack INTEGER NOT NULL,
            speed  REAL    NOT NULL
        )
        """;
    create.ExecuteNonQuery();

    // 테이블이 비어 있을 때만 1회차의 monsters.json을 초기 데이터로 넣는다. 그래서 재시작해도 중복되지 않는다
    var count = conn.CreateCommand();
    count.CommandText = "SELECT COUNT(*) FROM monsters";
    if ((long)count.ExecuteScalar()! == 0)
    {
        var seedPath = Path.Combine(app.Environment.ContentRootPath, "Data", "monsters.json");
        var seed = JsonSerializer.Deserialize<List<Monster>>(
            File.ReadAllText(seedPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        foreach (var m in seed)
        {
            var insert = conn.CreateCommand();
            insert.CommandText = "INSERT INTO monsters (name, hp, attack, speed) VALUES ($name, $hp, $attack, $speed)";
            insert.Parameters.AddWithValue("$name", m.Name);
            insert.Parameters.AddWithValue("$hp", m.Hp);
            insert.Parameters.AddWithValue("$attack", m.Attack);
            insert.Parameters.AddWithValue("$speed", m.Speed);
            insert.ExecuteNonQuery();
        }
    }
}

// SELECT 결과(행들)를 Monster 목록으로 바꾸는 헬퍼. GET 네 곳에서 같이 쓴다
static List<Monster> ReadMonsters(SqliteDataReader reader)
{
    var list = new List<Monster>();
    while (reader.Read())   // 한 줄(한 마리)씩
    {
        list.Add(new Monster
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Hp = reader.GetInt32(2),
            Attack = reader.GetInt32(3),
            Speed = reader.GetFloat(4)
        });
    }
    return list;
}

// 모든 몬스터를 DB에서 읽는다. /monsters, /monsters/slow, /monsters/big이 같이 쓴다
static List<Monster> LoadAllMonsters(string connectionString)
{
    using var conn = new SqliteConnection(connectionString);
    conn.Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT id, name, hp, attack, speed FROM monsters ORDER BY id";
    using var reader = cmd.ExecuteReader();
    return ReadMonsters(reader);
}

app.MapGet("/hello", () => "안녕하세요, 서버입니다.");

// ── 몬스터 조회 — 이제 파일이 아니라 DB에서 읽는다 ─────────────────────
app.MapGet("/monsters", () => new { monsters = LoadAllMonsters(connectionString) });

// ── 테스트용 1: 일부러 느린 서버 (기본 3초). 04 파트에서 기다리는 동안 Unity가 멈추는지 본다 ──
app.MapGet("/monsters/slow", async (int ms = 3000) =>
{
    await Task.Delay(ms);   // ms 밀리초 기다린다. ?ms=8000처럼 바꿀 수 있다
    return new { monsters = LoadAllMonsters(connectionString) };
});

// ── 테스트용 2: 일부러 큰 데이터 (기본 20,000마리). 04 파트에서 큰 JSON을 받을 때 걸리는 시간을 본다 ──
app.MapGet("/monsters/big", (int count = 20000) =>
{
    var monsters = LoadAllMonsters(connectionString);
    var big = new List<Monster>(count);
    for (int i = 1; i <= count; i++)
    {
        var src = monsters[(i - 1) % monsters.Count];   // 다섯 마리를 돌려 가며 복제한다
        big.Add(new Monster { Id = i, Name = $"{src.Name} #{i}", Hp = src.Hp, Attack = src.Attack, Speed = src.Speed });
    }
    return new { monsters = big };
});

app.Run("http://localhost:5000");

// 데이터 모양
class Monster
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Hp { get; set; }
    public int Attack { get; set; }
    public float Speed { get; set; }
}