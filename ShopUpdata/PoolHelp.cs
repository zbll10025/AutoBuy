using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using YKF;
using static BGMData;
using static PoolManager;
using UDebug = UnityEngine.Debug;
namespace AutoBuy
{
    public class PoolHelp
    {
        public static void PreLoadPlanLayout(int count)
        {
            ////临时挂载
            //Transform temp = new GameObject().GetComponent<Transform>();
            ////创建源对象
            //CustomTab tab = new CustomTab();
            //PlanLayout ho = YK.Create<PlanLayout>(temp);
            //ho.name = "planLayout";
            //ho.SetId(-99);
            //tab.IsActiveItem(ho);
            //ho.Spacer(1, 35);
            //tab.keyWordItem(ho);
            //ho.Spacer(1, 20);
            //tab.FilterMdoeItem(ho);
            //ho.Spacer(1, 40);
            //ho.allMatchObject = tab.IsAllMatchItem(ho).gameObject;
            //ho.spacerObject = ho.Spacer(1, 60).gameObject;
            //ho.Spacer(1, 40);
            //tab.RemoveItem(ho);
            //ho.RefreshState(ho.isShowIsAllMatch);

            //临时挂载
            Transform temp = new GameObject().GetComponent<Transform>();
            //创建源对象
            CustomTab tab = temp.gameObject.AddComponent<CustomTab>();
            PlanLayout ho = YK.Create<PlanLayout>(tab.transform);
            ho.OnLayout();
            ho.name = "planLayout";
            ho.SetId(-99);
            PoolHelp.UdeBug(tab.IsActiveItem(ho).transform,"isActive");
            ho.Spacer(1, 35);
            PoolHelp.UdeBug(tab.keyWordItem(ho).transform, "keyWord");
            ho.Spacer(1, 20);
            PoolHelp.UdeBug(tab.FilterMdoeItem(ho).transform, "filter");
            ho.Spacer(1, 40);
            ho.allMatchObject = tab.IsAllMatchItem(ho).gameObject;
            PoolHelp.UdeBug(ho.allMatchObject.transform, "allMatch");
            ho.spacerObject = ho.Spacer(1, 60).gameObject;
            ho.Spacer(1, 40);
            PoolHelp.UdeBug(tab.RemoveItem(ho).transform, "remove");
            ho.RefreshState(ho.isShowIsAllMatch);


            //进行预制对象的创建
            List<PlanLayout> planList = new List<PlanLayout>();
            for (int i = 0; i < count; i++)
            {
                planList.Add(PoolManager.Spawn<PlanLayout>(ho, null));
            }
            for (int i = 0; i < count; i++)
            {
                //planList.RemoveAt(i);
                PoolManager.Despawn(planList[i]);
            }
            planList.Clear();
            //消除临时对象
            //temp.DestroyObject();
        }
        public static PlanLayout SpawnPlanLayout(Transform parent,int index)
        {
            PoolGroup poolGroup = PoolManager.current.groups.TryGetValue("planLayout");
            if (poolGroup == null)
            {
                UDebug.Log("----------------------autobuy找不到对象池------------------------------");
            }
            PlanLayout layout = poolGroup.Spawn(parent).gameObject.GetComponent<PlanLayout>();
            UDebug.Log(index);
            layout.SetId(index);
            return layout;
        }

        public static void UdeBug(Transform t,string msg){
            UDebug.Log(msg+":" + t.position);
            }
    }
}
