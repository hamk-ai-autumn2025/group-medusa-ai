using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static dev.susybaka.TurnBasedGame.AI.AIHandler;

namespace dev.susybaka.TurnBasedGame.AI
{
    public class AIHandler : MonoBehaviour
    {
        public bool useApi = true;

        #region Snapshot
        //[SerializeField] public class Boss { public int hp; public int maxHp; public int attackPower; public int defense; public StatusEffect[] statusEffects; }
        [Serializable] public class PartyMember { public string id; public int hp; public bool alive = true; public StatusEffect[] status_effects; public bool analyzed = false; public Stat[] analyzed_stats; }
        [Serializable]
        public class Enemy { public string id; public int hp; public int max_hp; public int attack_power; public int defense; public bool alive = true; public StatusEffect[] status_effects; }
        [Serializable] public class Ability { public string id; public bool requires_target; public string[] tags; public bool attack; public int use_count = 0; public int last_turn_used = -1; } // e.g., ["aoe","fire"]
        [Serializable] public class StatusEffect { public string id; public int duration; public int stacks; public string[] tags; } // e.g., "burning", "stun"
        [Serializable] public class RecentBehavior { public string element_spam; public int guard_streak; public int heal_streak; }
        [Serializable] public class Knowledge { public string id; public string lore; }
        [Serializable] public class Stat { public string id; public string value; } // type = "ally" or "enemy"
        [Serializable] public class StoredTurn { public string ability_id; public StoredTurnPartyMember[] player_party; public StoredTurnEnemy[] enemy_party; }
        [Serializable] public class StoredTurnPartyMember { public string id; public int hp; public bool alive = true; public int status_effect_count; }
        [Serializable] public class StoredTurnEnemy { public string id; public int hp; public bool alive = true; public int status_effect_count; }


        [Serializable]
        public class Snapshot
        {
            public int turn;
            public string boss_id;
            //public Boss boss = new();
            public List<PartyMember> player_party = new();
            public List<Enemy> enemy_party = new();
            public RecentBehavior recent_player_behavior = new();
            public List<Ability> abilities = new();
            public List<Knowledge> lore_knowledge = new();
            public StoredTurn previous_turn = new StoredTurn();
        }

        [Serializable] public class StoredMood { public string boss_id; public int aggression_level; public int fear_level; public int respect_level; public int pity_level; public string dialogue; }

        [Serializable]
        public class  ActSnapshot
        {
            public int turn;
            public string boss_id;
            public RecentBehavior recent_player_behavior = new();
            public string player_action; // e.g., "taunted", "pleaded for mercy"
            public int aggression_level;
            public int fear_level;
            public int respect_level;
            public int pity_level;
            public StoredMood previous_mood = new StoredMood();
        }

