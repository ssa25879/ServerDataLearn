# 2일차 비동기 원리와 서버 고도화

이번 수업은 **03. 서버 고도화 — SQLite와 요청 네 가지**까지 진행했다. 동기·비동기의 원리와 C# 콘솔 실습을 정리하고, SQLite를 사용하는 서버 코드까지 기록한다. Unity 클라이언트를 비동기로 바꾸는 04 파트는 수업 시간이 부족해 코루틴과 함께 3일차로 미뤄졌다. 강의 제목의 "요청 네 가지"는 강의 원문 표현이며, 아래 표에는 조회 요청까지 포함해 여섯 가지를 정리했다.

## 오늘 배운 내용

### 코드가 실행되는 과정과 스레드

- 소스 코드 한 줄이 CPU 명령어 한 개와 그대로 대응하는 것은 아니다. `Add/add.cpp`의 `return a + b;`도 컴파일 설정에 따라 여러 명령어로 바뀔 수 있다.
- 프로세스는 실행 중인 프로그램이고, 스레드는 그 안에서 코드를 실행하는 흐름이다. OS가 스레드에 CPU 시간을 배분하고, 여러 코어에서는 실제로 병렬 실행도 가능하다.
- Unity에서는 입력 처리, `Update()`, UI와 오브젝트 조작 등 주요 작업이 메인 스레드에서 진행된다. 60 FPS라면 프레임당 약 16.7ms 안에 필요한 작업을 처리해야 한다.
- 1일차의 `while (!operation.isDone)`는 메인 스레드를 반복문 안에 붙잡는다. 요청 전송을 시작했더라도 그 함수가 반환하지 않으면 다음 프레임의 작업을 진행하지 못한다.

### 동기·비동기와 병렬의 구분

| 구분 | 아침 식사 비유 | 핵심 |
| --- | --- | --- |
| 동기 대기 | 토스트가 다 구워질 때까지 앞에서 기다린다 | 기다리는 스레드가 다른 일을 진행하지 못한다 |
| 비동기 | 토스트를 굽는 동안 다른 요리를 준비한다 | 기다리는 동안 호출자가 다른 일을 할 수 있다 |
| 병렬 | 여러 요리사가 각자 요리한다 | 여러 작업이 실제로 같은 순간에 실행된다 |

- **동시성**은 여러 작업이 같은 기간에 진행 중인 상태이고, **병렬성**은 실제 실행이 같은 순간에 겹치는 것이다.
- `async`를 붙인다고 새 스레드가 자동으로 생기거나 계산이 빨라지는 것은 아니다.
- 비동기는 네트워크 응답을 기다리는 동안 메인 스레드를 비워 줄 수 있다. 응답 후 JSON을 역직렬화하거나 UI를 만드는 계산 비용까지 없애 주지는 않는다.
- 코루틴도 일반적인 Unity 사용에서는 메인 스레드에서 실행된다. `yield return`으로 실행을 나누는 것이며, 무거운 계산을 자동으로 다른 스레드에 보내지는 않는다.

### async와 await — 시작과 기다림의 분리

- `Task`는 작업의 완료를 나타내고, `Task<T>`는 완료 후 `T` 타입의 결과를 받을 수 있다.
- `async` 메서드는 호출되면 실행을 시작하고, 완료되지 않은 작업을 `await`하는 지점에서 호출자에게 제어를 돌려줄 수 있다. 이미 끝난 작업이면 바로 이어서 실행할 수 있다.
- `await` 뒤의 코드는 기다리던 작업이 완료된 다음 실행된다. 재개되는 스레드는 실행 환경과 컨텍스트에 따라 달라지므로, 항상 같은 스레드라고 단정하면 안 된다.

```csharp
// 하나가 끝나야 다음 요리를 시작한다.
Egg eggs = await FryEggsAsync(2);
Bacon bacon = await FryBaconAsync(3);

// 서로 독립적인 요리는 먼저 시작해 두고 나중에 기다릴 수 있다.
Task<Egg> eggsTask = FryEggsAsync(2);
Task<Bacon> baconTask = FryBaconAsync(3);
Egg eggsResult = await eggsTask;
Bacon baconResult = await baconTask;
```

- 첫 방식도 비동기 대기지만, 두 요리의 대기 시간이 겹치지는 않는다. `await`를 붙이는 것만으로 전체 시간이 줄지는 않는 이유다.
- 두 번째 방식은 작업을 먼저 시작해 대기를 겹친다. 다만 빵을 구운 다음 버터와 잼을 바르는 것처럼 **선행 결과가 필요한 순서**는 유지해야 한다.
- 강의 예제는 달걀 6초, 베이컨 6초, 토스트 3초를 순차 처리하면 약 15초, 대기를 겹치면 약 6초로 설명한다.

