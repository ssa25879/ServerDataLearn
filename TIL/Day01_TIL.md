# 1일차 서버 기초

## 오늘 배운 내용

### 서버란 무엇인가

- 서버는 물리적인 컴퓨터 장비 자체를 뜻하기보다, 클라이언트의 요청을 기다렸다가 알맞은 응답을 돌려주는 **프로그램(프로세스)**을 의미한다.
- 따라서 서버가 꼭 멀리 떨어진 다른 컴퓨터에 있어야 하는 것은 아니다. 이번 실습에서도 한 대의 PC에서 서버와 Unity 클라이언트를 동시에 띄워 통신하는 구조를 사용했다.
- 이때 서버 주소로 사용하는 `localhost`는 요청을 보내는 기기(내 PC) 자신을 가리킨다.
- **역할 분리:** Unity 클라이언트는 데이터를 요청해 화면에 그려주고, 서버는 요청받은 데이터를 준비해 응답한다.
- 데이터 구조(틀)만 유지된다면 몬스터 HP 같은 세부 수치를 바꿀 때 서버 데이터만 수정하고 Unity에서 다시 요청해 받아오면 된다. 수치 하나를 바꿀 때마다 무거운 클라이언트를 새로 빌드하는 번거로움을 줄일 수 있다.

### 연결 관점과 전송 관점

| 구분 | 연결을 유지하며 주고받기 | HTTP 요청과 응답 |
| --- | --- | --- |
| **수업의 비유** | 전화 | 택배 |
| **사용하는 예** | 실시간 플레이어 위치, 대전 중 입력 동기화 | 몬스터 목록, 아이템 정보, 랭킹 조회 |
| **생각할 기준** | 계속 바뀌는 상태를 실시간으로 주고받는가? | 필요할 때만 데이터를 요청해 받아오면 되는가? |

- 이번 실습의 몬스터 데이터 조회는 필요할 때만 요청해서 받아오는 **HTTP 방식(택배)**에 해당한다.
- 주의할 점은 HTTP의 '요청·응답 단위'와 실제 하단의 '네트워크 연결 수명'은 구분해야 한다는 것이다. 응답이 끝났다고 해서 내부적인 네트워크 연결까지 매번 무조건 끊어지는 것은 아니다.

### URL 읽는 법

```text
http://localhost:5000/monsters
```

| 부분 | 의미 |
| --- | --- |
| `http://` | 데이터를 주고받을 통신 규칙 (프로토콜) |
| `localhost` | 요청을 보낼 호스트 (여기서는 내 PC) |
| `:5000` | 서버 프로그램이 요청을 기다리는 포트 번호 |
| `/monsters` | 요청할 데이터의 구체적인 경로 |

- 데이터를 조회할 때는 HTTP 메서드 중 `GET`을 사용한다.
- 요청을 처리하는 API의 접점을 **엔드포인트(Endpoint)**라고 하며, 보통 `GET /monsters`처럼 메서드와 경로를 묶어서 읽는다.
- 주요 상태 코드: `200 OK`(요청 성공), `404 Not Found`(요청한 경로의 자원을 찾을 수 없음).

### JSON과 C# 객체

- **JSON:** 서로 다른 환경(서버 ↔ Unity)에서 데이터를 주고받기 위해 사용하는 텍스트 기반의 공통 데이터 형식.
- 객체는 `{ }`, 배열(목록)은 `[ ]`로 표현한다. 키(Key)와 문자열에는 반드시 큰따옴표(`" "`)를 쓰고, 마지막 항목 뒤에는 쉼표를 붙이지 않는다.
- **직렬화(Serialization):** C# 객체를 전송 가능한 JSON 문자열로 변환하는 과정.
- **역직렬화(Deserialization):** JSON 문자열을 코드에서 쓸 수 있는 C# 객체로 변환하는 과정.

```json
{
  "monsters": [
    { "id": 1, "name": "일반 좀비", "hp": 100, "attack": 20, "speed": 2.0 }
  ]
}
```

Unity의 `JsonUtility`로 JSON을 읽으려면 키와 1:1로 대응하는 C# 데이터 클래스가 필요하다. 클래스에는 `[Serializable]`을 붙이고 필드는 `public`으로 선언해야 하며, **필드 이름이 JSON 키와 대소문자까지 정확히 일치**해야 한다.

```csharp
using System;

[Serializable]
public class MonsterList
{
    public Monster[] monsters;
}
```

- 위 코드는 `Monster.cs` 중 목록 전체를 감싸는 래퍼(Wrapper) 클래스 부분이다. (개별 몬스터 속성은 같은 파일의 `Monster` 클래스에 정의됨)
- `JsonUtility`는 `[ ... ]` 형태의 최상위 배열을 직접 역직렬화하지 못하는 제약이 있어서, 배열을 필드로 가진 객체로 한 번 감싸서 읽어야 한다.
- 때문에 원본 파일(`monsters.json`)은 배열로 되어 있더라도, 서버가 응답할 때는 `{ "monsters": [...] }` 형태로 한 번 감싸서 내려주고 Unity는 이를 `MonsterList` 타입으로 받는다.

---

## 기억할 것

