# ServerDataLearn

.NET 서버와 Unity 클라이언트를 연동하며 HTTP 요청과 JSON 데이터 처리를 연습하는 저장소입니다.

GameDataServer를 독립적으로 배포하기 위해 서버를 별도 저장소로 분리했습니다.
서버 소스와 실행 안내는 [GameDataServer 저장소](https://github.com/ssa25879/GameDataServer.git)에서 확인할 수 있습니다.
이 저장소는 Unity 클라이언트와 관련 실습 자료를 관리합니다.

## 프로젝트 구성

| 디렉토리 | 역할 | 개발 환경 |
| --- | --- | --- |
| 별도 [GameDataServer 저장소](https://github.com/ssa25879/GameDataServer.git) | SQLite 기반 몬스터 데이터를 제공하는 ASP.NET Core 서버 | .NET 10.0 |
| `MonsterDex/` | 서버에 몬스터 데이터를 요청하고 화면에 표시하는 Unity 프로젝트 | Unity 6000.3.19f1 |

## 서버 실행

.NET 10 SDK가 설치된 환경에서 서버 저장소를 `GameDataServer` 폴더로 별도 클론한 뒤 실행합니다.
이미 해당 폴더에 서버 저장소가 있다면 클론은 생략합니다.

```powershell
git clone https://github.com/ssa25879/GameDataServer.git GameDataServer
dotnet run --project GameDataServer/GameDataServer.csproj
```

서버 주소는 `http://localhost:5000`입니다. 현재 `Program.cs`에서 실행 주소를 지정합니다.
몬스터 원본 데이터는 `GameDataServer/Data/monsters.json`에 있습니다.

| 요청 | 응답 |
| --- | --- |
| `GET /hello` | 서버 확인 메시지 |
| `GET /monsters` | 전체 몬스터 목록 |
| `GET /monsters/count` | 몬스터 수 |
| `GET /monsters/{id}` | 지정 ID의 몬스터 또는 404 |
| `GET /monsters/slow?ms=3000` | 지정 시간만큼 지연한 몬스터 목록 |

## Unity 프로젝트 열기

1. Unity Hub에서 `MonsterDex` 폴더를 프로젝트로 추가합니다.
2. Unity `6000.3.19f1`로 열고 패키지 복원과 에셋 임포트를 기다립니다.
3. 서버를 실행한 뒤 `Assets/Scenes/SampleScene.unity`를 열어 실습합니다.

`MonsterClientSync`의 기본 요청 주소는 `http://localhost:5000/monsters`입니다.
현재 코드는 응답을 기다리는 동안 메인 스레드를 막는 동기 요청 실습용 구현입니다.

## Git 관리 범위

Unity의 `Assets`와 `.meta` 파일, `Packages`, `ProjectSettings`를 함께 관리합니다.
`Library`, `Temp`, IDE 캐시와 .NET의 `bin`, `obj`는 제외하며 개발 도구에서 다시 생성합니다.
Unity의 자동 생성 솔루션 파일과 `GameDataServer/` 폴더는 이 저장소의 관리 대상에서 제외합니다.
서버의 프로젝트·솔루션 파일은 별도 GameDataServer 저장소에서 관리합니다.
