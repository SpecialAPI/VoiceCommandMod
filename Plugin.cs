using BepInEx;
using DiskCardGame;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Pixelplacement;
using UnityEngine.Windows.Speech;
using System.Text.RegularExpressions;
using BepInEx.Logging;

namespace VoiceCommandMod
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "spapi.inscryption.voicecommandmod";
        public const string NAME = "Voice Command Mod";
        public const string VERSION = "1.0.0";
        public const float CONSOLE_CLEAR_DELAY = 1;
        public static ManualLogSource Log;

        public void Awake()
        {
            using(Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("VoiceCommandMod.sfx"))
            {
                bundle = AssetBundle.LoadFromStream(s);
            }
            GameObject reviver = new GameObject("Recognizer Reviver");
            reviver.AddComponent<Reviver>();
            DontDestroyOnLoad(reviver);
            Log = Logger;
        }

        public static void CreateRecognizer()
        {
            /*
            if (recognizer == null)
            {
                recognizer = new KeywordRecognizer(commands.Keys.ToArray());
                recognizer.OnPhraseRecognized += OnCommandRecognized;
            }
            recognizer.Start();
            */
            try
            {
                drecognizer?.Dispose();
            }
            catch { }
            drecognizer = new DictationRecognizer();
            drecognizer.DictationResult += DictationRecognized;
            drecognizer.Start();
        }

        public static void DictationRecognized(string str, ConfidenceLevel conf)
        {
            if (SaveManager.SaveFile.Part1NotLeshyFinale)
            {
                List<string> toLog = new List<string>();
                foreach (KeyValuePair<string[], Func<IEnumerator>> kvp in commands)
                {
                    int occurances = 0;
                    foreach (string s in kvp.Key)
                    {
                        int matches = Regex.Matches(str, s).Count;
                        occurances += matches;
                        if (matches > 0)
                        {
                            toLog.Add(" ^ sentance contains \"" + s + "\" " + matches + " times.");
                        }
                    }
                    if (occurances > 0)
                    {
                        CustomCoroutine.Instance.StartCoroutine(CommandRecognizedCR(kvp.Key));
                    }
                }
                if(toLog.Count > 0)
                {
                    Debug.LogError("Recognized sentance \"" + str + "\" with " + conf.ToString().ToLower() + " confidence.");
                    foreach(string s in toLog)
                    {
                        Debug.LogError(s);
                    }
                    if(consoleClearCR != null)
                    {
                        try { CustomCoroutine.Instance.StopCoroutine(DelayedClearConsole()); } catch { }
                    }
                    consoleClearCR = CustomCoroutine.Instance.StartCoroutine(DelayedClearConsole());
                }
                else
                {
                    Debug.LogError("Recognized sentance \"" + str + "\" with " + conf.ToString().ToLower() + " confidence.");
                    if (consoleClearCR != null)
                    {
                        try { CustomCoroutine.Instance.StopCoroutine(DelayedClearConsole()); } catch { }
                    }
                    consoleClearCR = CustomCoroutine.Instance.StartCoroutine(DelayedClearConsole());
                }
            }
        }

        public static IEnumerator DelayedClearConsole()
        {
            yield return new WaitForSeconds(CONSOLE_CLEAR_DELAY);
            Debug.ClearDeveloperConsole();
            yield break;
        }

        public static IEnumerator CommandRecognizedCR(string[] args)
        {
            if (!CanActivate)
            {
                yield return new WaitUntil(() => CanActivate);
            }
            if (Singleton<TurnManager>.Instance != null && !Singleton<TurnManager>.Instance.GameEnded && SaveManager.SaveFile.Part1NotLeshyFinale)
            {
                GlobalTriggerHandler.Instance.StackSize++;
                yield return commands[args]?.Invoke();
                GlobalTriggerHandler.Instance.StackSize--;
            }
            yield break;
        }

        public static void OnCommandRecognized(PhraseRecognizedEventArgs args)
        {
            Debug.LogWarning("recognized " + args.text);
            //CustomCoroutine.Instance.StartCoroutine(CommandRecognizedCR(args));
        }

        public static bool CanActivate
        {
            get
            {
                return Singleton<TurnManager>.Instance != null && !Singleton<TurnManager>.Instance.GameEnded && !Singleton<BoardManager>.Instance.ChoosingSacrifices && !Singleton<BoardManager>.Instance.ChoosingSlot &&
                    Singleton<TurnManager>.Instance.IsPlayerMainPhase && Singleton<GlobalTriggerHandler>.Instance.StackSize == 0;
            }
        }

        public static bool SoundEnabled
        {
            get
            {
                return true;
            }
        }

        public static IEnumerator PlaySoundAndWait(string sound, float wait = 1.5f)
        {
            if(AudioController.Instance.GetAudioClip(sound) == null)
            {
                AudioController.Instance.SFX.Add(bundle.LoadAsset<AudioClip>(sound));
            }
            AudioController.Instance.PlaySound2D(sound, MixerGroup.None, 1f, 0f, null, null, null, null, false);
            yield return new WaitForSeconds(wait);
            yield break;
        }

        /*public static IEnumerator CommandRecognizedCR(PhraseRecognizedEventArgs args)
        {
            if (!CanActivate)
            {
                yield return new WaitUntil(() => CanActivate);
            }
            if (Singleton<TurnManager>.Instance != null && !Singleton<TurnManager>.Instance.GameEnded && SaveManager.SaveFile.IsPart1)
            {
                GlobalTriggerHandler.Instance.StackSize++;
                yield return commands[args.text]?.Invoke();
                GlobalTriggerHandler.Instance.StackSize--;
            }
            yield break;
        }*/

        public static IEnumerator Bear()
        {
            yield return PlaySoundAndWait("bear");
            yield return Tutorial4BattleSequencer.BearGlitchSequence();
            yield break;
        }

        public static IEnumerator Gold()
        {
            yield return PlaySoundAndWait("gold");
            int numCardsInPlayerSlots = 0;
            foreach (CardSlot slot in Singleton<BoardManager>.Instance.PlayerSlotsCopy)
            {
                bool flag = slot.Card != null && slot.Card.Info.name != "GoldNugget";
                if (flag)
                {
                    int num = numCardsInPlayerSlots;
                    numCardsInPlayerSlots = num + 1;
                }
            }
            bool flag2 = numCardsInPlayerSlots > 0;
            if (flag2)
            {
                foreach (CardSlot slot2 in Singleton<BoardManager>.Instance.PlayerSlotsCopy)
                {
                    bool flag3 = slot2.Card != null;
                    if (flag3)
                    {
                        yield return slot2.Card.Die(false, null, true);
                        if (slot2.Card == null)
                        {
                            yield return new WaitForSeconds(0.25f);
                            yield return Singleton<BoardManager>.Instance.CreateCardInSlot(CardLoader.GetCardByName("GoldNugget"), slot2, 0.1f, true);
                        }
                    }
                }
                yield return new WaitForSeconds(0.1f);
            }
            yield break;
        }

        public static IEnumerator Fish()
        {
            yield return PlaySoundAndWait("bait");
            List<CardSlot> baitSlots = Singleton<BoardManager>.Instance.OpponentSlotsCopy.FindAll((CardSlot x) => x.opposingSlot.Card != null);
            foreach (CardSlot slot in baitSlots)
            {
                yield return Singleton<BoardManager>.Instance.CreateCardInSlot(CardLoader.GetCardByName("BaitBucket"), slot, 0.2f, true);
                slot.Card.AddPermanentBehaviour<Bait>();
                yield return new WaitForSeconds(0.25f);
            }
            yield break;
        }

        private static bool PeltInHand()
        {
            return Singleton<PlayerHand>.Instance.CardsInHand.Exists((PlayableCard x) => x.Info.HasTrait(Trait.Pelt));
        }

        private static void OnTradableSelected(HighlightedInteractable slot, PlayableCard card)
        {
            bool flag = PeltInHand();
            if (flag)
            {
                AscensionStatsData.TryIncrementStat(AscensionStat.Type.PeltsTraded);
                PlayableCard pelt = Singleton<PlayerHand>.Instance.CardsInHand.Find((PlayableCard x) => x.Info.HasTrait(Trait.Pelt));
                Singleton<PlayerHand>.Instance.RemoveCardFromHand(pelt);
                pelt.SetEnabled(false);
                pelt.Anim.SetTrigger("fly_off");
                Tween.Position(pelt.transform, pelt.transform.position + new Vector3(0f, 3f, 5f), 0.4f, 0f, Tween.EaseInOut, Tween.LoopType.None, null, delegate ()
                {
                    Destroy(pelt.gameObject);
                }, true);
                card.UnassignFromSlot();
                Tween.Position(card.transform, card.transform.position + new Vector3(0f, 0.25f, -5f), 0.3f, 0f, Tween.EaseInOut, Tween.LoopType.None, null, delegate ()
                {
                    Destroy(card.gameObject);
                }, true);
                CustomCoroutine.Instance.StartCoroutine(Singleton<PlayerHand>.Instance.AddCardToHand(CardSpawner.SpawnPlayableCard(card.Info), new Vector3(0f, 0.5f, -3f), 0f));
                slot.ClearDelegates();
                slot.HighlightCursorType = CursorType.Default;
            }
        }

        public static IEnumerator Trade()
        {
            yield return PlaySoundAndWait("trade"); 
            Singleton<ViewManager>.Instance.SwitchToView(View.Hand, false, false);
            yield return new WaitForSeconds(0.75f);
            yield return CardSpawner.Instance.SpawnCardToHand(CardLoader.GetCardByName("PeltGolden"), 0.25f);
            yield return new WaitForSeconds(0.75f);
            Singleton<ViewManager>.Instance.SwitchToView(View.Default, false, false);
            yield return Singleton<TurnManager>.Instance.Opponent.ClearBoard();
            yield return Singleton<TurnManager>.Instance.Opponent.ClearQueue();
            yield return new WaitForSeconds(0.25f);
            Singleton<ViewManager>.Instance.SwitchToView(View.Board, false, false);
            yield return new WaitForSeconds(0.75f);
            foreach (CardSlot slot in BoardManager.Instance.OpponentSlotsCopy)
            {
                yield return BoardManager.Instance.CreateCardInSlot(CardLoader.GetCardByName("Urayuli"), slot, 0.1f, false);
                PlayableCard card = slot.Card;
                card.RenderInfo.hiddenCost = false;
                card.RenderCard();
                yield return TurnManager.Instance.Opponent.QueueCard(CardLoader.GetCardByName("Urayuli"), slot, true, true, true);
                PlayableCard card2 = Singleton<TurnManager>.Instance.Opponent.Queue.Find((PlayableCard x) => x.QueuedSlot == slot);
                card2.AddTemporaryMod(new CardModificationInfo { fromCardMerge = true, abilities = new List<Ability> { Ability.TriStrike, Ability.DoubleStrike } });
                card2.RenderInfo.hiddenCost = false;
                card2.RenderCard();
            }
            foreach (CardSlot slot in BoardManager.Instance.OpponentSlotsCopy)
            {
                bool flag6 = slot.Card != null;
                if (flag6)
                {
                    slot.CursorSelectStarted = (Action<MainInputInteractable>)Delegate.Combine(slot.CursorSelectStarted, new Action<MainInputInteractable>(delegate (MainInputInteractable i)
                    {
                        OnTradableSelected(slot, slot.Card);
                    }));
                    slot.HighlightCursorType = CursorType.Pickup;
                }
            }
            foreach(PlayableCard card in TurnManager.Instance.Opponent.Queue)
            {
                HighlightedInteractable slot = Singleton<BoardManager>.Instance.OpponentQueueSlots[Singleton<BoardManager>.Instance.OpponentSlotsCopy.IndexOf(card.QueuedSlot)];
                slot.CursorSelectStarted = (Action<MainInputInteractable>)Delegate.Combine(slot.CursorSelectStarted, new Action<MainInputInteractable>(delegate (MainInputInteractable i)
                {
                    OnTradableSelected(slot, card);
                }));
                slot.HighlightCursorType = CursorType.Pickup;
            }
            yield return new WaitWhile(delegate ()
            {
                bool result;
                if (PeltInHand())
                {
                    result = (Singleton<BoardManager>.Instance.OpponentSlotsCopy.Exists((CardSlot x) => x.Card != null) || Singleton<TurnManager>.Instance.Opponent.Queue.Count > 0);
                }
                else
                {
                    result = false;
                }
                return result;
            });
            foreach (CardSlot slot2 in Singleton<BoardManager>.Instance.OpponentSlotsCopy)
            {
                slot2.ClearDelegates();
                slot2.HighlightCursorType = CursorType.Default;
            }
            foreach (HighlightedInteractable slot3 in Singleton<BoardManager>.Instance.OpponentQueueSlots)
            {
                slot3.ClearDelegates();
                slot3.HighlightCursorType = CursorType.Default;
            }
            yield return new WaitForSeconds(0.75f);
            Singleton<ViewManager>.Instance.SwitchToView(View.Default, false, false);
            yield break;
        }

        public static IEnumerator Wind()
        {
            yield return PlaySoundAndWait("wind");
            foreach (CardSlot slot in Singleton<BoardManager>.Instance.PlayerSlotsCopy)
            {
                bool flag = slot.Card != null;
                if (flag)
                {
                    slot.Card.ExitBoard(0.4f, Vector3.zero);
                    yield return new WaitForSeconds(0.1f);
                }
            }
            yield break;
        }

        public static IEnumerator Moon()
        {
            yield return PlaySoundAndWait("moon");
            yield return Singleton<TurnManager>.Instance.Opponent.ClearBoard();
            yield return Singleton<TurnManager>.Instance.Opponent.ClearQueue();
            Singleton<TurnManager>.Instance.Opponent.TurnPlan.Clear(); 
            yield return Singleton<BoardManager>.Instance.CreateCardInSlot(CardLoader.GetCardByName("!GIANTCARD_MOON"), Singleton<BoardManager>.Instance.OpponentSlotsCopy[0], 0.2f, true);
            yield return new WaitForSeconds(0.2f);
            AudioController.Instance.PlaySound3D("map_slam", MixerGroup.TableObjectsSFX, Singleton<BoardManager>.Instance.transform.position, 1f, 0f, null, null, null, null, false);
            Singleton<BoardManager>.Instance.OpponentSlotsCopy[0].Card.AddTemporaryMod(new CardModificationInfo() { fromTotem = true, abilities = new List<Ability> { Ability.Deathtouch } });
            yield return new WaitForSeconds(1f);
            yield break;
        }

        public static IEnumerator Stink()
        {
            yield return PlaySoundAndWait("stink");
            foreach (CardSlot slot in Singleton<BoardManager>.Instance.OpponentSlotsCopy)
            {
                if(slot.Card != null && slot.Card.HasAbility(Ability.DebuffEnemy))
                {
                    continue;
                }
                if (slot.Card != null)
                {
                    slot.Card.AddTemporaryMod(new CardModificationInfo { abilities = new List<Ability> { Ability.DebuffEnemy }, fromTotem = true });
                    slot.Card.Anim.PlayTransformAnimation();
                }
                else
                {
                    Singleton<BoardManager>.Instance.CreateCardInSlot(CardLoader.GetCardByName("Skunk"), slot, 0.1f, true);
                }
                yield return new WaitForSeconds(0.5f);
            }
            yield break;
        }

        public static IEnumerator Sleep()
        {
            yield return PlaySoundAndWait("sleep"); 
            foreach (CardSlot slot in Singleton<BoardManager>.Instance.PlayerSlotsCopy)
            {
                if (slot.Card == null || slot.Card.TemporaryMods.Exists((x) => x.singletonId == "sleep"))
                {
                    continue;
                }
                if (slot.Card != null)
                {
                    slot.Card.AddTemporaryMod(new CardModificationInfo { attackAdjustment = -999, singletonId = "sleep" });
                    slot.Card.Anim.PlayTransformAnimation();
                }
                yield return new WaitForSeconds(0.5f);
            }
            yield break;
        }

        public static IEnumerator Hole()
        {
            yield return PlaySoundAndWait("hole");
            Singleton<PlayerHand>.Instance.SetCardsInteractionEnabled(false);
            List<PlayableCard> cardsInHand = new List<PlayableCard>(Singleton<PlayerHand>.Instance.CardsInHand);
            foreach(PlayableCard card in cardsInHand)
            {
                card.Anim.PlayDeathAnimation();
                Singleton<PlayerHand>.Instance.RemoveCardFromHand(card);
                yield return new WaitForSeconds(0.25f);
                Destroy(card.gameObject);
            }
            yield break;
        }

        public static IEnumerator Glass()
        {
            yield return PlaySoundAndWait("glass"); 
            Singleton<CombatBell3D>.Instance.OnBellPressed();
            Singleton<TurnManager>.Instance.OnCombatBellRang();
            yield break;
        }

        public static IEnumerator Clock()
        {
            yield return PlaySoundAndWait("clock"); 
            foreach (CardSlot slot in Singleton<BoardManager>.Instance.PlayerSlotsCopy)
            {
                if (slot.Card == null || slot.Card.HasAbility(Ability.BuffEnemy))
                {
                    continue;
                }
                if (slot.Card != null)
                {
                    slot.Card.AddTemporaryMod(new CardModificationInfo { fromTotem = true, abilities = new List<Ability> { Ability.BuffEnemy } });
                    slot.Card.Anim.PlayTransformAnimation();
                }
                yield return new WaitForSeconds(0.5f);
            }
            yield break;
        }

        public static IEnumerator Break()
        {
            yield return PlaySoundAndWait("break");
            foreach (CardSlot slot in Singleton<BoardManager>.Instance.PlayerSlotsCopy)
            {
                if (slot.Card == null || slot.Card.HasAbility(Ability.Brittle))
                {
                    continue;
                }
                if (slot.Card != null)
                {
                    slot.Card.AddTemporaryMod(new CardModificationInfo { fromTotem = true, abilities = new List<Ability> { Ability.Brittle } });
                    slot.Card.Anim.PlayTransformAnimation();
                }
                yield return new WaitForSeconds(0.5f);
            }
            yield break;
        }

        public static IEnumerator Jump()
        {
            yield return PlaySoundAndWait("jump");
            List<CardInfo> infos = new List<CardInfo>();
            foreach (CardSlot slot in Singleton<BoardManager>.Instance.PlayerSlotsCopy)
            {
                bool flag = slot.Card != null;
                if (flag)
                {
                    CardInfo info = CardLoader.Clone(slot.Card.Info);
                    infos.Add(info);
                    slot.Card.ExitBoard(0.4f, Vector3.zero);
                    yield return new WaitForSeconds(0.1f);
                }
            }
            yield return new WaitForSeconds(0.4f);
            Singleton<ViewManager>.Instance.SwitchToView(View.Hand, false, false);
            yield return new WaitForSeconds(0.75f);
            foreach(CardInfo info in infos)
            {
                yield return CardSpawner.Instance.SpawnCardToHand(info, 0.25f);
            }
            yield break;
        }

        public static IEnumerator Starve()
        {
            yield return PlaySoundAndWait("starve");
            yield return CardDrawPiles3D.Instance.ExhaustedSequence();
        }

        public static IEnumerator Cuckoo()
        {
            yield return PlaySoundAndWait("cuckoo");
            foreach (CardSlot slot in Singleton<BoardManager>.Instance.PlayerSlotsCopy)
            {
                if (slot.Card != null)
                {
                    continue;
                }
                yield return Singleton<BoardManager>.Instance.CreateCardInSlot(CardLoader.GetCardByName("BrokenEgg"), slot, 0.15f, true);
                if(slot.opposingSlot != null && slot.opposingSlot.Card != null && !slot.opposingSlot.Card.HasAbility(Ability.Flying))
                {
                    slot.opposingSlot.Card.AddTemporaryMod(new CardModificationInfo { fromTotem = true, abilities = new List<Ability> { Ability.Flying } });
                    slot.opposingSlot.Card.Anim.PlayTransformAnimation();
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        public static IEnumerator Fire()
        {
            yield return PlaySoundAndWait("fire");
            AscensionStatsData.TryIncrementStat(AscensionStat.Type.Misplays);
            Singleton<InteractionCursor>.Instance.InteractionDisabled = true;
            bool flag2 = Singleton<CandleHolder>.Instance != null;
            if (flag2)
            {
                yield return Singleton<CandleHolder>.Instance.BlowOutCandleSequence(false);
            }
            yield return new WaitForSeconds(1f);
            bool flag5 = RunState.Run.playerLives <= 0;
            if (flag5)
            {
                RunState.Run.causeOfDeath = new RunState.CauseOfDeath(Opponent.Type.Default);
                AudioController.Instance.FadeOutLoop(3f, Array.Empty<int>());
                yield return new WaitForSeconds(0.5f);
                yield return TurnManager.Instance.Opponent.DefeatedPlayerSequence();
                Singleton<ViewManager>.Instance.SwitchToView(View.Default, false, false);
                yield return Singleton<Part1GameFlowManager>.Instance.KillPlayerSequence();
            }
            else
            {
                Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Unlocked;
                Singleton<ViewManager>.Instance.SwitchToView(View.Default, false, false);
            }
            Singleton<InteractionCursor>.Instance.InteractionDisabled = false;
            yield break;
        }
        
        public static IEnumerator Tooth()
        {
            yield return PlaySoundAndWait("dam");
            yield return Singleton<LifeManager>.Instance.ShowDamageSequence(3, 3, true, 0.125f, null, 0f, true);
            yield break;
        }

        public static IEnumerator Boom()
        {
            yield return PlaySoundAndWait("boom");
            List<CardSlot> s = BoardManager.Instance.PlayerSlotsCopy.FindAll((x) => x.Card != null);
            if(s.Count > 0)
            {
                CardSlot random = s[UnityEngine.Random.Range(0, s.Count)];
                random.Card.AddTemporaryMod(new CardModificationInfo { abilities = new List<Ability> { Ability.ExplodeOnDeath }, fromTotem = true });
                random.Card.Anim.PlayTransformAnimation();
                yield return new WaitForSeconds(0.5f);
                yield return random.Card.Die(false, null, true);
                yield return new WaitForSeconds(0.25f);
            }
            yield break;
        }

        public static IEnumerator Money()
        {
            yield return PlaySoundAndWait("money");
            RunState.Run.currency = 0;
            yield break;
        }

        public static IEnumerator Bone()
        {
            yield return PlaySoundAndWait("bone");
            yield return ResourcesManager.Instance.SpendBones(ResourcesManager.Instance.PlayerBones);
            yield break;
        }

        public static IEnumerator Shape()
        {
            yield return PlaySoundAndWait("shape");
            foreach (CardSlot slot in Singleton<BoardManager>.Instance.OpponentSlotsCopy)
            {
                if (slot.Card == null)
                {
                    continue;
                }
                if (slot.Card != null)
                {
                    yield return slot.Card.TransformIntoCard(CardLoader.GetCardByName("Ijiraq"));
                }
            }
            yield break;
        }

        public static IEnumerator Moth()
        {
            yield return PlaySoundAndWait("man");
            List<CardSlot> s = BoardManager.Instance.OpponentSlotsCopy.FindAll((x) => x.Card == null);
            if (s.Count > 0)
            {
                CardSlot random = s[UnityEngine.Random.Range(0, s.Count)];
                yield return BoardManager.Instance.CreateCardInSlot(CardLoader.GetCardByName("Mothman_Stage3"), random, 0.1f, true);
                random.Card.AddTemporaryMod(new CardModificationInfo { abilities = new List<Ability> { Ability.TriStrike, Ability.DoubleStrike }, fromTotem = true });
            }
            yield break;
        }

        private static bool CardHasNoAbilities(PlayableCard card)
        {
            return !card.TemporaryMods.Exists((CardModificationInfo t) => t.abilities.Count > 0) && card.Info.Abilities.Count <= 0;
        }

        private static void SpawnSplatter(PlayableCard card)
        {
            GameObject gameObject = Instantiate(Resources.Load<GameObject>("Prefabs/Items/BleachSplatter"));
            gameObject.transform.position = card.transform.position + new Vector3(0f, 0.1f, -0.25f);
            Destroy(gameObject, 5f);
        }

        public static IEnumerator Bleach()
        {
            yield return PlaySoundAndWait("bleach");
            List<CardSlot> validSlots = Singleton<BoardManager>.Instance.PlayerSlotsCopy;
            validSlots.RemoveAll((CardSlot x) => x.Card == null || CardHasNoAbilities(x.Card));
            foreach (CardSlot slot in BoardManager.Instance.PlayerSlotsCopy)
            {
                bool flag = validSlots.Contains(slot);
                if (flag)
                {
                    SpawnSplatter(slot.Card);
                    bool faceDown = slot.Card.FaceDown;
                    if (faceDown)
                    {
                        slot.Card.SetFaceDown(false, true);
                    }
                    slot.Card.Anim.PlayTransformAnimation();
                    CustomCoroutine.WaitThenExecute(0.15f, delegate
                    {
                        CardModificationInfo cardModificationInfo = new CardModificationInfo();
                        cardModificationInfo.negateAbilities = new List<Ability>();
                        foreach (CardModificationInfo cardModificationInfo2 in slot.Card.TemporaryMods)
                        {
                            cardModificationInfo.negateAbilities.AddRange(cardModificationInfo2.abilities);
                        }
                        cardModificationInfo.negateAbilities.AddRange(slot.Card.Info.Abilities);
                        slot.Card.AddTemporaryMod(cardModificationInfo);
                    }, false);
                }
                yield return new WaitForSeconds(0.04166f);
            }
            yield break;
        }

        public static IEnumerator God()
        {
            yield return PlaySoundAndWait("god");
            for(int i = 0; i < 2; i++)
            {
                List<CardSlot> s = BoardManager.Instance.OpponentSlotsCopy.FindAll((x) => x.Card == null);
                if (s.Count > 0)
                {
                    CardSlot random = s[UnityEngine.Random.Range(0, s.Count)];
                    yield return BoardManager.Instance.CreateCardInSlot(CardLoader.GetCardByName("MantisGod"), random, 0.1f, true);
                    random.Card.AddTemporaryMod(new CardModificationInfo { abilities = new List<Ability> { Ability.Deathtouch, Ability.GainAttackOnKill }, fromTotem = true, healthAdjustment = 2 });
                }
            }
            yield break;
        }

        public static PhraseRecognizer recognizer;
        public static DictationRecognizer drecognizer;
        public static AssetBundle bundle;
        public static Coroutine consoleClearCR;
        public static Dictionary<string[], Func<IEnumerator>> commands = new Dictionary<string[], Func<IEnumerator>>
        {
            {new string[] { "bear" }, Bear},
            {new string[] { "gold" }, Gold},
            {new string[] { "bait", "fish" }, Fish},
            {new string[] { "wind" }, Wind},
            {new string[] { "trade" }, Trade},
            {new string[] { "moon" }, Moon},
            {new string[] { "stink" }, Stink},
            {new string[] { "sleep" }, Sleep},
            {new string[] { "hole" }, Hole},
            {new string[] { "glass", "hour" }, Glass},
            {new string[] { "clock", "alarm" }, Clock},
            {new string[] { "break" }, Break},
            {new string[] { "jump" }, Jump},
            {new string[] { "starv", "hung", "famine" }, Starve},
            {new string[] { "cuckoo", "egg" }, Cuckoo},
            {new string[] { "fire", "flame" }, Fire},
            {new string[] { "dam", "tooth" }, Tooth},
            {new string[] { "boom", "bomb" }, Boom},
            {new string[] { "money", "currency" }, Money},
            {new string[] { "bone" }, Bone},
            {new string[] { "shape", "form" }, Shape},
            {new string[] { "man", "moth" }, Moth},
            {new string[] { "bleach" }, Bleach},
            {new string[] { "god", "mantis" }, God}
        };
    }
}
