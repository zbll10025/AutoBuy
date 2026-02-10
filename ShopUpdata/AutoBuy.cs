using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;
using YKF;
using static ActPlan;
using UDebug = UnityEngine.Debug;
namespace AutoBuy
{
    [BepInPlugin("me.acc.plugin.AutoBuy", "AutoBuy", "1.0.0")]
    public class AutoBuy:BaseUnityPlugin
    {
        public static ConfigLayer layer;

        public static List<Thing> matchThings = new List<Thing>();
        public static UIInventory uIInventory;

        public static ConfigData configeData;
        static ConfigEntry<KeyCode> uiKeyCode;
        public static string pasueMessage = "未启动自动购买";
        public static bool pause = true;
        public static int count = 0;
        public static int rerollNum = 0;
        public static Stopwatch sw = null;
        public static int tempCount = 0;
        public enum FilterMdoe
        {
            Default,
            Name,
            Detail,
            Tags,
            Element,
            StockNum,
        }
        void Start()
        {
            Logger.LogInfo("hello AutoBuy");
            Harmony.CreateAndPatchAll(typeof(AutoBuy));
            configeData = JsonHelper.Load<ConfigData>(new ConfigData() ,"AutoBuy.json");
            uiKeyCode = Config.Bind<KeyCode>("config", "UIKeyCode", KeyCode.P, "模组设置界面的按键/Mod Configuration Buttons");
            //PoolHelp.PreLoadPlanLayout(100);
        }
        void Update()
        {
            if (Input.GetKeyDown(uiKeyCode.Value))
            {
               ConfigLayer.CreateLayer();
            }

            //if (Input.GetKeyDown(KeyCode.O))
            //{
            //    PoolHelp.PreLoadPlanLayout(100);
            //}
        }
        private void OnApplicationQuit()
        {
            JsonHelper.Save<ConfigData>(configeData, "AutoBuy.json");
        }
        [HarmonyPostfix, HarmonyPatch(typeof(InvOwner), "OnRightClick", new Type[]{
            typeof( ButtonGrid )
        })]
        public static void InvOwner_OnRightClick_Post(ButtonGrid button)
        {
            // ItemInfo info =  GetThingInfo(button.card as Thing);
            //info.DeBugLog();

        }

        [HarmonyPostfix, HarmonyPatch(typeof(LayerInventory), "CreateBuy", new Type[]{
            typeof( Card ),
            typeof(CurrencyType),
            typeof(PriceType)
        })]
        public static void LayerInventory_CreateBuy_Post(LayerInventory __result)
        {
            UDebug.Log("商店打开成功");
            if (__result.invs.Count > 0)
            {
                 uIInventory = __result.invs[0];
            }
            
        }

        [HarmonyPostfix, HarmonyPatch(typeof(LayerInventory), "TryShowGuide", new Type[]{
            typeof(List<ButtonGrid>)
        })]
        public static void LayerInventory_TryShowGuide_Postfix(List<ButtonGrid> list)
        {
            if (!pause) { return; }
            if(uIInventory == null) { return; }
            var things = uIInventory.owner.owner.things.Find("chest_merchant").things;
            if (things == null || things.Count <= 0) {  return; }
            matchThings.Clear();
            SearchItems(things);
            foreach (ButtonGrid button in list)
            {
                Thing thing = button.card as Thing;
                if (thing == null) continue;

                if (matchThings.Contains(thing))
                {
                    if (configeData.isGuide)
                    {
                        button.Attach("guide", rightAttach: false);
                    }

                }

            }
            return;
        }
        [HarmonyPostfix, HarmonyPatch(typeof(UIInventory), "RefreshMenu")]
        public static void UIInventory_RefreshMenu_Postfix(UIInventory __instance)
        {
            UIInventory.Mode mode = __instance.currentTab.mode;
            WindowMenu menuBottom = __instance.window.menuBottom;
            bool flag = mode == UIInventory.Mode.Buy;
            if (flag)
            {
                menuBottom.AddButton("Auto Buy", delegate { StartAuto(); }, null, "Default");
            }
        }

