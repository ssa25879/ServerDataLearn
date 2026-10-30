using System;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace Ranking
{
    public class RankingClient : MonoBehaviour
    {
        public string serverBaseUrl = "http://localhost:5000";
        public TMP_InputField nameInput;
        public TMP_InputField scoreInput;
        public UnityEngine.UI.Button submitButton;
        public UnityEngine.UI.Button loadButton;
        public TMP_Text statusText;
        public TMP_Text rankingText;

        private bool isBusy;
        private UnityWebRequest activeRequest;

        private void Awake()
        {
            submitButton.onClick.AddListener(OnClickSubmit);
            loadButton.onClick.AddListener(OnClickLoadRanking);
        }

        private void OnDestroy()
        {
            activeRequest?.Abort();
        }

        private void SetBusy(bool busy)
        {
            isBusy = busy;
            submitButton.interactable = !busy;
            loadButton.interactable = !busy;
        }

        public async void OnClickSubmit()
        {
            if (isBusy) return;
            string playerName = nameInput.text.Trim();
            if (playerName.Length == 0)
            {
                statusText.text = "이름을 입력하세요.";
                return;
            }
            if (!int.TryParse(scoreInput.text, out int score) || score < 0)
            {
                statusText.text = "점수는 0부터 2147483647 사이의 정수로 입력하세요.";
                return;
            }

            SetBusy(true);
            statusText.text = "점수를 저장하고 있습니다...";
            try
            {
                ScoreEntry saved = await SubmitScoreAsync(playerName, score);
                if (this == null) return;
                string success = $"저장 성공 (201): {saved.name} · 최고 점수 {saved.score} · 번호 {saved.id}";
                statusText.text = success;
                try
                {
                    ScoreList list = await FetchScoresAsync();
                    if (this == null) return;
                    ShowRanking(list);
                    statusText.text = success;
                }
                catch (Exception e)
                {
                    if (this != null) statusText.text = success + "\n목록 조회 실패: " + e.Message;
                }
            }
            catch (Exception e)
            {
                if (this != null) statusText.text = "등록 실패: " + e.Message;
            }
            finally
            {
                if (this != null) SetBusy(false);
            }
        }

        public async void OnClickLoadRanking()
        {
            if (isBusy) return;
            SetBusy(true);
            statusText.text = "랭킹을 불러오고 있습니다...";
            try
            {
                ScoreList list = await FetchScoresAsync();
                if (this == null) return;
                ShowRanking(list);
                statusText.text = "상위 10명 조회 완료";
            }
            catch (Exception e)
            {
                if (this != null) statusText.text = "조회 실패: " + e.Message;
            }
            finally
            {
                if (this != null) SetBusy(false);
            }
        }

        private async Task<ScoreEntry> SubmitScoreAsync(string playerName, int score)
        {
            string body = JsonUtility.ToJson(new ScoreSubmission { name = playerName, score = score });
            return JsonUtility.FromJson<ScoreEntry>(await SendAsync(body));
        }

        private async Task<ScoreList> FetchScoresAsync()
        {
            return JsonUtility.FromJson<ScoreList>(await SendAsync());
        }

        private async Task<string> SendAsync(string body = null)
        {
            string url = serverBaseUrl.TrimEnd('/') + "/scores";
            using (UnityWebRequest request = body == null
                ? UnityWebRequest.Get(url)
                : UnityWebRequest.Post(url, body, "application/json"))
            {
                request.timeout = 15;
                activeRequest = request;
                try
                {
                    await request.SendWebRequest();
                    long expectedStatus = body == null ? 200 : 201;
                    if (request.result != UnityWebRequest.Result.Success || request.responseCode != expectedStatus)
                    {
                        if (request.responseCode == 400)
                        {
                            var error = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                            throw new Exception(error.error);
                        }
                        throw new Exception($"HTTP {request.responseCode}: {request.error}");
                    }
                    return request.downloadHandler.text;
                }
                finally { activeRequest = null; }
            }
        }

        private void ShowRanking(ScoreList list)
        {
            if (list.scores.Length == 0)
            {
                rankingText.text = "아직 등록된 점수가 없습니다.";
                return;
            }
            var text = new StringBuilder();
            foreach (ScoreEntry entry in list.scores)
                text.AppendLine($"{entry.rank}위    {entry.name}    {entry.score}점");
            rankingText.text = text.ToString();
        }
    }

    [Serializable]
    public class ScoreSubmission { public string name; public int score; }

    [Serializable]
    public class ScoreEntry { public long id; public string name; public int score; public int rank; }

    [Serializable]
    public class ScoreList { public ScoreEntry[] scores; }

    [Serializable]
    public class ErrorResponse { public string error; }
}