        // Build the Snapshot JSON from your current state
        public static Snapshot BuildSnapshot(
            int turn, string bossId,
            IEnumerable<(string id, int hp, bool alive, IEnumerable<(string id, int duration, int stacks, IEnumerable<string> tags)> statusEffects, bool analyzed, IEnumerable<(string id, string value)> analyzedStats)> playerParty,
            IEnumerable<(string id, int hp, int maxHp, int attackPower, int defense, bool alive, IEnumerable<(string id, int duration, int stacks, IEnumerable<string> tags)> statusEffects)> enemyParty,
            string elementSpam, int guardStreak, int healStreak,
            IEnumerable<(string id, bool requiresTarget, IEnumerable<string> tags, bool attack, int useCount, int lastTurnUsed)> abilities,
            IEnumerable<(string id, string lore)> bossKnowledge,
            string previousBossAbilityId, IEnumerable<(string id, int hp, bool alive, int statusEffectCount)> previousPlayerParty, IEnumerable<(string id, int hp, bool alive, int statusEffectCount)> previousEnemyParty)
        {
            Snapshot snap = new Snapshot
            {
                turn = turn,
                boss_id = bossId,
                recent_player_behavior = new RecentBehavior 
                { 
                    element_spam = elementSpam, 
                    guard_streak = guardStreak, 
                    heal_streak = healStreak 
                },
                player_party = playerParty.Select(p => new PartyMember 
                { 
                    id = p.id, 
                    hp = p.hp, 
                    alive = p.alive, 
                    status_effects = p.statusEffects.Select(pse => new StatusEffect 
                    { 
                        id = pse.id, 
                        duration = pse.duration, 
                        stacks = pse.stacks, 
                        tags = pse.tags.ToArray() 
                    }).ToArray(), 
                    analyzed = p.analyzed, 
                    analyzed_stats = p.analyzedStats.Select(aps => new Stat 
                    { 
                        id = aps.id, 
                        value = aps.value 
                    }).ToArray() 
                }).ToList(),
                enemy_party = enemyParty.Select(e => new Enemy 
                { 
                    id = e.id, 
                    hp = e.hp, 
                    max_hp = e.maxHp, 
                    attack_power = e.attackPower, 
                    defense = e.defense, 
                    alive = e.alive, 
                    status_effects = e.statusEffects.Select(ese => new StatusEffect 
                    { 
                        id = ese.id, 
                        duration = ese.duration, 
                        stacks = ese.stacks, 
                        tags = ese.tags.ToArray() 
                    }).ToArray() 
                }).ToList(),
                abilities = abilities.Select(a => new Ability 
                { 
                    id = a.id, 
                    requires_target = a.requiresTarget, 
                    tags = a.tags.ToArray(), 
                    attack = a.attack, 
                    use_count = a.useCount,
                    last_turn_used = a.lastTurnUsed 
                }).ToList(),
                lore_knowledge = bossKnowledge.Select(k => new Knowledge 
                { 
                    id = k.id, 
                    lore = k.lore 
                }).ToList(),
                previous_turn = new StoredTurn 
                { 
                    ability_id = previousBossAbilityId, 
                    player_party = previousPlayerParty.Select(pp => new StoredTurnPartyMember 
                    { 
                        id = pp.id, 
                        hp = pp.hp, 
                        alive = pp.alive, 
                        status_effect_count = pp.statusEffectCount
                    }).ToArray(), 
                    enemy_party = previousEnemyParty.Select(ep => new StoredTurnEnemy 
                    { 
                        id = ep.id, 
                        hp = ep.hp, 
                        alive = ep.alive, 
                        status_effect_count = ep.statusEffectCount 
                    }).ToArray()
                }
            };
            return snap;
        }

        public static ActSnapshot BuildActSnapshot(
            int turn, string bossId,
            string elementSpam, int guardStreak, int healStreak,
            string playerAction,
            int aggressionLevel, int fearLevel, int respectLevel, int pityLevel,
            string previousBossId, int previousAggressionLevel, int previousFearLevel, int previousRespectLevel, int previousPityLevel, string previousDialogue)
        {
            ActSnapshot snap = new ActSnapshot
            {
                turn = turn,
                boss_id = bossId,
                recent_player_behavior = new RecentBehavior
                {
                    element_spam = elementSpam,
                    guard_streak = guardStreak,
                    heal_streak = healStreak
                },
                player_action = playerAction,
                aggression_level = aggressionLevel,
                fear_level = fearLevel,
                respect_level = respectLevel,
                pity_level = pityLevel,
                previous_mood = new StoredMood
                {
                    boss_id = previousBossId,
                    aggression_level = previousAggressionLevel,
                    fear_level = previousFearLevel,
                    respect_level = previousRespectLevel,
                    pity_level = previousPityLevel,
                    dialogue = previousDialogue
                }
            };
            return snap;
        }
        #endregion

        #region OpenAI Response
        // DTOs for Responses API
        [Serializable] class ResponsesRoot { public OutputEntry[] output; }
        [Serializable] class OutputEntry { public string type; public ContentPiece[] content; }
        [Serializable] class ContentPiece { public string type; public string text; }
        // DTO for our decision
        [Serializable] public class Decision { public string ability_id; public string target_id; public string rationale; }
        [Serializable] public class Mood { public string boss_id; public int aggression_level; public int fear_level; public int respect_level; public int pity_level; public string dialogue; public string rationale; }