        #region 提取物体词条
        public static ItemInfo GetThingInfo(Thing t)
        {
            Card c = t as Card;
            if (t.sourceCard._origin == "dish")
            {
                t.CheckJustCooked();
            }
            string text3 = "";
            TraitAbility traitAbility = t.trait as TraitAbility;
            bool showEQStats = c.IsEquipmentOrRangedOrAmmo;
            bool flag = false;
            //bool flag2 = (t as Card).IsIdentified || flag ;
            bool flag2 = true;
            text3 = c.Name;
            string nameSimpleText = c.NameSimple;
            string nameText = text3;
            //数量与重量
            string text5 = c.Num.ToFormat() ?? "";
            string text6 = (Mathf.Ceil(0.01f * (float)c.ChildrenAndSelfWeight) * 0.1f).ToString("F1") + "s";
            if (t.things.Count > 0)
            {
                text5 = text5 + " (" + t.things.Count + ")";
            }
            if (c.ChildrenAndSelfWeight != t.SelfWeight)
            {
                text6 = text6 + " (" + (Mathf.Ceil(0.01f * (float)t.SelfWeight) * 0.1f).ToString("F1") + "s)";
            }
            string num_weight_Text = "_quantity".lang(text5 ?? "", text6);
            if (showEQStats)
            {
                //伤害、防御、闪避、命中、贯穿等文本
                text3 = "";
                if (t.DV != 0 || t.PV != 0 || c.HIT != 0 || c.DMG != 0 || t.Penetration != 0)
                {
                    if (c.DMG != 0)
                    {
                        text3 = text3 + "DMG".lang() + ((c.DMG > 0) ? "+" : "") + c.DMG + ", ";
                    }
                    if (c.HIT != 0)
                    {
                        text3 = text3 + "HIT".lang() + ((c.HIT > 0) ? "+" : "") + c.HIT + ", ";
                    }
                    if (t.DV != 0)
                    {
                        text3 = text3 + "DV".lang() + ((t.DV > 0) ? "+" : "") + t.DV + ", ";
                    }
                    if (t.PV != 0)
                    {
                        text3 = text3 + "PV".lang() + ((t.PV > 0) ? "+" : "") + t.PV + ", ";
                    }
                    if (t.Penetration != 0)
                    {
                        text3 = text3 + "PEN".lang() + ((t.Penetration > 0) ? "+" : "") + t.Penetration + "%, ";
                    }
                    string basicValuesText = text3.TrimEnd(' ').TrimEnd(',');
                }
                if (t.trait is TraitToolRange traitToolRange)
                {
                    string rangeText = "tip_range".lang(traitToolRange.BestDist.ToString() ?? "");
                }
            }
            else
            {
                // 非装备：显示额外信息，如品质/营养/硬度/最大品质
                string text7 = "";
                if (EClass.debug.showExtra)
                {
                    int totalQuality = t.GetTotalQuality();
                    int totalQuality2 = t.GetTotalQuality(applyBonus: false);
                    text7 += "Lv. " + c.LV + " TQ. " + t.GetTotalQuality()
                          + ((totalQuality == totalQuality2) ? "" : (" (" + totalQuality2 + ")"));
                }
                if (t.HasElement(10)) // 食物营养
                {
                    text7 += (text7.IsEmpty() ? "" : "  ") + "_nutrition".lang(t.Evalue(10).ToFormat() ?? "");
                }
                if ((c.category.IsChildOf("throw") || c.category.IsChildOf("resource") || t.trait.IsTool) && !(t.trait is TraitAbility))
                {
                    text7 += (text7.IsEmpty() ? "" : "  ") + "_hardness".lang(c.material.hardness.ToString() ?? "");
                }
                //if (flag && recipe != null && (bool)LayerCraft.Instance)
                //{
                //    text7 += (text7.IsEmpty() ? "" : "  ") + "_max_quality".lang(recipe.GetQualityBonus().ToString() ?? "");
                //}
                string eShow = text7;
               // UDebug.Log(eShow);
            }


            //具体描述
            string detailText = t.GetDetail();

            if (t.trait is TraitBookPlan)
            {
                TraitBookPlan traitBookPlan = t.trait as TraitBookPlan;
                 detailText =  traitBookPlan.source.GetDetail();

            }
            //UDebug.Log("detailText" + detailText);

            //物品标签
            string tagText = "";
            tagText+="isMadeOf".lang(c.material.GetText(), c.material.hardness.ToString() ?? "");
            tagText = AddTagText(tagText, "isCategorized".lang(c.category.GetText()));
            if (c.IsContainer)
            {
                tagText = AddTagText(tagText, "isContainer".lang(t.things.MaxCapacity.ToString() ?? ""));
            }
            if (c.c_lockLv != 0)
            {
                tagText = AddTagText(tagText, (c.c_lockedHard ? "isLockedHard" : "isLocked").lang(c.c_lockLv.ToString() ?? ""));
            }
            if (c.isCrafted)
            {
                tagText = AddTagText(tagText, "isCrafted".lang());
            }
            if (t.trait.Decay > 0)
            {
                string text8 = "";
                text8 = (c.IsDecayed ? "isRotten" : (c.IsRotting ? "isRotting" : ((!c.IsFresn) ? "isNotFresh" : "isFresh")));
                tagText = AddTagText(tagText, text8.lang());
            }
            if (c.isDyed)
            {
                tagText = AddTagText(tagText, "isDyed".lang(c.DyeMat.GetName() ?? ""));
            }
            if (c.IsEquipment)
            {
                tagText = AddTagText(tagText, "isEquipable".lang(Element.Get(c.category.slot).GetText()));
            }
            if (c.isFireproof)
            {
                tagText = AddTagText(tagText, "isFreproof".lang());
            }
            if (c.isAcidproof)
            {
                tagText = AddTagText(tagText, "isAcidproof".lang());
            }
            if (t.trait.Electricity > 0)
            {
                tagText = AddTagText(tagText, "isGenerateElectricity".lang(t.trait.Electricity.ToString() ?? ""));
            }
            if (t.trait.Electricity < 0)
            {
                tagText = AddTagText(tagText, "isConsumeElectricity".lang(Mathf.Abs(t.trait.Electricity).ToString() ?? ""));
            }
            if (c.IsUnique)
            {
                tagText = AddTagText(tagText, "isPrecious".lang());
            }
            if (c.isCopy)
            {
                tagText = AddTagText(tagText, "isCopy".lang());
            }
            if (flag && t.HasTag(CTAG.noMix))
            {
                tagText = AddTagText(tagText, "isNoMix".lang());
            }
            if (!t.trait.CanBeDestroyed)
            {
                tagText = AddTagText(tagText, "isIndestructable".lang());
            }
            if (t.GetInt(107) > 0)
            {
                tagText = AddTagText(tagText, "isLicked".lang());
            }
            if (t.HasRune())
            {
                tagText = AddTagText(tagText, "isRuneAdded".lang());
            }
            if (!c.c_idDeity.IsEmpty())
            {
                Religion religion = EClass.game.religions.Find(c.c_idDeity) ?? EClass.game.religions.Eyth;
                tagText = AddTagText(tagText, "isDeity".lang(religion.Name));
            }
            if (c.isGifted && t.GetRoot() != EClass.pc)
            {
                tagText = AddTagText(tagText, "isGifted".lang());
            }
            if (c.isNPCProperty)
            {
                tagText = AddTagText(tagText, "isNPCProperty".lang());
            }
            if (c.c_priceFix != 0)
            {
                tagText = AddTagText(tagText, ((c.c_priceFix > 0) ? "isPriceUp" : "isPriceDown").lang(Mathf.Abs(c.c_priceFix).ToString() ?? ""));
            }
            if (c.noSell)
            {
                tagText = AddTagText(tagText, "isNoSell".lang());
            }
            if (t.trait.IsOnlyUsableByPc)
            {
                tagText = AddTagText(tagText, "isOnlyUsableByPC".lang());
            }
            if (c.isStolen)
            {
                tagText = AddTagText(tagText, "isStolen".lang());
            }
            if (c.c_isImportant)
            {
                tagText = AddTagText(tagText, "isMarkedImportant".lang());
            }
            if (t.GetInt(25) != 0)
            {
                tagText = AddTagText(tagText, "isDangerLv".lang((t.GetInt(25) + 1).ToString() ?? "", (EClass.pc.FameLv + 10).ToString() ?? ""));
            }
            //特殊的标签
            if (t.trait is TraitTool && !(t.trait is TraitToolRange))
            {
                if (t.HasElement(220))
                {
                   tagText= AddTagText(tagText,"canMine".lang());
                }
                if (t.HasElement(225))
                {
                    tagText = AddTagText(tagText,"canLumberjack".lang());
                    tagText = AddTagText(tagText, "canLumberjack2".lang());
                }
                if (t.HasElement(230))
                {
                    tagText = AddTagText(tagText,"canDig".lang());
                }
                if (t.HasElement(286))
                {
                    tagText = AddTagText(tagText, "canFarm".lang());
                }
                if (t.HasElement(245))
                {
                    tagText = AddTagText(tagText, "canFish".lang());
                }
                if (t.HasElement(237))
                {
                    tagText = AddTagText(tagText, "canTame".lang());
                }
            }
            if (t.trait is TraitToolMusic)
            {
                tagText = AddTagText(tagText, "canPlayMusic".lang());
            }
            if (Lang.Has("hint_" + t.trait.ToString()))
            {
                tagText = AddTagText(tagText,("hint_" + t.trait.ToString()).lang());
            }
            if (Lang.Has("hint_" + t.trait.ToString() + "2"))
            {
                tagText = AddTagText(tagText, ("hint_" + t.trait.ToString() + "2").lang());
            }
            if (t.HasTag(CTAG.tourism))
            {
                tagText = AddTagText(tagText, "isTourism".lang());
            }
            string langPlaceType = c.TileType.LangPlaceType;
            if (langPlaceType == "place_Door" || langPlaceType == "place_WallMount")
            {
                tagText = AddTagText(tagText,c.TileType.LangPlaceType + "_hint".lang());
            }
            if (t.trait.IsHomeItem)
            {
                tagText = AddTagText(tagText,"isHomeItem".lang());
            }
            if (t.HasTag(CTAG.throwWeapon))
            {
                tagText = AddTagText(tagText,"isThrowWeapon".lang());
            }
            if (EClass.debug.showExtra && t.HasTag(CTAG.throwWeaponEnemy))
            {
                tagText = AddTagText(tagText,"isThrowWeaponEnemy".lang());
            }
            if (t.trait is TraitFoodFishSlice)
            {
                tagText = AddTagText(tagText,"isNoProcessIng".lang());
            }
            if (t.HasElement(10))
            {
                tagText = AddTagText(tagText,"isEdible".lang());
            }
            if (FoodEffect.IsLeftoverable(t))
            {
                tagText = AddTagText(tagText,"isLeftoverable".lang());
            }
            if (t.HasTag(CTAG.rareResource))
            {
                tagText = AddTagText(tagText,"isRareResource".lang());
            }
            if (t.trait is TraitBed traitBed)
            {
                tagText = AddTagText(tagText,"isBed".lang(traitBed.MaxHolders.ToString() ?? ""));
            }
           // UDebug.Log(tagText);

            string elementsText = "";
            bool flag3 = c.IsEquipmentOrRangedOrAmmo || c.IsThrownWeapon || t.trait is TraitToolMusic;
            bool showTraits = !flag3 || c.ShowFoodEnc;
            bool infoMode = true;
            List<Element> listTrait = t.ListValidTraits(isCraft: false, !infoMode);
            List<Element> list = t.ListValidTraits(isCraft: false, limit: false);
            if (list.Count - listTrait.Count <= 1)
            {
                listTrait = list;
            }
            string stockNum = "";
            string geneElemntText = "";
            if (flag2)
            {
                //是武器装备的附魔文本
                Element element = t.elements.GetElement(653);
                if (element != null)
                {
                    string pvdv = "isAlive".lang(element.vBase.ToString() ?? "", (element.vExp / 10).ToString() ?? "", (element.ExpToNext / 10).ToString() ?? "");
                }
                if (flag3)
                {
                    string[] rangedSubCats = new string[2] { "eleConvert", "eleAttack" };
                   elementsText= AddNote(t,delegate(Element e)
                    {
                        if (t.trait is TraitToolRange && c.category.slot == 0 && !(e is Ability) && !rangedSubCats.Contains(e.source.categorySub) && !e.HasTag("modRanged"))
                        {
                            return false;
                        }
                        if (e.IsTrait || (showTraits && listTrait.Contains(e)))
                        {
                            return false;
                        }
                        if (e.source.categorySub == "eleAttack" && !c.IsWeapon && !c.IsRangedWeapon && !c.IsAmmo && !c.IsThrownWeapon && !(t.trait is TraitToolMusic))
                        {
                            return false;
                        }
                        return (!showEQStats || (e.id != 64 && e.id != 65 && e.id != 66 && e.id != 67)) ? true : false;
                    },null, ElementContainer.NoteMode.Default, addRaceFeat: false,delegate(Element e,string s)
                    {
                        //if (mode != IInspect.NoteMode.Info)
                        //{
                        //    return s;
                        //}
                        Card root = t.GetRootCard();
                        //int num4 = e.Value; 
                        int num3 = e.Value;
                        if (e.source.IsWeaponEnc && !e.source.tag.Contains("modRanged") && t.isEquipped && root.isChara)
                        {
                            int num4 = e.id;
                            if (num4 != 482 && (uint)(num4 - 660) > 2u && num4 != 666)
                            {
                                num3 = num3 * (100 + AttackProcess.GetTwoHandEncBonus(root.Chara, t)) / 100;
                            }
                        }
                        string text16 = " (" + e.Value + ((e.Value == num3) ? "" : (" → " + num3)) + ")";
                        string text17 = "_bracketLeft３".lang() + e.Name + "_bracketRight３".lang();
                        return s + text16 + " " + text17;
                    });
                }
                //远程武器的插件文本
                if (t.sockets != null)
                {
                    stockNum = t.sockets.Count.ToString();
                    foreach (int socket in t.sockets)
                    {
                        string socketText =(socket == 0) ? "emptySocket".lang() : "socket".lang(EClass.sources.elements.map[socket / 1000].GetName(), (socket % 1000).ToString() ?? "");
                        //UDebug.Log("详细插件：" + socketText);
                    }
                }
                //不是武器装备的附魔
               if (showTraits)
                {
                    elementsText= AddNote(t, (Element e) => listTrait.Contains(e), null, ElementContainer.NoteMode.BonusTrait, addRaceFeat: false, delegate (Element e, string s)
                    {
                        string text13 = s;
                        string text14 = e.source.GetText("textExtra");
                        if (!text14.IsEmpty())
                        {
                            string text15 = "";
                            //if (e.id == 2 && mode == IInspect.NoteMode.Product)
                            //{
                            //    int num2 = recipe.GetQualityBonus() / 10;
                            //    if (num2 >= 0)
                            //    {
                            //        num2++;
                            //    }
                            //    text15 = "qualityLimit".lang(num2.ToString() ?? "");
                            //}
                            int num3 = e.Value / 10;
                            num3 = ((e.Value < 0) ? (num3 - 1) : (num3 + 1));
                            text14 = "Lv." + num3 + text15 + " " + text14;
                            if (infoMode && e.IsFoodTraitMain)
                            {
                                text14 += "traitAdditive".lang();
                            }
                            text13 += text14 ;
                        }
                        return text13;
                    });
                }
                    
            }
            if (t.trait is TraitGene)
            {
                geneElemntText = GeneWriteNote(t.trait);
                elementsText += geneElemntText;
            }
            return new ItemInfo(nameText,nameSimpleText ,detailText, tagText,elementsText,stockNum);
        }
        public static string AddNote(
            Thing t, 
            Func<Element, bool> isValid = null, 
            Action onAdd = null,
            ElementContainer.NoteMode mode = ElementContainer.NoteMode.Default,
            bool addRaceFeat = false,
            Func<Element, string, string> funcText = null
            )
        {
            string result = "";
            //bool addRaceFeat = false;
            List<Element> list = new List<Element>();
            foreach (Element value in t.elements.dict.Values)
            {
                if (isValid(value) && (value.ValueWithoutLink != 0) && (value.Value != 0 ) && (!value.HasTag("hidden") || EClass.debug.showExtra))
                {
                    list.Add(value);
                }
            }
            if (addRaceFeat)
            {
                Element element = Element.Create(29, 1);
                element.owner = t.elements;
                list.Add(element);
            }
            if (list.Count == 0)
            {
                return"";
            }
            onAdd?.Invoke();

            switch (mode)
            {
                case ElementContainer.NoteMode.CharaMake:
                case ElementContainer.NoteMode.CharaMakeAttributes:
                    list.Sort((Element a, Element b) => a.GetSortVal(UIList.SortMode.ByElementParent) - b.GetSortVal(UIList.SortMode.ByElementParent));
                    break;
                case ElementContainer.NoteMode.BonusTrait:
                    list.Sort((Element a, Element b) => ElementContainer.GetSortVal(b) - ElementContainer.GetSortVal(a));
                    break;
                default:
                    list.Sort((Element a, Element b) => a.SortVal() - b.SortVal());
                    break;
            }

            foreach (Element item in list)
            {
                 result = result+AddEncNote(t.elements.Card,item,mode,funcText);
            }
            return result;
        }
        public static string AddEncNote(
            Card Card,
            Element e,
            ElementContainer.NoteMode mode = ElementContainer.NoteMode.Default,
            Func<Element, string, string> funcText = null
            )
        {
            string text = "";
            bool flag = e.source.tag.Contains("common");
            string categorySub = e.source.categorySub;
            bool flag2 = false;
            bool flag3 = (e.source.tag.Contains("neg") ? (e.Value > 0) : (e.Value < 0));
            int num = Mathf.Abs(e.Value);
            bool flag4 = Card?.ShowFoodEnc ?? false;
            bool flag5 = Card != null && e is Ability && (Card.IsWeapon || Card.category.slot == 35);
            if (e.IsTrait || (flag4 && e.IsFoodTrait))
            {
                string[] textArray = e.source.GetTextArray("textAlt");
                int num2 = Mathf.Clamp(e.Value / 10 + 1, (e.Value < 0 || textArray.Length <= 2) ? 1 : 2, textArray.Length - 1);
                text = "altEnc".lang(textArray[0].IsEmpty(e.Name), textArray[num2], EClass.debug.showExtra ? (e.Value + " " + e.Name) : "");
                flag3 = num2 <= 1 || textArray.Length <= 2;
                flag2 = true;
            }
            else if (flag5)
            {
                text = "isProc".lang(e.Name);
                flag3 = false;
            }
            else if (categorySub == "resist" || e is Feat)
            {
                text = ("isResist" + (flag3 ? "Neg" : "")).lang(e.Name);
            }
            else if (categorySub == "eleAttack")
            {
                text = "isEleAttack".lang(e.Name);
            }
            else if (!e.source.textPhase.IsEmpty() && e.Value > 0)
            {
                text = e.source.GetText("textPhase");
            }
            else
            {
                string name = e.Name;
                bool flag6 = e.source.category == "skill" || (e.source.category == "attribute" && !e.source.textPhase.IsEmpty());
                bool flag7 = e.source.category == "enchant";
                if (e.source.tag.Contains("multiplier"))
                {
                    flag6 = (flag7 = false);
                    name = EClass.sources.elements.alias[e.source.aliasRef].GetName();
                }
                flag2 = !(flag6 || flag7);
                text = (flag6 ? "textEncSkill" : (flag7 ? "textEncEnc" : "textEnc")).lang(name, num + (e.source.tag.Contains("ratio") ? "%" : ""), ((e.Value > 0) ? "encIncrease" : "encDecrease").lang());
            }
            int num3 = ((!(e is Resistance)) ? 1 : 0);
            int num4 = 5;
            if (e.id == 484)
            {
                num3 = 0;
                num4 = 1;
            }
            if (!flag && !flag2 && !e.source.tag.Contains("flag"))
            {
                text = text + " [" + "*".Repeat(Mathf.Clamp(num * e.source.mtp / num4 + num3, 1, 5)) + ((num * e.source.mtp / num4 + num3 > 5) ? "+" : "") + "]";
            }
            //if (e.HasTag("hidden") && mode != ElementContainer.NoteMode.BonusTrait)
            //{
            //    text = "(debug)" + text;
            //}
           //FontColor color = (flag ? FontColor.Default : (flag3 ? FontColor.Bad : FontColor.Good));
            if (e.IsGlobalElement)
            {
                text = text + " " + (e.IsFactionWideElement ? "_factionWide" : "_partyWide").lang();
                if (!e.IsActive(Card))
                {
                    return"";
                }
            }
            //if (flag4 && IsFoodTrait && !IsFoodTraitMain)
            //{
            //    color = FontColor.FoodMisc;
            //}
            //if (id == 2 && Value >= 0)
            //{
            //    color = FontColor.FoodQuality;
            //}
            //if (funcText != null)
            //{
               // text = funcText(this, text);
            return text = funcText(e,text);
            //UDebug.Log(text + "\n");
            //}
            //UIItem uIItem = n.AddText("NoteText_enc", text, color);
            //Sprite sprite = EClass.core.refs.icons.enc.enc;
            //Thing thing = Card?.Thing;
            //if (thing != null)
            //{
            //    if (thing.material.HasEnc(id))
            //    {
            //        sprite = EClass.core.refs.icons.enc.mat;
            //    }
            //    foreach (int key in thing.source.elementMap.Keys)
            //    {
            //        if (key == id)
            //        {
            //            sprite = EClass.core.refs.icons.enc.card;
            //        }
            //    }
            //    if (thing.ShowFoodEnc && IsFoodTrait)
            //    {
            //        sprite = EClass.core.refs.icons.enc.traitFood;
            //    }
            //    if (id == thing.GetInt(107))
            //    {
            //        sprite = EClass.core.refs.icons.enc.cat;
            //    }
            //    if (thing.GetRuneEnc(id) != null)
            //    {
            //        sprite = EClass.core.refs.icons.enc.rune;
            //    }
            //}
            //if ((bool)sprite)
            //{
            //    uIItem.image1.SetActive(enable: true);
            //    uIItem.image1.sprite = sprite;
            //}
            //uIItem.image2.SetActive(source.IsWeaponEnc || source.IsShieldEnc);
            //uIItem.image2.sprite = (source.IsWeaponEnc ? EClass.core.refs.icons.enc.weaponEnc : EClass.core.refs.icons.enc.shieldEnc);
            //onAddNote?.Invoke(n, this);
            //return;

        }
        public static string AddTagText(string tagText,string r)
        {
            tagText = tagText + r;
            return tagText;
        }
        public static string GeneWriteNote(Trait trait){

            DNA dna = trait.owner.c_DNA;
            SourceChara.Row row = EClass.sources.charas.map.TryGetValue(dna.id);
            string dnaText = "";
            if(dna.type == DNA.Type.Brain)
            {
                if (row != null)
                {
                    string key = row.tactics.IsEmpty(EClass.sources.tactics.map.TryGetValue(row.id)?.id ?? EClass.sources.tactics.map.TryGetValue(row.job)?.id ?? "predator");
                    dnaText = "gene_info".lang(EClass.sources.tactics.map[key].GetName().ToTitleCase(), "");
                    //UDebug.Log(dnaText);
                }

            }
            return dnaText;
        }
        #endregion
        #region 购买物品
        public static void StartAuto()
        {
            count = 0;
            tempCount = 0;
            sw = Stopwatch.StartNew();
            if (uIInventory == null) { return; }
            rerollNum = configeData.rerollNum;
            pause = false;
            CoroutineRunner.StartStaticCoroutine(AutoBuyMainRoutine());
            //AutoBuyMain();
            //LayerInventory.TryShowGuide(uIInventory.list);
        }

