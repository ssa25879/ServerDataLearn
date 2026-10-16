using System.Text.Json;

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
var dataPath = Path.Combine(app.Environment.ContentRootPath, "Data", "monsters.json");
var json = File.ReadAllText(dataPath);
var monsters =
    JsonSerializer.Deserialize<List<Monster>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });


// 살아 있는지 확인
app.MapGet("/hello", () => "안녕하세요, 서버입니다.");

// 몬스터 전체 — 반환값(객체)이 자동으로 JSON이 된다
app.MapGet("/monsters", () => new { monsters });

// 몬스터의 수를 반환
app.MapGet("/monsters/count", () => new { count = monsters?.Count });

// 몬스터 하나 — 없으면 404
app.MapGet("/monsters/{id:int}", (int id) =>
{
    var found = monsters?.Find(m => m.Id == id);
    return found is null
        ? Results.NotFound(new { error = $"id {id} 몬스터가 없습니다." })
        : Results.Ok(found);
});

// 끊김 체험용: 일부러 느린 서버 (기본 3초)
app.MapGet("/monsters/slow", async (int ms = 3000) =>
{
    await Task.Delay(ms);
    return new { monsters };
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