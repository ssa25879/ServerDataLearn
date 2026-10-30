using System;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using UnityEngine.UI;

// 2회차 · 비동기 요청: 같은 요청을 async와 await로 — 응답을 기다리는 동안 화면은 계속 돈다
public class MonsterClientAsync : MonoBehaviour
{
    public string baseUrl = "http://localhost:5000/monsters";   // 네 요청이 전부 이 주소에서 시작한다
    public TMP_Text resultText;
    public TMP_InputField nameInput;   // 04-3. 추가할 몬스터 이름
    public TMP_InputField idInput;     // 04-4, 04-5. 고치거나 지울 몬스터 번호
    public TMP_InputField hpInput;     // 04-4. 새 HP
    public Button loadButton;          // 04-1. 요청 중에는 네 버튼이 전부 회색
    public Button addButton;
    public Button updateButton;
    public Button deleteButton;
    
    private bool isLoading = false;            // 04-1. 요청 상태: true면 "요청 중"

    // 04-1. 요청 상태를 한곳에서 바꾼다
    void SetBusy(bool busy)
    {
        isLoading = busy;
        loadButton.interactable = !busy;
        addButton.interactable = !busy;
        updateButton.interactable = !busy;
        deleteButton.interactable = !busy;
    }

    // ── 불러오기 — GET /monsters ──────────────────────────────────────────
    public async void OnClickLoad()
    {
        if (isLoading) return;         // 04-1. 이미 요청 중이면 무시 (중복 클릭 방지)
        SetBusy(true);
        resultText.text = "요청 중...";

        try
        {
            MonsterList list = await FetchMonstersAsync(baseUrl);   // 끝날 때까지 이 메서드만 멈추고 메인은 돌아간다
            ShowMonsters(list);
        }
        catch (Exception e)
        {
            resultText.text = "실패: " + e.Message;   // 04-6. DescribeError가 만든 사람 말 메시지
        }
        finally
        {
            SetBusy(false);            // 04-1. 성공이든 실패든 버튼을 되살린다
        }
    }

