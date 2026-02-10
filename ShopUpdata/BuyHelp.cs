using B83.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using static BGMData;
using static SourceAsset;

namespace AutoBuy
{   //当前mod没有用以下逻辑
    //将每次购买都会ui刷新去除
    class BuyHelp
    {
        //商店
        public static Thing MySplit(int a, Thing thing)
        {
            if (a == thing.Num)
            {
                return thing.Thing;
            }

            Thing result = thing.Duplicate(a);
            MyModNum(thing, -a, notify: false);
            return result;
        }

        public static void MyModNum(Thing thing, int a, bool notify = true)
        {
            if (thing.Num + a < 0)
            {
                a = -thing.Num;
            }

            thing.Num += a;
            if (thing.props != null)
            {
                thing.props.OnNumChange(thing, a);
            }

            if (thing.parent != null)
            {
                thing.parent.OnChildNumChange(thing);
            }

            if (a > 0 && EClass.core.IsGameStarted && thing.GetRootCard() == EClass.pc && notify)
            {
                thing.NotifyAddThing(thing.Thing, a);
            }

            thing.SetDirtyWeight();
            if (thing.Num <= 0)
            {
                thing.Destroy();
            }
        }
        //玩家
        public static Thing MyPcPick(Thing t,bool tryStack = true)
        {
            //卡片收集相关
            //if (t.trait is TraitCard && t.isNew && EClass.game.config.autoCollectCard && !t.c_idRefCard.IsEmpty())
            //{
            //    ContentCodex.Collect(t);
            //    return t;
            //}

            if (t.parent == EClass.pc)
            {
                return t;
            }

            t = EClass.pc.TryPoisonPotion(t);
            //检查背包是否满了
            ThingContainer.DestData dest = EClass.pc.things.GetDest(t, true);
            if (!dest.IsValid)
            {
                if (t.parent != EClass._zone)
                {
                    if (EClass.pc.IsPC)
                    {
                        EClass.pc.Say("backpack_full_drop", t);
                        SE.Drop();
                    }

                    return EClass._zone.AddCard(t, EClass.pc.pos).Thing;
                }

                if (EClass.pc.IsPC)
                {
                    EClass.pc.Say("backpack_full", t);
                }

                return t;
            }
            //堆叠 or 新增
            if (dest.stack != null)
            {
                //if (msg)
                //{
                //    PlaySound("pick_thing");
                //    Say("pick_thing", this, t);
                //}

                t.TryStackTo(dest.stack);
                return dest.stack;
            }

            //以太病的法杖次数吸收？
            MyTryAbsorbRod(t);
            //if (msg)
            //{
            //    PlaySound("pick_thing");
            //    Say("pick_thing", this, t);
            //}

            //教程关卡的特殊处理
            //TryReservePickupTutorial(t);
            //return dest.container.AddThing(t, tryStack);
            return MyAddThing(dest.container, t, tryStack);
        }
        public static void MyTryAbsorbRod(Thing t)
        {
            if (!EClass.pc.IsPC || !(t.trait is TraitRod) || t.c_charges <= 0 || !EClass.pc.HasElement(1564))
            {
                return;
            }

            //Say("absorbRod", this, t);
            TraitRod rod = t.trait as TraitRod;
            bool flag = false;
            if (rod.source != null)
            {
                if (rod.source != null)
                {
                    using (IEnumerator<SourceElement.Row> enumerator = EClass.sources.elements.rows
                        .Where((SourceElement.Row a) => a.id == rod.source.id)
                        .GetEnumerator())
                    {
                        if (enumerator.MoveNext())
                        {
                            SourceElement.Row current = enumerator.Current;
                            if (EClass.pc.IsPC)
                            {
                                EClass.pc.GainAbility(current.id, t.c_charges * 100, t);
                                flag = true;
                            }
                        }
                    } // Dispose() 会在这里自动调用
                }
            }

            if (!flag)
            {
                EClass.pc.mana.Mod(-50 * t.c_charges);
            }

            t.c_charges = 0;
            //LayerInventory.SetDirty(t);
        }