### WhenAll, WhenAny와 예외

| 도구 | 기다리는 기준 | 사용할 때 |
| --- | --- | --- |
| `Task.WhenAll(...)` | 전달한 작업이 모두 완료됨 | 전체 결과가 준비된 뒤 진행할 때 |
| `Task.WhenAny(...)` | 전달한 작업 중 하나가 완료됨 | 먼저 끝난 작업부터 처리할 때 |

- `WhenAny`가 돌려주는 것은 **완료된 Task**다. 성공 여부와 결과는 그 Task를 다시 `await`해서 확인한다.
- 비동기 메서드 안에서 발생한 예외는 반환된 Task에 기록되고, 이를 `await`할 때 호출 쪽으로 전달된다.
- 하나가 실패했다고 다른 작업이 자동으로 취소되는 것은 아니다. 취소가 필요하다면 별도 설계가 필요하다.

### JSON 파일에서 SQLite로

1일차에는 시작할 때 `monsters.json`을 읽어 메모리에 보관했다. 이번에는 **SQLite의 `Data/game.db`에 데이터를 저장하고 요청마다 조회**하는 구조로 바뀌었다.

| 구분 | 1일차 구현 | 현재 구현 |
| --- | --- | --- |
| 데이터 보관 | JSON을 읽은 메모리 목록 | SQLite DB 파일 |
| 조회 | 메모리의 목록에서 찾기 | SQL `SELECT` 실행 |
| 추가·수정·삭제 | API에 저장 기능이 없었음 | SQL로 DB에 반영 |
| 재시작 | JSON 파일을 다시 읽음 | 저장된 DB를 다시 엶 |
| `monsters.json` 역할 | 조회 데이터의 원본 | 테이블이 비었을 때 넣는 초기 데이터 |

- SQLite는 별도 DB 서버 프로세스를 띄우지 않고 라이브러리를 통해 파일을 읽고 쓴다.
- 테이블은 데이터의 표, 행은 몬스터 한 마리, 열은 `id`, `name`, `hp`, `attack`, `speed` 같은 속성이다.
- DB가 있다고 동시 쓰기가 무제한으로 가능한 것은 아니다. 다른 도구가 쓰기 작업을 잡고 있으면 잠금 문제가 생길 수 있다.
- 현재 `Microsoft.Data.Sqlite`를 사용하는 DB 코드는 동기 호출이다. 로컬 파일에도 I/O 비용과 잠금 대기는 있다. 모든 DB 접근이 즉시 끝나는 것은 아니다.

### HTTP 요청과 SQL의 연결

| 요청 | 역할 | SQL | 현재 코드의 성공 응답 |
| --- | --- | --- | --- |
| `GET /monsters` | 목록 조회 | `SELECT` | 200 + `{ "monsters": [...] }` |
| `GET /monsters/{id}` | 한 마리 조회 | `SELECT ... WHERE` | 200 + 몬스터 객체 |
| `POST /monsters` | 새 몬스터 추가 | `INSERT` | 201 + 생성된 객체, `Location` 헤더 |
| `PUT /monsters/{id}` | 해당 몬스터 값 교체 | `UPDATE` | 200 + 수정된 객체 |
| `DELETE /monsters/{id}` | 해당 몬스터 삭제 | `DELETE` | 204, 본문 없음 |
| `PATCH /monsters/{id}` | 보낸 속성만 수정 | `UPDATE` + `COALESCE` | 200 + 수정된 객체 |

- POST·PUT은 이름이 비어 있거나 공백이면 400을 반환한다. PATCH는 이름을 보냈을 때 같은 검사를 한다.
- 단건 조회·수정·삭제는 해당 정수 ID가 없으면 404를 반환한다.
- 같은 경로라도 HTTP 메서드가 다르면 다른 작업이다. 브라우저 주소 입력만으로 POST·PUT·DELETE를 대신할 수는 없다.
- Postman에서 JSON 본문을 보낼 때는 Body를 `raw → JSON`으로 선택하고 `Content-Type: application/json`을 사용한다.

---

## 기억할 것