    // GET: 목록을 받는다
    async Task<MonsterList> FetchMonstersAsync(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 5;              // 04-6. 5초 안에 응답이 없으면 실패로 끝낸다
            await request.SendWebRequest();   // 여기서 메인으로 돌아간다

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception(DescribeError(request));

            return JsonUtility.FromJson<MonsterList>(request.downloadHandler.text);
        }
    }

    // 04-6. 실패 원인을 사람 말로 바꾼다
    static string DescribeError(UnityWebRequest request)
    {
        string body = request.downloadHandler != null ? request.downloadHandler.text : "";
        if (request.error != null && request.error.Contains("timeout"))
            return "시간 초과. 서버가 너무 느립니다";
        if (request.result == UnityWebRequest.Result.ConnectionError)
            return "서버에 연결할 수 없습니다. 서버가 켜져 있나요?";
        if (request.responseCode == 404)
            return "없는 몬스터 번호입니다 (404)";
        if (request.responseCode == 400)
            return "서버가 요청을 거절했습니다 (400): " + body;
        return $"{request.responseCode}: {request.error}";
    }
    
        // ── 추가 — POST /monsters ─────────────────────────────────────────────
    public async void OnClickAdd()
    {
        if (isLoading) return;
        string newName = nameInput.text.Trim();   // name은 MonoBehaviour의 속성이라 다른 이름을 쓴다
        if (newName.Length == 0)
        {
            resultText.text = "이름을 입력하세요";
            return;
        }

        SetBusy(true);
        resultText.text = "요청 중...";

        try
        {
            var body = new Monster { name = newName, hp = 100, attack = 10, speed = 1.0f };   // id는 서버가 붙이므로 비워 둔다
            Monster created = await PostMonsterAsync(baseUrl, body);   // 서버에 저장 (DB에 INSERT)
            nameInput.text = "";
            MonsterList list = await FetchMonstersAsync(baseUrl);      // 저장이 끝난 뒤에 목록을 다시 읽는다
            ShowMonsters(list, $"{created.id}번 {created.name} 추가됨");
        }
        catch (Exception e)
        {
            resultText.text = "실패: " + e.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    // POST: 본문에 JSON을 실어 보낸다. 서버가 id를 붙인 몬스터를 돌려준다
    async Task<Monster> PostMonsterAsync(string url, Monster monster)
    {
        string json = JsonUtility.ToJson(monster);   // 직렬화: C# 객체 → JSON 글자
        using (UnityWebRequest request = UnityWebRequest.Post(url, json, "application/json"))
        {
            request.timeout = 5;
            await request.SendWebRequest();

            if (request.responseCode != 201)   // 서버는 성공하면 201 Created
                throw new Exception(DescribeError(request));

            return JsonUtility.FromJson<Monster>(request.downloadHandler.text);
        }
    }
        // ── HP 고치기 — GET /monsters/{id} 다음 PUT /monsters/{id} ────────────
    public async void OnClickUpdate()
    {
        if (isLoading) return;
        if (!int.TryParse(idInput.text, out int id) || !int.TryParse(hpInput.text, out int newHp))
        {
            resultText.text = "번호와 HP를 숫자로 입력하세요";
            return;
        }

        SetBusy(true);
        resultText.text = "요청 중...";

        try
        {
            Monster m = await FetchMonsterAsync($"{baseUrl}/{id}");    // PUT은 통째로 바꾸기라 먼저 한 마리를 읽어 온다
            m.hp = newHp;                                               // 바꿀 칸만 고친다
            Monster updated = await PutMonsterAsync($"{baseUrl}/{id}", m);   // 서버에 보낸다 (DB에 UPDATE)
            MonsterList list = await FetchMonstersAsync(baseUrl);
            ShowMonsters(list, $"{updated.id}번 {updated.name} HP → {updated.hp}");
        }
        catch (Exception e)
        {
            resultText.text = "실패: " + e.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    // GET: 한 마리를 받는다
    async Task<Monster> FetchMonsterAsync(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 8;
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception(DescribeError(request));

            return JsonUtility.FromJson<Monster>(request.downloadHandler.text);
        }
    }

    // PUT: 본문에 JSON을 실어 보낸다. Put()은 본문 종류를 octet-stream으로 적으므로 JSON이라고 다시 적어 준다
    async Task<Monster> PutMonsterAsync(string url, Monster monster)
    {
        string json = JsonUtility.ToJson(monster);
        using (UnityWebRequest request = UnityWebRequest.Put(url, json))
        {
            request.SetRequestHeader("Content-Type", "application/json");   // 이 줄이 없으면 서버가 415로 거절한다
            request.timeout = 5;
            await request.SendWebRequest();

            if (request.responseCode != 200)   // 서버는 성공하면 200 OK + 바뀐 몬스터
                throw new Exception(DescribeError(request));

            return JsonUtility.FromJson<Monster>(request.downloadHandler.text);
        }
    }
    
    // ── 삭제 — DELETE /monsters/{id} ──────────────────────────────────────
    public async void OnClickDelete()
    {
        if (isLoading) return;
        if (!int.TryParse(idInput.text, out int id))
        {
            resultText.text = "번호를 숫자로 입력하세요";
            return;
        }

        SetBusy(true);
        resultText.text = "요청 중...";

        try
        {
            await DeleteMonsterAsync($"{baseUrl}/{id}");               // 서버에서 지운다 (DB에 DELETE)
            MonsterList list = await FetchMonstersAsync(baseUrl);
            ShowMonsters(list, $"{id}번 삭제됨");
        }
        catch (Exception e)
        {
            resultText.text = "실패: " + e.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    // DELETE: 본문이 없다. Delete()는 응답을 담을 그릇(DownloadHandler)을 안 붙이므로 하나 붙여 준다 (404 메시지를 읽기 위해)
    async Task DeleteMonsterAsync(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Delete(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 5;
            await request.SendWebRequest();

            if (request.responseCode != 204)   // 서버는 성공하면 204 No Content
                throw new Exception(DescribeError(request));
        }
    }

    // ── 화면 ────────────────────────────────────────────────────────────────
    void ShowMonsters(MonsterList list, string headline = null)
    {
        var sb = new StringBuilder();
        if (headline != null) sb.AppendLine(headline);   // 방금 한 일을 첫 줄에
        sb.AppendLine($"총 {list.monsters.Length}마리");
        int shown = Mathf.Min(list.monsters.Length, 20);   // 화면에는 20마리까지만
        for (int i = 0; i < shown; i++)
        {
            Monster m = list.monsters[i];
            sb.AppendLine($"{m.id}. {m.name}   HP {m.hp}   공격 {m.attack}   속도 {m.speed}");
        }
        resultText.text = sb.ToString();
    }

    void ShowMonsters(MonsterList list)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"총 {list.monsters.Length}마리");
        int shown = Mathf.Min(list.monsters.Length, 20);   // 화면에는 20마리까지만 (2만 줄을 넣으면 그것만으로 멈춘다)
        for (int i = 0; i < shown; i++)
        {
            Monster m = list.monsters[i];
            sb.AppendLine($"{m.id}. {m.name}   HP {m.hp}   공격 {m.attack}   속도 {m.speed}");
        }
        resultText.text = sb.ToString();
    }
}