        private const string Endpoint = "https://api.openai.com/v1/responses";

        private const string systemCombat = "You are the BOSS in a turn-based game. Your ONLY job is to try and defeat the player's party by picking ONE boss ability and target for it from the provided party lists each turn. "
       + "NEVER invent IDs. ONLY ever pick abilities from each turn's own abilities list, NEVER pick abilities that are not present on the current newest turn's abilities json list. The BOSS'S own information is an entry with matching name to the boss_id string in the enemy_party list. "
       + "If an ability has requires_target=false, return target_id=\"none\". If requires_target=true, return target_id=\"{id}\" from the CORRECT party list, player_party for ATTACKS or enemy_party for SUPPORT. Previous turn's information if turn > 1 is inside previous_turn and should be used to track state across multiple turns."
       + "VARIETY: NEVER pick the same ability that was used on previous turn previous_turn.ability_id unless it is the only option you can pick. Consider use_count and last_turn_used to avoid repetition; prefer synergies and coverage. If turn > 1, do not pick abilities as an opener/start or describe anything as such."
       + "NEVER choose abilities whose tags include type_analyze with targets that have analyzed=true already unless significant amount of turns have passed (turns > last_turn_used + 3) since last use of an ability with the specified tag. You control and choose ONLY the current enemy boss's (boss_id) next action. Output strictly as JSON per schema.";

        private const string systemAct = "You are the BOSS in a turn-based game. Your job is to set your mood each turn and respond with short dialogue based on the player's recent behavior. You can see your previous mood from previous_mood. "
            + "Aggression level is how much you want to attack and KO the player, it is increased by the player acting disrespectful towards you or otherwise not caring. Fear level is how much you are scared of losing, it increases your chance to miss attacks. Respect level is how much you agree with the player, with high enough respect you can accept the player's request for mercy and end the battle without KOs. Pity level is how much you feel bad for the player or yourself, it defines your will to continue the battle, if your pity reaches 100 the battle can end. "
            + "Return a fitting mood value for aggression_level, fear_level, respect_level and pity_level as an integer ranging from 0-100. ONLY alter them based on the current state. Return the same boss_id back that was requested with the json. "
            + "Return a short Dialogue taunt or comment (max 42 characters) directed at the player that reflects your mood, also return rationale as a brief explanation of why you set your mood this way. ";

        private string requestTemplateCombat = string.Empty;
        private string requestTemplateAct = string.Empty;
        private string apiKey = string.Empty;

        static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");