        private static IEnumerator AutoBuyMainRoutine()
        {
            while (!pause)
            {
                var things = uIInventory?.owner?.owner?.things.Find("chest_merchant")?.things;
                if (things == null || things.Count <= 0)
                {
                    // 商店空了，尝试刷新
                    if (!RerollShop(uIInventory))
                    {
                        break;
                    }
                    yield return null; // 刷新后等一帧再检查
                    continue;
                }

                // 搜索匹配物品
                matchThings.Clear();                   
                SearchItems(things);

                // 如果有匹配物品，分批购买
                if (matchThings.Count > 0)
                {
                    // 执行分帧购买（等待它完成）
                    yield return CoroutineRunner.StartStaticCoroutine(FramingBuy(matchThings, 35));
                }

                // 购买完成后，刷新商店（如果还有刷新次数）
                if (rerollNum > 0 && !pause)
                {
                    if (!RerollShop(uIInventory))
                    {
                        break;
                    }
                    yield return null; // 刷新后等一帧
                }
                else
                {
                    // 没有刷新次数了，退出
                    break;
                }
            }

            // 循环结束，清理
            sw?.Stop();
            UDebug.Log($"自动购买结束。总耗时: {sw?.ElapsedMilliseconds ?? 0} ms");
            Console.WriteLine($"总购买次数: {tempCount} ");
            Console.WriteLine($"商店刷新次数: {count} ");
            LayerInventory.SetDirtyAll();
            pause = true;
            uIInventory?.RefreshGrid();
            uIInventory?.Sort();
        }
        static IEnumerator FramingBuy(List<Thing> list,int batch)
        {
            int count = 0;
            foreach(Thing t in list)
            {
                if (!FastBuy(t, t.Num))
                {
                    pasueMessage = "购买失败";
                    pause = true;
                    UDebug.Log(pasueMessage);
                    break;

                }
                tempCount++;
                count++;
                if (count >= batch)
                {
                    count = 0;
                    yield return null; // 等待下一帧
                }
            }
        }
        public static void SearchItems(List<Thing> list)
        {        
                foreach (Thing item in list)
                {
                    Thing t = item;
                    if (t == null) continue;
                    ItemInfo itemInfo = GetThingInfo(t);
                    //UDebug.Log(itemInfo.name);
                    foreach (var i in configeData.planList)
                    {
                        if (!i.isActive) {  continue; }
                        switch (i.filterMdoe)
                        {
                            case FilterMdoe.Name:
                                
                                if (IsMatch(i.isAllMatch?itemInfo.nameSimple:itemInfo.name , i.keyword, isall: i.isAllMatch))
                                {
                                AddToMatchThings(item);
                                }
                                break;
                            case FilterMdoe.Detail:
                            if (IsMatch(itemInfo.detail, i.keyword))
                                {
                                AddToMatchThings(item);
                                }
                                break;
                            case FilterMdoe.Tags:
                                if (IsMatch(itemInfo.tags, i.keyword))
                                {
                                    AddToMatchThings(item);
                                }
                                break;
                            case FilterMdoe.Element:
                                if (IsMatch(itemInfo.elements, i.keyword))
                                {
                                    AddToMatchThings(item);
                                }
                                break;
                            case FilterMdoe.StockNum:
                                if (IsMatch(itemInfo.stockNum, i.keyword))
                                {
                                    AddToMatchThings(item);
                                }
                                break;
                            case FilterMdoe.Default:
                                if (IsMatch(itemInfo.allText, i.keyword))
                                {
                                    AddToMatchThings(item);
                                }
                                break;
                    }
                    }

                }
        }
        public static bool IsMatch(string text, string keyword,bool isall = false)
        {

            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
                return false;
           
                if (isall) 
                { 
                    return string.Equals(text, keyword, StringComparison.OrdinalIgnoreCase);
                } 
                else
                {
                    return text.Contains(keyword); 
                }
        }