- 서버의 기본 흐름은 **요청 수신 → 데이터 준비 → 응답 반환**이다.
- C# 미니멀 API에서는 `MapGet`으로 특정 `GET` 경로와 처리 함수를 간단히 연결한다.
- 객체를 반환하면 자동으로 JSON 직렬화되어 응답되지만, `/hello`처럼 단순 문자열만 반환할 수도 있다.
- 현재 서버 코드는 **서버가 켜질 때 JSON 파일을 딱 한 번만 읽어서 메모리에 올려둔다.** 실행 중에 `monsters.json`을 수정해도 서버를 재시작하지 않으면 변경 사항이 반영되지 않는다.
- 서버의 파일 읽기는 `System.Text.Json`, Unity의 응답 읽기는 `JsonUtility`를 사용한다. 서로 다른 라이브러리이므로 지원하는 옵션과 제약 사항이 다르다.
- 예를 들어 서버 코드의 `PropertyNameCaseInsensitive = true`는 서버가 파일을 읽을 때 대소문자를 무시하겠다는 설정일 뿐이다. Unity 쪽 클래스 필드 이름까지 대소문자 구분 없이 지어도 된다는 뜻은 아니므로 주의해야 한다.
- 서버에서 `await Task.Delay`로 비동기 지연을 주더라도, 요청을 보낸 Unity 클라이언트의 대기 방식이 알아서 비동기로 바뀌지는 않는다.

---

## 실습 코드

### 1. 서버에서 파일을 읽고 응답하기

`GameDataServer/Program.cs`의 핵심 부분.

```csharp
var dataPath = Path.Combine(app.Environment.ContentRootPath, "Data", "monsters.json");
var json = File.ReadAllText(dataPath);
var monsters =
    JsonSerializer.Deserialize<List<Monster>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

app.MapGet("/monsters", () => new { monsters });
app.MapGet("/monsters/count", () => new { count = monsters?.Count });
```

1. `ContentRootPath`를 기준으로 `Data/monsters.json` 경로를 생성한다.
2. 파일 내용을 문자열로 읽어온다.
3. JSON 문자열을 `Monster` 리스트로 역직렬화한다.
4. `/monsters` 요청 시 리스트를 감싼 익명 객체를 반환한다.
5. `/monsters/count` 요청 시 로드된 몬스터의 총 마릿수를 반환한다.

**현재 정의된 엔드포인트 정리 (코드 기준)**

| 요청 | 코드 기준 응답 |
| --- | --- |
| `GET /hello` | 서버 동작 확인용 문자열 |
| `GET /monsters` | 몬스터 목록 배열을 감싼 객체 |
| `GET /monsters/count` | 전체 몬스터 수 (현재 파일 기준 5마리) |
| `GET /monsters/{id}` | 해당 ID의 몬스터 정보 (일치하는 정수 ID가 없으면 404 및 오류 객체) |
| `GET /monsters/slow?ms=3000` | 쿼리로 지정한 시간(ms)만큼 대기 후 몬스터 목록 반환 |

### 2. Unity에서 요청하고 JSON 읽기

`MonsterClientSync.cs`의 전체 실행 흐름.

```text
버튼 클릭 → OnClickLoad → GET 요청 전송 → 응답 대기
          → 성공 여부 확인 → JSON 역직렬화 → UI 텍스트 표시
```

```csharp
// 응답 수신 후 실행하는 ShowMonsters 메서드의 일부
MonsterList list = JsonUtility.FromJson<MonsterList>(json);

var sb = new StringBuilder();
foreach (Monster m in list.monsters)
{
    sb.AppendLine($"{m.id}. {m.name}   HP {m.hp}   ATK {m.attack}   SPD {m.speed}");
}
resultText.text = sb.ToString();
```

- `UnityWebRequest`로 HTTP 요청을 보내고 `downloadHandler.text`에서 응답 본문(JSON 문자열)을 가져온다.
- `JsonUtility.FromJson<MonsterList>(json)`으로 C# 객체를 생성한다.
- 반복문으로 몬스터 정보를 한 줄씩 이어 붙여 `TMP_Text`에 출력한다.

### 3. 동기 대기로 화면이 멈추는 이유

현재 클라이언트는 `SendWebRequest()` 호출 직후 아래 `while` 반복문에서 응답을 기다린다.

```csharp
var watch = System.Diagnostics.Stopwatch.StartNew();
while (!operation.isDone)
{
    if (watch.ElapsedMilliseconds > 10000) break;
}
```

- 요청 전송을 시작하는 것과 메인 스레드가 결과를 기다리는 방식은 별개다.
- 메인 스레드가 `while` 문을 계속 돌며 묶여 있기 때문에, 응답이 올 때까지 다음 프레임의 `Update()`나 화면 갱신(렌더링)이 전혀 진행되지 못한다.
- 요청 직전에 `Loading...` 텍스트를 대입해 두어도 화면을 새로 그릴 틈이 없어서 사용자 눈에는 텍스트가 바뀌는 모습조차 보이지 않고 화면이 그대로 멈춘다.
- `/monsters/slow`로 느린 응답을 호출해 이 블로킹 현상을 직접 관찰하는 것이 1일차의 핵심이며, 해결책인 코루틴과 비동기 처리는 2일차에 이어서 다룬다.

---

## 정리

- 서버와 클라이언트는 물리적 기기 구분이 아니라, 요청과 응답이라는 각자의 역할을 맡은 프로그램이다.
- JSON은 서로 다른 환경의 프로그램 사이에서 데이터를 주고받기 위한 공통 텍스트 형식이다.
- 서버에 요청을 보내는 기능 구현뿐만 아니라, **응답을 기다리는 동안 메인 스레드를 어떻게 동작하게 할지** 설계하는 것도 중요하다.