        public IEnumerator DecideCoroutine(Snapshot snapshot, Action<Decision> onComplete)
        {
            // Build ability json enum list to inject as options for response ability_id
            string abilityEnums = ToJsonArray(snapshot.abilities.Select(a => a.id));

            // Build the user payload once
            string stateJson = JsonUtility.ToJson(snapshot);

            if (!useApi)
            {
                onComplete?.Invoke(null);
                yield break;
            }

            Debug.Log($"[AIHandler] Snapshot json:\n\n{stateJson}");

            // Load the premade Responses API body json and it's decision json_schema along with our API key
            if (string.IsNullOrEmpty(requestTemplateCombat))
                requestTemplateCombat = LoadJson("request.json");
            //if (string.IsNullOrEmpty(decisionSchema))
                //decisionSchema = LoadJson("decision_schema.json");
            if (string.IsNullOrEmpty(apiKey))
                apiKey = LoadApiKey();

            // Create the final request body that we will inject our data into and send out
            string requestBody = requestTemplateCombat;

            // Inject system, state, and schema
            requestBody = requestBody.Replace("{{SYSTEM}}", Escape(systemCombat));
            requestBody = requestBody.Replace("{{USER}}", Escape(stateJson));
            requestBody = requestBody.Replace("{{ABILITY_ENUM}}", abilityEnums);
            //responseBody.Replace("{{SCHEMA}}", decisionSchema);

            Debug.Log($"[AIHandler] Final request json:\n\n{requestBody}");

            // Validate we have everything
            if (string.IsNullOrEmpty(requestBody) /*|| string.IsNullOrEmpty(decisionSchema)*/ || string.IsNullOrEmpty(apiKey))
            {
                Debug.LogWarning("AIHandler: Missing request.json or OpenAI API key!");
                onComplete?.Invoke(null);
                yield break;
            }

            // Post to OpenAI
            yield return StartCoroutine(PostWithRetries(Endpoint, apiKey, requestBody, www => {
                if (www.result == UnityWebRequest.Result.Success)
                {
                    Decision decision = null;
                    try
                    {
                        Debug.Log($"[AIHandler] OpenAI response:\n\n{www.downloadHandler.text}");

                        // 1) Parse the Responses envelope
                        ResponsesRoot root = JsonUtility.FromJson<ResponsesRoot>(www.downloadHandler.text);
                        // 2) Find the message item
                        OutputEntry msg = root?.output?.FirstOrDefault(o => o.type == "message");
                        // 3) Find the output_text chunk
                        string outText = msg?.content?.FirstOrDefault(c => c.type == "output_text")?.text;

                        // outText is the strict-JSON string the schema produced (already unescaped by JsonUtility)
                        if (!string.IsNullOrEmpty(outText))
                            decision = JsonUtility.FromJson<Decision>(outText);
                    }
                    catch { /* ignore */ }

                    onComplete?.Invoke(IsValid(decision, snapshot) ? decision : null);
                }
                else
                {
                    Debug.LogError($"[AIHandler] UnityWebRequest failed!\nText: '{www.downloadHandler.text}'\nError: '{www.error}'");
                    onComplete?.Invoke(null);
                }
            }));          
        }