        public static bool FastBuy(Thing itemToBuy, int maxQuantity = 1)
        {
            if (itemToBuy == null || maxQuantity <= 0)
            {
                UDebug.Log("快速购买失败：物品为空 或 购买数量无效（≤0）");
                return false;
            }

            InvOwner trader = InvOwner.Trader;
            InvOwner player = InvOwner.Main;

            if (trader == null || player == null)
            {
                UDebug.Log("快速购买失败：商人或玩家的背包持有者（InvOwner）为空");
                return false;
            }

            if (itemToBuy.parent != trader.Container)
            {
                UDebug.Log($"快速购买失败：物品 '{itemToBuy.id}' 不属于当前商人的容器（当前父容器：）");
                return false;
            }

            int buyCount = Math.Min(maxQuantity, itemToBuy.Num);
            if (buyCount <= 0)
            {
                UDebug.Log($"快速购买失败：实际购买数量 ≤ 0（请求：{maxQuantity}，物品库存：{itemToBuy.Num}）");
                return false;
            }

            // 获取单价
            int unitPrice = trader.GetPrice(itemToBuy, trader.currency, 1, sell: false);
            if (unitPrice <0)
            {
                UDebug.Log($"快速购买失败：物品 '{itemToBuy.id}' 的单价无效（≤0），价格为：{unitPrice}");
                return false;
            }

            int totalPrice = unitPrice * buyCount;

            // 检查货币是否足够
            int playerCurrency = EClass.pc.GetCurrency(trader.IDCurrency);
            if (playerCurrency < totalPrice)
            {
                UDebug.Log($"快速购买失败：货币不足。需要 {totalPrice}，当前持有 {playerCurrency}（货币类型：{trader.IDCurrency}）");
                return false;
            }

            // ==一次性拆分整批 ===
            Thing batch = itemToBuy.Split(buyCount);
            //Thing batch = BuyHelp.MySplit(buyCount,itemToBuy);
            if (batch == null || batch.isDestroyed || batch.Num != buyCount)
            {
                UDebug.Log($"快速购买失败：Split({buyCount}) 返回的物品无效（为空/已销毁/数量不符）。原物品：{itemToBuy.id}（库存：{itemToBuy.Num}）");
                return false;
            }

            // 尝试让玩家拾取整批
            Thing added = EClass.pc.Pick(batch, msg: false);
            //Thing added = BuyHelp.MyPcPick(batch);

            // 检查是否完整接收
            if (added == null || added.isDestroyed)
            {
                UDebug.Log($"快速购买失败：玩家背包未能完整接收物品。请求数量：{buyCount}，实际接收：{(added?.Num ?? -1)}");

                // 尝试归还物品
                if (batch != null && !batch.isDestroyed)
                {
                    if (batch.parent == null)
                    {
                        trader.Container.AddThing(batch);
                        UDebug.Log("快速购买：已将未接收的物品归还给商人。");
                    }
                    else
                    {
                        UDebug.Log("快速购买：物品已有父容器，跳过归还操作。");
                    }
                }
                return false;
            }

            // 成功拾取，扣款
            EClass.pc.ModCurrency(-totalPrice, trader.IDCurrency);
            //UDebug.Log($"快速购买成功：购买了 {buyCount} 个 '{itemToBuy.id}'，花费 {totalPrice} {trader.IDCurrency}");

            // 触发交易事件
            if (ShopTransaction.current != null)
            {
                //ShopTransaction.current.Process(added, buyCount, sell: false);
            }
            else
            {
                UDebug.Log("快速购买：当前无商店交易上下文（ShopTransaction.current 为 null），跳过事件处理。");
            }

            return true;
        }
        public static bool Buy(ButtonGrid button)
        {
            Card card = button.card;
            Card card2 = uIInventory.owner.owner;
            if (card == null || card2 == null) { return false; }
            return  new InvOwner.Transaction(button, (InvOwner.HasTrader && !InvOwner.FreeTransfer) ? button.card.Num : card.Num).Process(startTransaction: false);
        }
        public static bool RerollShop(UIInventory uIInventory)
        {
            if (uIInventory != null && !pause)
            {
                if (rerollNum > 0)
                {
                    Card _owner = uIInventory.owner.owner;
                    int cost = _owner.trait.CostRerollShop;
                    if (EMono._zone.influence < cost)
                    {
                        SE.Beep();
                        Msg.Say("notEnoughInfluence");
                        pause = true;
                        pasueMessage = "名声不足";
                        return false;
                    }
                    else
                    {
                        count++;
                        rerollNum--;
                        //UDebug.Log("剩余刷新次数" + rerollNum);
                        SE.Dice();
                        EMono._zone.influence -= cost;
                        _owner.c_dateStockExpire = 0;
                        _owner.trait.OnBarter();
                        //uIInventory.RefreshGrid();
                        //uIInventory.Sort();
                        //SE.Play("shop_open");
                        return true;
                    }

                }
                else
                {
                    pause = true;
                    return false;
                }

            }
            else
            {
                pause = true;
                return false;
            }
        }
        public static void AddToMatchThings(Thing b)
        {
            if (!matchThings.Contains(b))
            {
                matchThings.Add(b);
            }
            
        }