- **요청을 시작하는 것과 결과를 기다리는 방식은 별개**다. 서버에 `await Task.Delay`가 있어도 Unity의 동기 반복문은 그대로 메인 스레드를 막는다.
- SQL에 사용자 입력을 문자열로 이어 붙이지 않는다. `$id`, `$name` 같은 파라미터와 `AddWithValue`로 값을 따로 전달한다.
- `ExecuteReader()`는 결과 행을 읽고, `ExecuteScalar()`는 첫 결과의 첫 값을 받는다.
- `ExecuteNonQuery()`는 INSERT·UPDATE·DELETE 같은 명령을 실행하고, 영향받은 행 수를 돌려준다.
- `ReadMonsters`의 `GetInt32(0)` 같은 숫자는 SELECT에 나열한 열 순서다. 조회 열 순서와 읽는 코드가 맞아야 한다.
- 초기 데이터 INSERT에는 ID가 없다. JSON의 ID를 복사하는 대신 DB가 ID를 생성하므로, 새 몬스터 번호를 항상 6이라고 가정하면 안 된다.
- 초기 데이터 조건은 단순히 **테이블이 비어 있는가**이다. 모두 삭제한 뒤 서버를 재시작하면 JSON 초기 데이터가 다시 들어간다.
- 현재 PUT은 숫자 속성 누락을 별도로 검증하지 않는다. `name`만 보내면 생략한 숫자는 기본값 0으로 저장될 수 있으므로, 전체 값을 보내는 습관이 필요하다.
- 현재 PATCH는 누락된 속성과 명시적인 JSON `null`을 모두 기존 값 유지로 처리한다. `hp: 0`은 실제 수정값이다.

---

## 실습 코드

### 1. 먼저 끝난 아침 식사 처리

`AsyncBreakfast/Program.cs`는 달걀·베이컨·토스트·해시브라운을 먼저 시작하고 `WhenAny`로 완료된 작업부터 처리한다. 아래는 핵심 흐름을 줄인 코드다.

```csharp
Task<Egg> eggsTask = FryEggsAsync(2);
Task<Bacon> baconTask = FryBaconAsync(3);
Task<Toast> toastTask = MakeToastWithButterAndJamAsync(2);
Task<HashBrown> hashBrwonTask = FryHashBrownAsync(2);

var breakfastTasks = new List<Task>
{
    eggsTask, baconTask, toastTask, hashBrwonTask
};
while (breakfastTasks.Count > 0)
{
    Task finishedTask = await Task.WhenAny(breakfastTasks);
    await finishedTask;
    // 실제 코드에서는 어떤 요리인지 확인하고 완료 메시지를 출력한다.
    breakfastTasks.Remove(finishedTask);
}
```

- 완료된 Task를 목록에서 제거해야 다음 완료를 기다릴 수 있다.
- 현재 파일은 `ToastBreadAsync`에서 2초 대기 후 `InvalidOperationException`을 던지는 **예외 실험 상태**다.
- 예외는 토스트를 기다리는 `MakeToastWithButterAndJamAsync`를 거쳐 Main의 `await finishedTask`로 전달된다. 현재 Main에는 이를 잡는 `try/catch`가 없어 정상 완료 메시지까지 진행하지 않는 흐름이다.

### 2. DB 준비와 초기 데이터

`GameDataServer/Program.cs`의 서버 시작 흐름.

```text
ContentRootPath로 Data/game.db 경로 생성
→ SQLite 연결 열기
→ CREATE TABLE IF NOT EXISTS
→ SELECT COUNT(*)
→ 비어 있을 때만 monsters.json을 읽어 INSERT
→ HTTP 요청 받기
```

```csharp
var dbPath = Path.Combine(app.Environment.ContentRootPath, "Data", "game.db");
var connectionString = $"Data Source={dbPath}";
```

- `GameDataServer.csproj`에는 `Microsoft.Data.Sqlite` 10.0.13 참조가 있다.
- `using` 범위가 끝나면 연결을 정리한다. 조회 요청은 `LoadAllMonsters`에서 DB를 다시 읽는다.
- DB에 데이터가 이미 있으면 JSON을 수정해도 초기 데이터가 다시 들어가지 않는다.
- DB Browser로 수정했다면 `Write Changes`로 저장해야 한다. 잠금 오류가 나면 저장하거나 변경을 되돌렸는지 확인한다.

### 3. ID 조회와 없는 데이터 구분

```csharp
cmd.CommandText = "SELECT id, name, hp, attack, speed FROM monsters WHERE id = $id";
cmd.Parameters.AddWithValue("$id", id);
using var reader = cmd.ExecuteReader();
var found = ReadMonsters(reader);
return found.Count == 0
    ? Results.NotFound(new { error = $"id {id} 몬스터가 없습니다." })
    : Results.Ok(found[0]);
```