        public IEnumerator RunActRequest(ActSnapshot snapshot, Action<Mood> onComplete)
        {
            // Build the user payload once
            string stateJson = JsonUtility.ToJson(snapshot);

            if (!useApi)
            {
                onComplete?.Invoke(null);
                yield break;
            }

            Debug.Log($"[AIHandler] ActSnapshot json:\n\n{stateJson}");

            // Load the premade Responses API body json and it's decision json_schema along with our API key
            if (string.IsNullOrEmpty(requestTemplateAct))
                requestTemplateAct = LoadJson("request2.json");
            //if (string.IsNullOrEmpty(decisionSchema))
            //decisionSchema = LoadJson("decision_schema.json");
            if (string.IsNullOrEmpty(apiKey))
                apiKey = LoadApiKey();

            // Create the final request body that we will inject our data into and send out
            string requestBody = requestTemplateAct;

            // Inject system, state, and schema
            requestBody = requestBody.Replace("{{SYSTEM}}", Escape(systemAct));
            requestBody = requestBody.Replace("{{USER}}", Escape(stateJson));

            Debug.Log($"[AIHandler] Final mood request json:\n\n{requestBody}");

            // Validate we have everything
            if (string.IsNullOrEmpty(requestBody) /*|| string.IsNullOrEmpty(decisionSchema)*/ || string.IsNullOrEmpty(apiKey))
            {
                Debug.LogWarning("AIHandler: Missing request2.json or OpenAI API key!");
                onComplete?.Invoke(null);
                yield break;
            }

            // Post to OpenAI
            yield return StartCoroutine(PostWithRetries(Endpoint, apiKey, requestBody, www => {
                if (www.result == UnityWebRequest.Result.Success)
                {
                    Mood mood = null;
                    try
                    {
                        Debug.Log($"[AIHandler] Mood OpenAI response:\n\n{www.downloadHandler.text}");

                        // 1) Parse the Responses envelope
                        ResponsesRoot root = JsonUtility.FromJson<ResponsesRoot>(www.downloadHandler.text);
                        // 2) Find the message item
                        OutputEntry msg = root?.output?.FirstOrDefault(o => o.type == "message");
                        // 3) Find the output_text chunk
                        string outText = msg?.content?.FirstOrDefault(c => c.type == "output_text")?.text;

                        // outText is the strict-JSON string the schema produced (already unescaped by JsonUtility)
                        if (!string.IsNullOrEmpty(outText))
                            mood = JsonUtility.FromJson<Mood>(outText);
                    }
                    catch { /* ignore */ }

                    onComplete?.Invoke(IsValid(mood, snapshot) ? mood : null);
                }
                else
                {
                    Debug.LogError($"[AIHandler] Mood UnityWebRequest failed!\nText: '{www.downloadHandler.text}'\nError: '{www.error}'");
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
        #endregion

        #region Helpers
        // Helper method for loading the OpenAI API key from common locations
        static string LoadApiKey()
        {
            try
            {
                string pathA = Path.Combine(Application.streamingAssetsPath, "openai_key.txt");
                if (File.Exists(pathA))
                {
                    Debug.Log("OpenAI API Key found succesfully from streaming assets!");
                    return File.ReadAllText(pathA).Trim();
                }
#if UNITY_EDITOR
                string pathB = Path.Combine(Application.dataPath, "openai_key.txt");
                if (File.Exists(pathB))
                {
                    Debug.Log("OpenAI API Key found succesfully from assets!");
                    return File.ReadAllText(pathB).Trim();
                }
#endif
                string env = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (!string.IsNullOrEmpty(env))
                {
                    Debug.Log("OpenAI API Key found succesfully from environment variables!");
                    return env.Trim();
                }
            }
            catch { }
            return null;
        }

        // Helper method for loading json text files from common locations
        static string LoadJson(string fileName)
        {
            try
            {
                string pathA = Path.Combine(Application.streamingAssetsPath, fileName);
                if (File.Exists(pathA))
                    return File.ReadAllText(pathA);
#if UNITY_EDITOR
                string pathB = Path.Combine(Application.dataPath, fileName);
                if (File.Exists(pathB))
                    return File.ReadAllText(pathB);
#endif
#if UNITY_WEBGL || UNITY_ANDROID
                string pathC = Path.Combine(Application.streamingAssetsPath, fileName);
                var www = UnityWebRequest.Get(pathC);
                var op = www.SendWebRequest();
                while (!op.isDone)
                { }
                if (www.result == UnityWebRequest.Result.Success)
                    return www.downloadHandler.text;
#endif
            }
            catch { }
            return null;
        }

        static string ToJsonArray(IEnumerable<string> items)
        {
            var safe = items.Select(s => s.Replace("\\", "\\\\").Replace("\"", "\\\""));
            return "[\"" + string.Join("\",\"", safe) + "\"]";
        }

        // Validate the returned decision is usable
        static bool IsValid(Decision d, Snapshot s)
        {
            if (d == null)
                return false;
            // Check if ability is a support action or an attack
            bool abilityTargetsFriendly = s.abilities.Any(a => a.id == d.ability_id && !a.attack && !a.tags.Contains("type_analyze"));
            // Check if ability exists
            bool abilityOK = s.abilities.Any(a => a.id == d.ability_id);
            // Check if target exists in the correct party for ability type or is self
            bool targetOK = (s.player_party.Any(pm => pm.id == d.target_id) && !abilityTargetsFriendly) || (s.enemy_party.Any(em => em.id == d.target_id) && abilityTargetsFriendly) || (!string.IsNullOrEmpty(d.target_id) && d.target_id.Contains("none")) || (!string.IsNullOrEmpty(d.target_id) && d.target_id.Contains("self"));
            return abilityOK && targetOK;
        }

        // Validate the returned mood is usable
        static bool IsValid(Mood m, ActSnapshot s)
        {
            if (m == null)
                return false;
            bool levelsOK = m.aggression_level >= 0 && m.aggression_level <= 100
                && m.fear_level >= 0 && m.fear_level <= 100
                && m.respect_level >= 0 && m.respect_level <= 100
                && m.pity_level >= 0 && m.pity_level <= 100;
            bool dialogueOK = !string.IsNullOrEmpty(m.dialogue) && m.dialogue.Length <= 140;
            return levelsOK && dialogueOK;
        }
        #endregion
    }
}