        public static List<ButtonGrid> ConvertButtonList(UIList list)
        {
            List<ButtonGrid> list2 = new List<ButtonGrid>();
            foreach (UIList.ButtonPair button in list.buttons)
            {
                ButtonGrid buttonGrid = button.component as ButtonGrid;
                if ((bool)buttonGrid)
                {
                    list2.Add(buttonGrid);
                }
            }
            return list2;
        }
        #endregion
        
        public static void ReBuildUI()
        {
            EMono.ui.RemoveLayer(layer);
            ConfigLayer.CreateLayer();
        }
       
    }
    public class ItemInfo
    {
        public string name;
        public string nameSimple;
        public string detail;
        public string tags;
        public string elements;
        public string allText;
        public string stockNum;
        public ItemInfo(string name,string nameSimple,string detail,string tags,string elements,string stockNum)
        {
            this.name = name;
            this.nameSimple = nameSimple;
            this.detail = detail;
            this.tags = tags;
            this.elements = elements;
            this.allText = name + detail + tags + elements;
            this.stockNum = stockNum;
        }

        public void DeBugLog()
        {
            UDebug.Log("-------------------------------ItemInfo-----------------------------");
            UDebug.Log("name:" + name);
            UDebug.Log("nameSimple:" + nameSimple);
            UDebug.Log("detail:" + detail);
            UDebug.Log("tags:" + tags);
            UDebug.Log("elements:" + elements);
            UDebug.Log("stockNum:" + stockNum);
        }
    }
}
