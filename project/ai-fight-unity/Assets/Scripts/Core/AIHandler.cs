using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace dev.susybaka.TurnBasedGame.AI
{
    public class AIHandler : MonoBehaviour
    {
        public bool useApi = true;
        [Serializable] public class PartyMember { public string id; public string characterDescription; public int hp; public bool alive = true; public StatusEffect[] statusEffects; }
        [Serializable] public class Ability { public string id; public string description; public bool requiresTarget; public string[] tags; public string[] conditions; } // e.g., ["aoe","fire"]
        [Serializable] public class StatusEffect { public string id; public int duration; public int stacks; public string description; public string[] tags; } // e.g., "burning", "stunned"
        //[Serializable] public class RecentBehavior { public string element_spam; public int guard_streak; }
        [Serializable] public class Knowledge { public string id; public string description; } // e.g., "fire_weakness", "regen"

        [Serializable]
        public class Snapshot
        {
            public int turn;
            public int boss_hp;
            public int boss_attack_power;
            public int boss_defense;
            public string boss_character_description;
            public List<StatusEffect> boss_statusEffects = new();
            public List<PartyMember> player_party = new();
            //public RecentBehavior recent_player_behavior = new();
            public List<Ability> abilities = new();
            public List<Knowledge> knowledge = new();
            public List<string> valid_targets = new();
        }

        [Serializable] class ChatMessage { public string role; public string content; }
        [Serializable] class RespFmt { public string type = "json_object"; }
        [Serializable]
        class ChatReq
        {
            public string model = "gpt-4o-mini";
            public ChatMessage[] messages;
            public RespFmt response_format = new RespFmt();
            public float temperature = 0.4f;
            public int max_tokens = 120;
        }
        [Serializable] class Choice { public ChatMessage message; }
        [Serializable] class ChatRes { public Choice[] choices; }
        [Serializable] public class Decision { public string ability_id; public string target_id; public string rationale; }

        const string Endpoint = "https://api.openai.com/v1/chat/completions";

        public IEnumerator DecideCoroutine(Snapshot snapshot, Action<Decision> onComplete)
        {
            // Build the user payload once
            var stateJson = JsonUtility.ToJson(snapshot);

            if (!useApi)
            {
                Debug.Log($"AI Snapshot json:\n\n{stateJson}");
                onComplete?.Invoke(null);
                yield break;
            }

            // System prompt
            var system = "You are a turn based battle AI selector. Pick exactly ONE ability and ONE target (If required) from the lists I provide. "
                       + "Only output valid JSON with keys: ability_id, target_id, rationale. If the ability has no targets required use 'none' as the target_id. Never invent IDs. "
                       + "Prefer counters to repeated player behavior. Try to keep your strategy varied. Keep rationale short.";

            var req = new ChatReq
            {
                messages = new[] {
                new ChatMessage{ role="system", content=system },
                new ChatMessage{ role="user", content=stateJson }
            }
            };
            var reqJson = JsonUtility.ToJson(req);
            var apiKey = LoadApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                onComplete?.Invoke(null);
                yield break;
            }

            yield return StartCoroutine(PostWithRetries(Endpoint, apiKey, reqJson, www => {
                if (www.result == UnityWebRequest.Result.Success)
                {
                    Decision decision = null;
                    try
                    {
                        var res = JsonUtility.FromJson<ChatRes>(www.downloadHandler.text);
                        var content = res?.choices?[0]?.message?.content ?? "";
                        // Safety: strip any leading/trailing text, keep innermost JSON block
                        int s = content.IndexOf('{');
                        int e = content.LastIndexOf('}');
                        if (s >= 0 && e >= s)
                            content = content.Substring(s, e - s + 1);
                        decision = JsonUtility.FromJson<Decision>(content);
                    }
                    catch { /* ignore and fall back */ }

                    onComplete?.Invoke(IsValid(decision, snapshot) ? decision : null);//RandomFallback(snapshot));
                }
                else
                {
                    Debug.LogWarning($"UnityWebRequest failed!\nText: '{www.downloadHandler.text}'\nError: '{www.error}'");
                    onComplete?.Invoke(null);
                }
            }));          
        }

        static float nextOkTime;
        const float MinInterval = 0.6f; // throttle
        IEnumerator PostWithRetries(string endpoint, string apiKey, string bodyJson, Action<UnityWebRequest> onDone, int maxRetries = 3)
        {
            // throttle
            var wait = nextOkTime - Time.realtimeSinceStartup;
            if (wait > 0)
                yield return new WaitForSeconds(wait);

            int attempt = 0;
            while (true)
            {
                attempt++;
                using var www = new UnityWebRequest(endpoint, "POST");
                www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyJson));
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", "Bearer " + apiKey);

                yield return www.SendWebRequest();
                nextOkTime = Time.realtimeSinceStartup + MinInterval; // schedule next slot

                // Success
                if (www.result == UnityWebRequest.Result.Success)
                { onDone?.Invoke(www); yield break; }

                // Retry 429/5xx
                long code = www.responseCode;
                if ((code == 429 || code >= 500) && attempt < maxRetries)
                {
                    // respect Retry-After if present
                    var ra = www.GetResponseHeader("Retry-After");
                    float backoff = !string.IsNullOrEmpty(ra) && float.TryParse(ra, out var s)
                        ? s
                        : Mathf.Pow(2f, attempt) + UnityEngine.Random.Range(0f, 0.3f);
                    yield return new WaitForSeconds(backoff);
                    continue;
                }

                // Give up
                onDone?.Invoke(www);
                yield break;
            }
        }

        static string LoadApiKey()
        {
            try
            {
                var pathA = Path.Combine(Application.streamingAssetsPath, "openai_key.txt");
                if (File.Exists(pathA))
                {
                    Debug.Log("OpenAI API Key found succesfully from streaming assets!");
                    return File.ReadAllText(pathA).Trim();
                }
#if UNITY_EDITOR
                var pathB = Path.Combine(Application.dataPath, "openai_key.txt");
                if (File.Exists(pathB))
                {
                    Debug.Log("OpenAI API Key found succesfully from assets!");
                    return File.ReadAllText(pathB).Trim();
                }
#endif
                var env = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (!string.IsNullOrEmpty(env))
                {
                    Debug.Log("OpenAI API Key found succesfully from environment variables!");
                    return env.Trim();
                }
            }
            catch { }
            return null;
        }

        static bool IsValid(Decision d, Snapshot s)
        {
            if (d == null)
                return false;
            bool abilityOK = s.abilities.Any(a => a.id == d.ability_id /*&& a.cd <= 0*/);
            bool targetOK = s.valid_targets.Contains(d.target_id) || string.IsNullOrEmpty(d.target_id) || d.target_id.Contains("none") || d.target_id.Contains("self");
            return abilityOK && targetOK;
        }

        /*Decision RandomFallback(Snapshot s)
        {
            var pool = s.abilities;//.Where(a => a.cd <= 0).ToList();
            if (pool.Count == 0)
                return new Decision { ability_id = "none", target_id = "none", rationale = "fallback-none" };
            var ability = pool[UnityEngine.Random.Range(0, pool.Count)].id;
            var target = s.valid_targets[UnityEngine.Random.Range(0, s.valid_targets.Count)];
            return new Decision { ability_id = ability, target_id = target, rationale = "fallback-random" };
        }*/

        // Build the Snapshot JSON from your current state
        public static Snapshot BuildSnapshot(
            int turn, int bossHp, int bossAttackPower, int bossDefense, string bossCharacterDescription,
            IEnumerable<(string id, int duration, int stacks, string description, IEnumerable<string> tags)> bossStatusEffects,
            IEnumerable<(string id, string description, int hp, bool alive, IEnumerable<(string id, int duration, int stacks, string description, IEnumerable<string> tags)> statusEffects)> party,
            //string elementSpam, int guardStreak,
            IEnumerable<(string id, string description, bool requiresTarget, IEnumerable<string> tags, IEnumerable<string> conditions)> abilities,
            IEnumerable<(string id, string description)> knowledge,
            IEnumerable<string> validTargets)
        {
            var snap = new Snapshot
            {
                turn = turn,
                boss_hp = bossHp,
                boss_attack_power = bossAttackPower,
                boss_defense = bossDefense,
                boss_statusEffects = bossStatusEffects.Select(se => new StatusEffect { id = se.id, duration = se.duration, stacks = se.stacks, description = se.description, tags = se.tags.ToArray() }).ToList(),
                boss_character_description = bossCharacterDescription,
                //recent_player_behavior = new RecentBehavior { element_spam = elementSpam, guard_streak = guardStreak },
                player_party = party.Select(p => new PartyMember { id = p.id, characterDescription = p.description, hp = p.hp, alive = p.alive, statusEffects = p.statusEffects.Select(pse => new StatusEffect { id = pse.id, duration = pse.duration, stacks = pse.stacks, description = pse.description, tags = pse.tags.ToArray() }).ToArray() }).ToList(),
                abilities = abilities.Select(a => new Ability { id = a.id, description = a.description, requiresTarget = a.requiresTarget, tags = a.tags.ToArray(), conditions = a.conditions.ToArray() }).ToList(),
                knowledge = knowledge.Select(k => new Knowledge { id = k.id, description = k.description }).ToList(),
                valid_targets = validTargets.ToList()
            };
            return snap;
        }
    }
}