- 경로의 `{id:int}`로 정수 ID를 받는다.
- 목록 응답은 `monsters`로 감싼 객체지만 단건 응답은 몬스터 객체 자체다. 클라이언트에서도 응답 형태를 구분해야 한다.

### 4. POST와 PUT, DELETE 요청 흐름

POST 본문 예시. ID는 DB가 생성한다.

```json
{
  "name": "실습 좀비",
  "hp": 150,
  "attack": 25,
  "speed": 2.5
}
```

```text
POST /monsters → INSERT → last_insert_rowid() → 201 + 생성된 객체
PUT /monsters/{생성된 ID} → UPDATE → 200 + 수정된 객체
DELETE /monsters/{생성된 ID} → DELETE → 204
GET /monsters/{삭제한 ID} → 조회 결과 없음 → 404
```

- PUT의 대상은 URL의 ID다. 본문 ID가 다르더라도 응답에는 URL의 ID를 사용한다.
- UPDATE·DELETE가 영향받은 행 수를 확인해 대상이 없으면 404를 반환한다.
- 위 순서는 확인용 요청 예시다.

### 5. PATCH로 HP만 수정

```http
PATCH http://localhost:5000/monsters/5
Content-Type: application/json

{ "hp": 777 }
```

```csharp
// MonsterPatch.Hp는 int?이므로 생략하면 null이다.
cmd.Parameters.AddWithValue("$hp", (object?)p.Hp ?? DBNull.Value);
```

```sql
UPDATE monsters
SET hp = COALESCE($hp, hp)
WHERE id = $id;
```

- 위 SQL은 HP 부분만 줄인 예시다. 실제 코드는 이름·HP·공격력·속도를 각각 같은 방식으로 처리한다.
- `COALESCE($hp, hp)`는 전달값이 SQL NULL이면 기존 HP를 선택한다. C#의 `null`은 파라미터에 `DBNull.Value`로 전달한다.
- 5번 몬스터가 존재한다면 HP만 바뀌고 나머지는 유지된다. 해당 ID가 없으면 404다.

### 6. 다음 실습을 위한 서버 주소

| 요청 | 현재 코드의 역할 |
| --- | --- |
| `GET /hello` | 서버 확인용 문자열 |
| `GET /monsters/slow?ms=3000` | 지정 시간만큼 비동기로 지연한 뒤 DB 목록 반환 |
| `GET /monsters/big?count=20000` | DB 목록을 반복 복제한 테스트용 목록 반환 |

- `/big`은 DB에 2만 마리를 추가하는 기능이 아니다. 응답용 메모리 목록을 생성한다.
- `MonsterDex/Assets/Scripts/MonsterClientSync.cs`에는 여전히 최대 10초 동안 반복문으로 기다리는 코드가 있다. Unity의 비동기 전환과 CRUD 버튼 연결은 다음 수업(3일차)에서 다룬다.
- 1일차 문서의 `/monsters/count`는 당시 코드 기록이다. 현재 `Program.cs`에는 해당 엔드포인트가 없다.

---

## 정리

- 비동기는 기다리는 동안 다른 일을 진행하도록 흐름을 나누는 것이다. 독립적인 작업을 먼저 시작하면 대기를 겹칠 수 있고, 결과가 필요한 순서는 유지해야 한다.
- `WhenAny`는 완료된 작업을 알려 주며, 그 작업을 다시 `await`해야 실패도 확인할 수 있다.
- 서버는 JSON 조회에서 SQLite 기반 CRUD로 확장됐다. HTTP 요청이 서버의 SQL 실행으로 연결되고, 저장된 변경은 DB 파일에 남는 구조다.
- 현재 폴더와 소스, 강의 01~03을 대조해 작성했다. 강의 예제의 실행 시간, 예외 실험의 실행 결과, 서버 HTTP 응답, 재시작 후 데이터 유지, Unity 화면 동작은 이번 문서 작성 중 직접 실행해 확인하지 않았다.

### 참고 자료

- [01. 동기와 비동기의 원리](https://app.notion.com/p/e76a72f14e0e82b5ac898142cfab35b8)
- [02. async와 await — 아침 식사로 배우는 비동기](https://app.notion.com/p/750a72f14e0e8372a9a4810d2ba35574)
- [03. 서버 고도화 — SQLite와 요청 네 가지](https://app.notion.com/p/361a72f14e0e8376bb0381d56469ccd5)