        public static Thing MyAddThing(Card container, Thing t, bool tryStack = true, int destInvX = -1, int destInvY = -1)
        {
            if (t.Num == 0 || t.isDestroyed)
            {
                Debug.LogWarning("tried to add destroyed thing:" + t.Num + "/" + t.isDestroyed + "/" + t?.ToString() + "/" + container);
                return t;
            }

            if (t.parent == container)
            {
                Debug.LogWarning("already child:" + t);
                return t;
            }

            if (container.things.Contains(t))
            {
                Debug.Log("already in the list" + t);
                return t;
            }

            _ = t.parent;
            _ = EClass._zone;
            bool flag = container.IsPC && t.GetRootCard() == EClass.pc;
            if (t.parent != null)
            {
                //t.parent.RemoveCard(t);
                MyRemoveCard(t.parent as Card, t);
            }
            t.isMasked = false;
            t.ignoreAutoPick = false;
            t.parent = container;
            t.noShadow = false;
            t.isSale = false;
            if (t.IsContainer)
            {
                t.RemoveEditorTag(EditorTag.PreciousContainer);
            }
            //放置的位置，默认放在非快捷栏
            t.invX = -1;
            if (destInvY == -1)
            {
                t.invY = 0;
            }
            //神器相关
            //if (t.IsUnique && t.HasTag(CTAG.godArtifact) && t.GetRootCard() is Chara { IsPCFactionOrMinion: not false })
            if (t.IsUnique && t.HasTag(CTAG.godArtifact) && t.GetRootCard() is Chara chara && chara.IsPCFactionOrMinion)
            {
                container.PurgeDuplicateArtifact(t);
            }
            Thing thing = (tryStack ? container.things.TryStack(t, destInvX, destInvY) : t);
            if (t == thing)
            {
                container.things.Add(t);
                container.things.OnAdd(t);
            }
            //通知？
            //if (thing == t && IsPC && EClass.core.IsGameStarted && EClass._map != null && parent == EClass.game.activeZone && pos.IsValid && !flag)
            //{
            //    NotifyAddThing(t, t.Num);
            //}

            if (t == thing && container.isThing && container.parent == EClass._zone && container.placeState != 0)
            {
                EClass._map.Stocked.Add(t); // 用于建筑库存、工作台原料追踪等
            }
            //刷新背包UI
            //SetDirtyWeight();
            //if (ShouldTrySetDirtyInventory())
            //{
            //    EClass.pc.SetDirtyWeight();
            //    LayerInventory.SetDirty(thing); // 👈 这就是刷新背包/商店 UI 的地方！
            //}
            if (container.IsPC)
            {
                goto IL_029f;
            }

            if (container.IsContainer)
            {
                Card rootCard = container.GetRootCard();
                if (rootCard != null && rootCard.IsPC)
                {
                    goto IL_029f;
                }
            }

            goto IL_0345;
        IL_0345:
            return thing;
        IL_029f:
            t.isNPCProperty = false;
            t.isGifted = false;
            int count = 0;
            HashSet<string> ings = EClass.player.recipes.knownIngredients;
            TryAdd(t);
            if (t.CanSearchContents)
            {
                t.things.Foreach(delegate (Thing _t)
                {
                    TryAdd(_t);
                });
            }

            if (count > 0 && EClass.core.IsGameStarted)
            {
                Msg.Say((count == 1) ? "newIng" : "newIngs", count.ToString() ?? "");
            }

            goto IL_0345;
            void TryAdd(Thing a)
            {
                if (!ings.Contains(a.id))
                {
                    ings.Add(a.id);
                    count++;
                    if (a.sourceCard.origin != null && !ings.Contains(a.sourceCard.origin.id))
                    {
                        ings.Add(a.sourceCard.origin.id);
                    }
                }
            }
        }
        public static void MyRemoveCard(Card container, Thing thing)
        {
            //手持物品的特殊处理
            //Card rootCard = GetRootCard();
            //if (rootCard != null && rootCard.isChara && (rootCard.Chara.held == thing || (rootCard.IsPC && thing.things.Find((Thing t) => EClass.pc.held == t) != null)))
            //{
            //    rootCard.Chara.held = null;
            //    if (rootCard.IsPC)
            //    {
            //        WidgetCurrentTool instance = WidgetCurrentTool.Instance;
            //        if ((bool)instance && instance.selected != -1 && instance.selectedButton.card != null && instance.selectedButton.card == thing)
            //        {
            //            instance.selectedButton.card = null;
            //        }

            //        EClass.player.RefreshCurrentHotItem();
            //        ActionMode.AdvOrRegion.updatePlans = true;
            //        LayerInventory.SetDirty(thing);
            //    }

            //    RecalculateFOV();
            //}

            //dirtyWeight = true;
            //if (thing.c_equippedSlot != 0 && isChara)
            //{
            //    Chara.body.Unequip(thing);
            //}

            container.things.Remove(thing);
            container.things.OnRemove(thing);
            if (container.isSale && container.things.Count == 0 && container.IsContainer)
            {
                container.isSale = false;
                EClass._map.props.sales.Remove(container);
            }

            //if (thing.invY == 1)
            //{
            //    WidgetCurrentTool.dirty = true;
            //}

            //thing.invX = -1;
            //thing.invY = 0;
            //if (thing.props != null)
            //{
            //    thing.props.Remove(thing);
            //}

            //SetDirtyWeight();
            //if (ShouldTrySetDirtyInventory())
            //{
            //    LayerInventory.SetDirty(thing);
            //    WidgetHotbar.dirtyCurrentItem = true;
            //    thing.parent = null;
            //    if (thing.trait.IsContainer)
            //    {
            //        foreach (LayerInventory item in LayerInventory.listInv.Copy())
            //        {
            //            if (item.invs[0].owner.Container.GetRootCard() != EClass.pc && item.floatInv)
            //            {
            //                EClass.ui.layerFloat.RemoveLayer(item);
            //            }
            //        }
            //    }
            //}

            thing.parent = null;
        }

    }
}
